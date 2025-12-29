using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Azure;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using SpeechAnalyticsLibrary.Models;

#pragma warning disable OPENAI001 // OpenAI SDK preview constructs required by Azure Agent Framework preview
namespace SpeechAnalyticsLibrary
{
   public class FoundryAgentClient
   {
      private const string InsightsKey = "Transcription.Insights";
      private const string SpeakerKey = "Transcription.SpeakerId";
      private const string QueryKey = "CosmosDb.QueryGenerator";
      private const string AnswerKey = "Ask.Question";

      private readonly ILogger<FoundryAgentClient> _log;
      private readonly CosmosHelper _cosmosHelper;
      private readonly FoundryAgentSettings _settings;
      private readonly AIProjectClient _projectClient;
      private readonly ConcurrentDictionary<string, AgentVersion> _agentCache = new(StringComparer.OrdinalIgnoreCase);
      private readonly IReadOnlyDictionary<string, string> _instructions;
      private readonly IReadOnlyDictionary<string, string> _agentNameMap;
      private readonly SemaphoreSlim _agentGate = new(1, 1);

      public FoundryAgentClient(ILogger<FoundryAgentClient> log, AnalyticsSettings analyticsSettings, CosmosHelper cosmosHelper)
      {
         _log = log ?? throw new ArgumentNullException(nameof(log));
         _cosmosHelper = cosmosHelper ?? throw new ArgumentNullException(nameof(cosmosHelper));
         _settings = analyticsSettings?.FoundryAgent ?? throw new InvalidOperationException("FoundryAgent settings are missing from configuration.");

         if (string.IsNullOrWhiteSpace(_settings.ProjectEndpoint))
         {
            throw new InvalidOperationException("FoundryAgent.ProjectEndpoint must be configured.");
         }

         if (string.IsNullOrWhiteSpace(_settings.ModelDeploymentName))
         {
            throw new InvalidOperationException("FoundryAgent.ModelDeploymentName must be configured.");
         }

         var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
         {
            ExcludeInteractiveBrowserCredential = true
         });

         _projectClient = new AIProjectClient(new Uri(_settings.ProjectEndpoint), credential);
         _instructions = LoadInstructionResources();
         _agentNameMap = BuildAgentNameMap();
      }

      public async Task<string> GetTranscriptionInsights(string transcription, string callId, CancellationToken cancellationToken = default)
      {
         if (string.IsNullOrWhiteSpace(transcription))
         {
            return string.Empty;
         }

         var payload = new StringBuilder()
            .AppendLine("Task: Analyze the customer service transcript and produce a JSON payload using the required schema.")
            .AppendLine($"CallId: {callId}")
            .AppendLine("Transcript:")
            .AppendLine(transcription)
            .ToString();

         return await ExecuteAgentAsync(InsightsKey, payload, cancellationToken);
      }

      internal async Task<Dictionary<string, string>?> GetSpeakerNames(string sourceFileName, string transcription, CancellationToken cancellationToken = default)
      {
         if (string.IsNullOrWhiteSpace(transcription))
         {
            return null;
         }

         var payload = new StringBuilder()
            .AppendLine("Identify participant names for the provided transcript. Preserve any speaker labels you cannot confidently replace.")
            .AppendLine("Transcript:")
            .AppendLine(transcription)
            .ToString();

         try
         {
            var json = await ExecuteAgentAsync(SpeakerKey, payload, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
               return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, new JsonSerializerOptions
            {
               PropertyNameCaseInsensitive = true
            });
         }
         catch (JsonException jsonEx)
         {
            _log.LogError(jsonEx, "Unable to parse speaker response for {SourceFileName}", sourceFileName);
            return null;
         }
      }

      public async Task<string> AskQuestions(string userQuestion, CancellationToken cancellationToken = default)
      {
         try
         {
            var cosmosQueryPrompt = new StringBuilder()
               .AppendLine("Generate a Cosmos DB SQL query that answers the user's question. Return only the query text.")
               .AppendLine("User question:")
               .AppendLine(userQuestion)
               .ToString();

            var cosmosQuery = await ExecuteAgentAsync(QueryKey, cosmosQueryPrompt, cancellationToken);
            var cosmosResults = await _cosmosHelper.GetQueryResults(cosmosQuery ?? string.Empty);

            if (string.IsNullOrWhiteSpace(cosmosResults))
            {
               return "Sorry, I was unable to find an answer. Please try asking in a different way.";
            }

            var answerPrompt = new StringBuilder()
               .AppendLine("You are an analyst that answers user questions using the supplied Cosmos DB results. If the data is insufficient, reply with \"I don't know\".")
               .AppendLine("Question:")
               .AppendLine(userQuestion)
               .AppendLine()
               .AppendLine("Cosmos results (JSON):")
               .AppendLine(cosmosResults)
               .ToString();

            return await ExecuteAgentAsync(AnswerKey, answerPrompt, cancellationToken);
         }
         catch (RequestFailedException ex)
         {
            _log.LogError(ex, "Foundry response failed for question '{Question}'", userQuestion);
            return "Sorry, I am having trouble answering your question.";
         }
         catch (Exception ex)
         {
            _log.LogError(ex, "Unexpected exception when answering question '{Question}'", userQuestion);
            return $"Sorry, I am having trouble answering your question. {ex.Message}";
         }
      }

