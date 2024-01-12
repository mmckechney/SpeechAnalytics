
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Spectre.Console;
using SpeechAnalytics.Models;


namespace SpeechAnalytics
{

   public class Program
   {

      private const string batchRoute = "/speechtotext/v3.1/transcriptions";
      private static AnalyticsSettings settings;
      private static ILoggerFactory logFactory;
      private static SemanticMemory semanticMemory;
      private static FileHandling fileHandler;
      private static BatchTranscription batch;
      private static IdentityHelper identityHelper;
      private static IConfigurationRoot config;
      private static ILogger log;
      private static SkAi skAi;
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

         //Not used yet...
         //semanticMemory = new SemanticMemory(logFactory, settings.AzureOpenAi, settings.AiSearch);
         //semanticMemory.InitMemory();
         identityHelper = new IdentityHelper(logFactory.CreateLogger<IdentityHelper>());
         fileHandler = new FileHandling(logFactory.CreateLogger<FileHandling>(), identityHelper);
         batch = new BatchTranscription(logFactory.CreateLogger<BatchTranscription>(), fileHandler);
         skAi = new SkAi(logFactory.CreateLogger<SkAi>(), config, logFactory, settings.AzureOpenAi);
      }
      static async Task Main(string[] args)
      {
         SetUp(args);

         Console.ForegroundColor = ConsoleColor.Blue;
         AnsiConsole.Write(new FigletText($"Azure AI Speech Analytics"));
         Console.WriteLine("This demo tool will transcribe an audio file and analyze its contents for sentiment, action items and problems causes");
         Console.ForegroundColor = ConsoleColor.White;

         Dictionary<int, string> files;
         files = await fileHandler.GetTranscriptionList(settings.Storage.TargetContainerUrl);


         while (true)
         {
            log.LogInformation("");
            log.LogInformation("Please make a selection:", ConsoleColor.Green);
            log.LogInformation("1. Transcribe a new audio file");
            log.LogInformation("");
            if (files.Count > 0) log.LogInformation("or select from a previous transcription:");
            foreach (var file in files)
            {
               log.LogInformation($"{file.Key}. {file.Value}");
            }
            Console.WriteLine();
            var selection = Console.ReadLine();

            string transcriptionText;
            if (!int.TryParse(selection, out int index))
            {
               log.LogError("Please make a valid selection, number only.");
               continue;
            }

            if (index == 1)
            {
               transcriptionText = await NewFileTranscription();
            }
            else
            {
               string fileName = files[index];
               transcriptionText = await fileHandler.GetTranscriptionFileText(fileName, settings.Storage.TargetContainerUrl);
            }

            if (string.IsNullOrWhiteSpace(transcriptionText))
            {
               log.LogWarning("No transcription text found. Please make another selection", ConsoleColor.Magenta);
               continue;
            }

            log.LogInformation("Analyzing transcript for sentiment...");
            log.LogInformation("");

            string sentiment = await skAi.GetSentimentFromTranscription(transcriptionText);
            log.LogInformation(sentiment, ConsoleColor.DarkCyan);

            log.LogInformation("");
            log.LogInformation("Analyzing transcript for action items...");
            log.LogInformation("");
            string actions = await skAi.GetFollowUpActionItems(transcriptionText);
            log.LogInformation($"Action Items:{Environment.NewLine}{actions}", ConsoleColor.Cyan);

            log.LogInformation("");
            log.LogInformation("Analyzing transcript problem root cause...");
            log.LogInformation("");
            string root = await skAi.GetProblemRootCause(transcriptionText);
            log.LogInformation($"Problem Statement and Root Cause:{Environment.NewLine}{root}", ConsoleColor.DarkCyan);


            files = await fileHandler.GetTranscriptionList(settings.Storage.TargetContainerUrl);
            

         }
      }

      private static async Task<string> NewFileTranscription()
      {
         log.LogInformation("Provide the full path to a document to upload and transcribe:", ConsoleColor.Cyan);
         var path = Console.ReadLine();
         path = path.Replace("\"", "");
         if (!File.Exists(path))
         {
            log.LogInformation("File not found. Please try again.");
            await NewFileTranscription();
         }


         var aiSvcs = settings.AiServices;
         var fileInfo = new FileInfo(path);
         log.LogInformation("");
         var initialResponse = await batch.StartBatchTranscription(fileInfo, aiSvcs.Endpoint, aiSvcs.Key, settings.Storage.SourceContainerUrl);

         TranscriptionResponse? statusResponse = null;
         if (initialResponse != null)
         {
            log.LogDebug($"Path to Transcription Job: {initialResponse.Self}");
            statusResponse = await batch.CheckTranscriptionStatus(fileInfo, initialResponse.Self, aiSvcs.Key);
         }

         List<string>? translationLinks = null;
         if (statusResponse != null && statusResponse.Links != null && statusResponse.Links.Files != null)
         {
            log.LogDebug($"Path to Transcription Files List: ${statusResponse.Links.Files}");
            translationLinks = await batch.GetTranslationOutputLinks(statusResponse.Links.Files, aiSvcs.Key);
         }else
         {
            log.LogError("Failed to transcribe files");
            return "";
         }

         string transcriptionText = null;
         if (translationLinks != null)
         {
            log.LogDebug($"Transcription File Links: {string.Join(Environment.NewLine, translationLinks.ToArray())}");
            log.LogInformation("");
            transcriptionText = await batch.GetTranscriptionText(translationLinks);
            await fileHandler.SaveTranscriptionFile(fileInfo, transcriptionText, settings.Storage.TargetContainerUrl);
            log.LogDebug($"Transcription Text:{Environment.NewLine}{transcriptionText}");
         }
         log.LogInformation("Complete");

         //Not used yet...
         //if (transcriptionText != null)
         //{
         //   await semanticMemory.StoreMemoryAsync(fileInfo.Name, fileInfo.Name, transcriptionText);

         //}
         return transcriptionText;
      }


      private static LogLevel SetLogLevel(string[] args)
      {
         var levelIndex = Array.IndexOf(args, "--loglevel");
         if (levelIndex >= 0 && args.Length > levelIndex)
         {
            var logLevel = args[levelIndex + 1];
            if (Enum.TryParse<LogLevel>(logLevel, true, out LogLevel logLevelParsed))
            {
               return logLevelParsed;
            }
         }
         return LogLevel.Information;
      }
   }
}
