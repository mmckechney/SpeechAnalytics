using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Spectre.Console;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;
using System.Text.Json;
using YamlDotNet.Serialization;

namespace SpeechAnalytics
{
#pragma warning disable SKEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

   public class Program
   {

      private static AnalyticsSettings settings;
      private static ILoggerFactory logFactory;
      private static FileHandling fileHandler;
      private static BatchTranscription batch;
      private static IdentityHelper identityHelper;
      private static CosmosHelper cosmosHelper;
      private static SpeechDiarization speechD;
      private static IFunctionInvocationFilter functionFilter;
      private static IConfigurationRoot config;
      private static ILogger log;
      private static SkAi skAi;
      private static LogLevel logLevel = LogLevel.Information;
      static Program()
      {
         config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
           .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
           .AddEnvironmentVariables()
           .Build();

         settings = new AnalyticsSettings();
         config.Bind(settings);


      }
      public static void SetUp(string[] args)
      {
         var loglevel = SetLogLevel(args);
         logFactory = LoggerFactory.Create(builder =>
         {
            builder.SetMinimumLevel(loglevel);
            builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddConsole(options =>
            {
               options.FormatterName = "custom";

            });
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
         });
         log = logFactory.CreateLogger("Program");

         identityHelper = new IdentityHelper(logFactory.CreateLogger<IdentityHelper>());
         fileHandler = new FileHandling(logFactory.CreateLogger<FileHandling>(), identityHelper);
         cosmosHelper = new CosmosHelper(logFactory.CreateLogger<CosmosHelper>(), settings);
         functionFilter = new FunctionInvocationFilter(new LoggerFactory().CreateLogger<FunctionInvocationFilter>());
         skAi = new SkAi(logFactory.CreateLogger<SkAi>(), config, logFactory, settings, cosmosHelper, loglevel, functionFilter);
         batch = new BatchTranscription(logFactory.CreateLogger<BatchTranscription>(), fileHandler, skAi, settings);
         speechD = new SpeechDiarization(logFactory.CreateLogger<SpeechDiarization>(), settings);

      }
      static async Task Main(string[] args)
      {
         SetUp(args);

         Console.ForegroundColor = ConsoleColor.Blue;
         AnsiConsole.Write(new FigletText($"Azure AI Speech Analytics"));
         Console.WriteLine("This demo tool will transcribe an audio file and analyze its contents for sentiment, action items and problems causes");
         Console.WriteLine("Find the code here: https://github.com/mmckechney/SpeechAnalytics");
         Console.ForegroundColor = ConsoleColor.White;

         Dictionary<int, string> files;
         int startIndex = 3;
         int pad = 3;
         while (true)
         {
            // Generate a correlation ID for this operation
            var correlationId = Guid.NewGuid().ToString();
            try
            {
               files = await fileHandler.GetTranscriptionList(settings.Storage.TargetContainerUrl, startIndex);
               log.LogInformation($"[CorrelationId: {correlationId}] Loaded transcription list.");
               log.LogInformation("");
               log.LogInformation("Please make a selection:", ConsoleColor.Green);
               log.LogInformation($"{"1.".PadRight(pad)} Transcribe a new audio file");
               log.LogInformation($"{"2.".PadRight(pad)} Transcribe all audio files in container");
               log.LogInformation("");
               if (files.Count > 0) log.LogInformation("Or select from a previous transcription:", ConsoleColor.DarkGreen);
               foreach (var file in files)
               {
                  var tmp = file.Key.ToString() + ".";
                  log.LogInformation($"{tmp.PadRight(pad)} {file.Value}");
               }
               log.LogInformation("");
               log.LogInformation($"Or just start typing to ask a question (be sure to include the filename of the transcription!).", ConsoleColor.Green);
               log.LogInformation("Type 'exit' or 'quit' to end the application.", ConsoleColor.Yellow);

               Console.WriteLine();
               var selection = Console.ReadLine();

               if (string.IsNullOrWhiteSpace(selection))
               {
                  log.LogWarning($"[CorrelationId: {correlationId}] No input provided. Please enter a valid selection.", ConsoleColor.Magenta);
                  continue;
               }
               if (selection.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) || selection.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
               {
                  log.LogInformation($"[CorrelationId: {correlationId}] Application exiting by user request.", ConsoleColor.Cyan);
                  Console.WriteLine("Goodbye!");
                  break;
               }

               List<(string source, string transcription)> transcriptions = new();
               if (!int.TryParse(selection, out int index) || (files.Keys.Count > 0 && index > files.Keys.Max()) || index < 1)
               {
                  log.LogInformation("");
                  log.LogInformation("Getting your response...", ConsoleColor.Cyan);
                  log.LogInformation("Answer:", ConsoleColor.Cyan);
                  try
                  {
                     await foreach (var bit in skAi.AskQuestionsStreaming(selection))
                     {
                        Console.Write(bit);
                     }
                  }
                  catch (Exception ex)
                  {
                     log.LogError($"[CorrelationId: {correlationId}] Error during streaming answer: {ex.Message}", new Dictionary<string, object> { { "Selection", selection } });
                  }
                  log.LogInformation("");
                  log.LogInformation("");
                  log.LogInformation("Press any key to continue...", ConsoleColor.Cyan);
                  Console.ReadKey();
                  continue;
               }

               string fileName;
               try
               {
                  if (index == 1)
                  {
                     transcriptions = await SingleFileTranscription(files);
                     if (transcriptions == null || transcriptions.Count == 0)
                     {
                        log.LogWarning($"[CorrelationId: {correlationId}] No transcriptions returned for single file.", ConsoleColor.Magenta);
                        continue;
                     }
                  }
                  else if (index == 2)
                  {
                     transcriptions = await NewTranscriptions(null);
                     if (transcriptions == null || transcriptions.Count == 0)
                     {
                        log.LogWarning($"[CorrelationId: {correlationId}] No transcriptions returned for all files.", ConsoleColor.Magenta);
                        continue;
                     }
                  }
                  else
                  {
                     fileName = files[index];
                     transcriptions.Add(await fileHandler.GetTranscriptionFileTextFromBlob(fileName, settings.Storage.TargetContainerUrl));
                  }
               }
               catch (Exception ex)
               {
                  log.LogError($"[CorrelationId: {correlationId}] Error during transcription selection: {ex.Message}", new Dictionary<string, object> { { "Selection", selection }, { "Index", index } });
                  continue;
               }

               if (transcriptions.Count == 0 || transcriptions[0].transcription.Length == 0)
               {
                  log.LogWarning($"[CorrelationId: {correlationId}] No transcription text found. Please make another selection", ConsoleColor.Magenta);
                  continue;
               }

               var reask = true;
               var rerun = true;
               InsightResults insightObj = null;
               foreach (var transcription in transcriptions)
               {
                  try
                  {
                     var existingInsightObj = await cosmosHelper.GetAnalysis(transcription.source);
                     if (existingInsightObj != null && reask == true)
                     {
                        insightObj = existingInsightObj;
                        if (transcriptions.Count == 1)
                        {
                           log.LogInformation("Analysis of this transcript has already been performed. Do you want to analyze it again (y/n)?", ConsoleColor.Yellow);
                        }
                        else
                        {
                           log.LogInformation("Analysis of this transcript has already been performed. Do you want to analyze it again (y/n) or (Y/N) - yes for all/no for all)?", ConsoleColor.Yellow);
                        }
                        var input = Console.ReadLine();
                        if (input.StartsWith("Y"))
                        {
                           rerun = true;
                           reask = false;
                        }
                        else if (input.StartsWith("N"))
                        {
                           rerun = false;
                           reask = false;
                        }
                        else if (input.StartsWith("y"))
                        {
                           rerun = true;
                        }
                        else
                        {
                           rerun = false;
                        }
                     }

                     if (rerun)
                     {
                        log.LogInformation("");
                        log.LogInformation("");
                        log.LogInformation($"Analyzing transcript {transcription.source} for sentiment...");
                        log.LogInformation("");
                        string insights = string.Empty;
                        try
                        {
                           insights = await skAi.GetTranscriptionInsights(transcription.transcription, transcription.source);
                        }
                        catch (Exception ex)
                        {
                           log.LogError($"[CorrelationId: {correlationId}] Error getting insights from Azure OpenAI: {ex.Message}", new Dictionary<string, object> { { "Source", transcription.source } });
                           Console.WriteLine("An error occurred while analyzing the transcript. Please try again later.");
                           continue;
                        }
                        if (string.IsNullOrWhiteSpace(insights))
                        {
                           log.LogError($"[CorrelationId: {correlationId}] Failed to get insights from Azure OpenAI", new Dictionary<string, object> { { "Source", transcription.source } });
                           Console.WriteLine("No insights were returned for this transcript.");
                           continue;
                        }
                        try
                        {
                           log.LogDebug($"{insights.ExtractJson()}", ConsoleColor.DarkCyan);
                           insightObj = JsonSerializer.Deserialize<InsightResults>(insights.ExtractJson());
                           insightObj.TranscriptText = transcription.transcription;
                           if (existingInsightObj != null)
                           {
                              insightObj.id = existingInsightObj.id;
                           }
                           log.LogDebug(JsonSerializer.Serialize<InsightResults>(insightObj, new JsonSerializerOptions() { WriteIndented = true }));
                        }
                        catch (Exception ex)
                        {
                           log.LogError($"[CorrelationId: {correlationId}] Error deserializing insight results: {ex.Message}", new Dictionary<string, object> { { "Source", transcription.source } });
                           Console.WriteLine("An error occurred while processing the analysis results. Please try again later.");
                           continue;
                        }
                        bool saved = false;
                        try
                        {
                           saved = await cosmosHelper.SaveAnalysis(insightObj);
                        }
                        catch (Exception ex)
                        {
                           log.LogError($"[CorrelationId: {correlationId}] Error saving analysis to CosmosDB: {ex.Message}", new Dictionary<string, object> { { "CallId", insightObj?.CallId } });
                           Console.WriteLine("An error occurred while saving the analysis results. Please try again later.");
                        }
                        if (saved)
                        {
                           log.LogInformation($"[CorrelationId: {correlationId}] Saved analysis to CosmosDB", ConsoleColor.Green);
                        }
                        else
                        {
                           log.LogWarning($"[CorrelationId: {correlationId}] Failed to save analyis to CosmosDB", ConsoleColor.Red);
                        }
                     }

                     log.LogInformation("");
                     log.LogInformation("");
                     log.LogInformation($"Analysis for: {insightObj.CallId}", ConsoleColor.DarkCyan);
                     log.LogInformation("Sentiment:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + insightObj.Sentiment);
                     log.LogInformation("Sentiment Examples:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + string.Join($"{Environment.NewLine}  - ", insightObj.SentimentExamples));
                     log.LogInformation("Action Items:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + string.Join($"{Environment.NewLine}  - ", insightObj.FollowUpActions));
                     log.LogInformation("Problem Statement:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + insightObj.ProblemStatement);
                     log.LogInformation("Root Cause Type:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + insightObj.RootCause);
                     log.LogInformation("Problem Resolved?:", ConsoleColor.Cyan);
                     log.LogInformation($"  - " + insightObj.Resolved);
                     log.LogInformation("");
                     log.LogInformation("Full Transcript:", ConsoleColor.Cyan);
                     log.LogInformation(insightObj.TranscriptText);
                  }
                  catch (Exception ex)
                  {
                     log.LogError($"[CorrelationId: {correlationId}] Error during analysis or logging: {ex.Message}", new Dictionary<string, object> { { "Source", transcription.source } });
                     Console.WriteLine("An unexpected error occurred during analysis. Please try again.");
                  }
               }
            }
            catch (Exception exe)
            {
               log.LogError($"[CorrelationId: {correlationId}] Unhandled error in main loop: {exe.Message}");
               Console.WriteLine("A critical error occurred. Please restart the application.");
            }
            log.LogInformation($"[CorrelationId: {correlationId}] ----------------------------------------------------------");
         }
      }

      private static async Task<List<(string source, string transcription)>> SingleFileTranscription(Dictionary<int, string> existingFiles)
      {
         List<(string source, string transcription)> transcriptions = new();
         log.LogInformation("Provide the full path to a document to upload and transcribe:", ConsoleColor.Cyan);
         var path = Console.ReadLine();
         path = path.Replace("\"", "");
         if (!File.Exists(path))
         {
            log.LogInformation("File not found.", ConsoleColor.Red);
            return transcriptions;
         }

         var fileName = fileHandler.GetTranscriptionFileName(new FileInfo(path));
         if (existingFiles.ContainsValue(fileName))
         {
            log.LogInformation("This file has already been transcribed. Do you want to transcribe it again (y/n)?", ConsoleColor.Yellow);
            var overwrite = Console.ReadLine();
            if (overwrite.ToLower() == "y")
            {
               transcriptions = await NewTranscriptions(path);
            }
            else
            {
               transcriptions.Add(await fileHandler.GetTranscriptionFileTextFromBlob(fileName, settings.Storage.TargetContainerUrl));
            }
         }
         else
         {
            if (Path.GetExtension(path).ToLower() == ".wav")
            {
               var transcription = await speechD.TranscribeWAVAudio(fileName, path);
               await fileHandler.SaveTranscriptionFile(transcription.source, transcription.transcription, settings.Storage.TargetContainerUrl);
               transcriptions.Add(transcription);
            }
            else
            {
               transcriptions.AddRange(await NewTranscriptions(path));
            }

         }

         return transcriptions;
      }

      private static async Task<List<(string source, string transcription)>> NewTranscriptions(string? path)
      {
         List<(string source, string transcription)> transcriptions = null;
         var aiSvcs = settings.AiServices;
         FileInfo fileInfo = null;
         if (!string.IsNullOrWhiteSpace(path))
         {
            fileInfo = new FileInfo(path);
         }
         log.LogInformation("");
         var initialResponse = await batch.StartBatchTranscription(aiSvcs.Endpoint, aiSvcs.Key, settings.Storage.SourceContainerUrl, settings.Storage.TargetContainerUrl, fileInfo);

         TranscriptionResponse? statusResponse = null;
         if (initialResponse != null)
         {
            log.LogDebug($"Path to Transcription Job: {initialResponse.Self}");
            statusResponse = await batch.CheckTranscriptionStatus(initialResponse.Self, aiSvcs.Key);
         }

         List<string>? translationLinks = null;
         if (statusResponse != null && statusResponse.Links != null && statusResponse.Links.Files != null)
         {
            log.LogDebug($"Path to Transcription Files List: ${statusResponse.Links.Files}");
            translationLinks = await batch.GetTranslationOutputLinks(statusResponse.Links.Files, aiSvcs.Key);
         }
         else
         {
            log.LogError("Failed to transcribe files");
            return transcriptions;
         }


         if (translationLinks != null)
         {
            log.LogDebug($"Transcription File Links: {string.Join(Environment.NewLine, translationLinks.ToArray())}");
            log.LogInformation("");
            transcriptions = await batch.GetTranscriptionText(translationLinks);
            foreach (var transcription in transcriptions)
            {
               await fileHandler.SaveTranscriptionFile(transcription.source, transcription.transcription, settings.Storage.TargetContainerUrl);
               log.LogDebug($"Transcription for {transcription.source}:{Environment.NewLine}{transcription.transcription}");
            }
         }
         log.LogInformation("Complete");

         //Not used yet...
         //if (transcriptionText != null)
         //{
         //   await semanticMemory.StoreMemoryAsync(fileInfo.Name, fileInfo.Name, transcriptionText);

         //}
         return transcriptions;
      }



      private static LogLevel SetLogLevel(string[] args)
      {
         var levelIndex = Array.IndexOf(args, "--loglevel");
         if (levelIndex >= 0 && args.Length > levelIndex)
         {
            var logLevel = args[levelIndex + 1];
            if (Enum.TryParse<LogLevel>(logLevel, true, out LogLevel logLevelParsed))
            {
               Program.logLevel = logLevelParsed;
               return logLevelParsed;
            }
         }
         return LogLevel.Information;
      }
   }
}