      public async IAsyncEnumerable<string> AskQuestionsStreaming(string userQuestion, [EnumeratorCancellation] CancellationToken cancellationToken = default)
      {
         var answer = await AskQuestions(userQuestion, cancellationToken);
         if (!string.IsNullOrWhiteSpace(answer))
         {
            yield return answer;
         }
      }

      private async Task<string> ExecuteAgentAsync(string instructionKey, string userInput, CancellationToken cancellationToken)
      {
         var agentVersion = await EnsureAgentVersionAsync(instructionKey, cancellationToken);
         var responseClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);

         CreateResponseOptions options = new()
         {
            InputItems =
            {
               ResponseItem.CreateUserMessageItem(userInput ?? string.Empty)
            }
         };

         ResponseResult response = await responseClient.CreateResponseAsync(options, cancellationToken);
         if (response.Status != ResponseStatus.Completed)
         {
            if (response.Error != null)
            {
               _log.LogError("Agent {Agent} returned error: {Code} - {Message}", agentVersion.Name, response.Error.Code, response.Error.Message);
               throw new InvalidOperationException(response.Error.Message ?? "Agent response returned an error.");
            }
            throw new InvalidOperationException($"Agent {agentVersion.Name} returned status {response.Status}.");
         }

         return response.GetOutputText();
      }

      private async Task<AgentVersion> EnsureAgentVersionAsync(string instructionKey, CancellationToken cancellationToken)
      {
         if (_agentCache.TryGetValue(instructionKey, out var cached))
         {
            return cached;
         }

         await _agentGate.WaitAsync(cancellationToken);
         try
         {
            if (_agentCache.TryGetValue(instructionKey, out cached))
            {
               return cached;
            }

            if (!_instructions.TryGetValue(instructionKey, out var instructions))
            {
               throw new InvalidOperationException($"No instructions found for prompt '{instructionKey}'. Ensure a corresponding .txt file exists in SpeechAnalyticsLibrary/Prompts.");
            }

            var agentName = _agentNameMap.TryGetValue(instructionKey, out var configuredName)
               ? configuredName
               : instructionKey.Replace('.', '-');

            PromptAgentDefinition definition = new(model: _settings.ModelDeploymentName)
            {
               Instructions = instructions
            };

            AgentVersion agentVersion = (await _projectClient.Agents.CreateAgentVersionAsync(
               agentName: agentName,
               options: new AgentVersionCreationOptions(definition),
               cancellationToken: cancellationToken)).Value;

            _agentCache[instructionKey] = agentVersion;
            return agentVersion;
         }
         finally
         {
            _agentGate.Release();
         }
      }

      private static IReadOnlyDictionary<string, string> LoadInstructionResources()
      {
         var assembly = Assembly.GetExecutingAssembly();
         var manifestNames = assembly.GetManifestResourceNames();
         var instructions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

         foreach (var resourceName in manifestNames)
         {
            if (!resourceName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || !resourceName.Contains(".Prompts."))
            {
               continue;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
               continue;
            }

            using StreamReader reader = new(stream);
            var content = reader.ReadToEnd();
            var tokens = resourceName.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 4)
            {
               continue;
            }
            var key = string.Join('.', tokens[^3], tokens[^2]);
            instructions[key] = content;
         }

         return instructions;
      }

      private IReadOnlyDictionary<string, string> BuildAgentNameMap()
      {
         return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
         {
            [InsightsKey] = _settings.InsightsAgentName ?? "speechanalytics-insights",
            [SpeakerKey] = _settings.SpeakerAgentName ?? "speechanalytics-speaker-id",
            [QueryKey] = _settings.QueryAgentName ?? "speechanalytics-cosmos-query",
            [AnswerKey] = _settings.AnswerAgentName ?? "speechanalytics-qna"
         };
      }
   }
}
#pragma warning restore OPENAI001

