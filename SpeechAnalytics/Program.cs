
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Spectre.Console;
using SpeechAnalytics.Models;
using System.Text.Json;


namespace SpeechAnalytics
{

   public class Program
   {

      private static AnalyticsSettings settings;
      private static ILoggerFactory logFactory;
      private static SemanticMemory semanticMemory;
      private static FileHandling fileHandler;
      private static BatchTranscription batch;
      private static IdentityHelper identityHelper;
      private static CosmosHelper cosmosHelper;
      private static SpeechDiarization speechD;
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

         //Not used yet...
         //semanticMemory = new SemanticMemory(logFactory, settings.AzureOpenAi, settings.AiSearch);
         //semanticMemory.InitMemory();
         identityHelper = new IdentityHelper(logFactory.CreateLogger<IdentityHelper>());
         fileHandler = new FileHandling(logFactory.CreateLogger<FileHandling>(), identityHelper);
         skAi = new SkAi(logFactory.CreateLogger<SkAi>(), config, logFactory, settings.AzureOpenAi, loglevel);
         batch = new BatchTranscription(logFactory.CreateLogger<BatchTranscription>(), fileHandler, skAi, settings.AiServices);
         cosmosHelper = new CosmosHelper(logFactory.CreateLogger<CosmosHelper>(), settings.CosmosDB);
         speechD = new SpeechDiarization(logFactory.CreateLogger<SpeechDiarization>(), settings.AiServices);

      }
      static async Task Main(string[] args)
      {
         SetUp(args);

         Console.ForegroundColor = ConsoleColor.Blue;
         AnsiConsole.Write(new FigletText($"Azure AI Speech Analytics"));
         Console.WriteLine("This demo tool will transcribe an audio file and analyze its contents for sentiment, action items and problems causes");
         Console.ForegroundColor = ConsoleColor.White;

         Dictionary<int, string> files;
         int startIndex = 3;
         files = await fileHandler.GetTranscriptionList(settings.Storage.TargetContainerUrl, startIndex);


         while (true)
         {
            try
            {
               log.LogInformation("");
               log.LogInformation("Please make a selection:", ConsoleColor.Green);
               log.LogInformation("1. Transcribe a new audio file");
               log.LogInformation($"2. Transcribe all audio files in container");
               log.LogInformation("");
               if (files.Count > 0) log.LogInformation("or select from a previous transcription:");
               foreach (var file in files)
               {
                  log.LogInformation($"{file.Key}. {file.Value}");
               }
               Console.WriteLine();
               var selection = Console.ReadLine();

               List<(string source, string transcription)> transcriptions = new();
               if (!int.TryParse(selection, out int index) || (files.Keys.Count > 0 && index > files.Keys.Max()) || index < 1)
               {
                  log.LogError("Please make a valid selection, number only.");
                  continue;
               }

               string fileName;
               if (index == 1)
               {
                  transcriptions = await SingleFileTranscription(files);
                  if (transcriptions == null || transcriptions.Count == 0)
                  {
                     continue;
                  }

               }
               else if (index == 2)
               {
                  transcriptions = await NewTranscriptions(null);
                  if (transcriptions == null || transcriptions.Count == 0)
                  {
                     continue;
                  }
               }
               else
               {
                  fileName = files[index];
                  transcriptions.Add(await fileHandler.GetTranscriptionFileTextFromBlob(fileName, settings.Storage.TargetContainerUrl));
               }

               if (transcriptions.Count == 0 || transcriptions[0].transcription.Length == 0)
               {
                  log.LogWarning("No transcription text found. Please make another selection", ConsoleColor.Magenta);
                  continue;
               }

               var reask = true;
               var rerun = true;
               InsightResults insightObj = null;
               foreach (var transcription in transcriptions)
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

                     string insights = await skAi.GetTranscriptionInsights(transcription.transcription, transcription.source);
                     if (string.IsNullOrWhiteSpace(insights))
                     {
                        log.LogError("Failed to get insights from Azure OpenAI");
                        continue;
                     }
                     log.LogDebug($"{insights.ExtractJson()}", ConsoleColor.DarkCyan);
                     insightObj = JsonSerializer.Deserialize<InsightResults>(insights.ExtractJson());
                     insightObj.TranscriptText = transcription.transcription;
                     if (existingInsightObj != null)
                     {
                        insightObj.id = existingInsightObj.id;
                     }

                     bool saved = await cosmosHelper.SaveAnalysis(insightObj);
                     if (saved)
                     {
                        log.LogInformation("Saved analysis to CosmosDB", ConsoleColor.Green);
                     }
                     else
                     {
                        log.LogWarning("Failed to save analyis to CosmosDB", ConsoleColor.Red);
                     }
                  }

                  log.LogInformation("");
                  log.LogInformation("");
                  log.LogInformation($"Analysis for: {insightObj.CallId}", ConsoleColor.DarkCyan);
                  log.LogInformation("Setiment:", ConsoleColor.Cyan);
                  log.LogInformation($"  - " + insightObj.Sentiment);
                  log.LogInformation("Setiment Examples:", ConsoleColor.Cyan);
                  log.LogInformation($"  - " + string.Join($"{Environment.NewLine}  - ", insightObj.SentimentExamples));
                  log.LogInformation("Action Items:", ConsoleColor.Cyan);
                  log.LogInformation($"  - " + string.Join($"{Environment.NewLine}  - ", insightObj.FollowUpActions));
                  log.LogInformation("Problem Statement/Root Cause", ConsoleColor.Cyan);
                  log.LogInformation($"  - " + insightObj.RootCause);
                  log.LogInformation("");
                  log.LogInformation("Full Transcript:", ConsoleColor.Cyan);
                  log.LogInformation(insightObj.TranscriptText);

               }
            }catch(Exception exe)
            {
               log.LogError(exe.Message);
            }

            files = await fileHandler.GetTranscriptionList(settings.Storage.TargetContainerUrl, startIndex);
            log.LogInformation("----------------------------------------------------------");
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
            //if (overwrite.ToLower() == "f")
            //{
            //   log.LogInformation("Using AI to format conversation...");
            //   var raw = await fileHandler.GetTranscriptionFileTextFromBlob(fileName, settings.Storage.TargetContainerUrl);
            //   var formatted = await skAi.GetSpeakerNames(fileName, raw.transcription);
            //   if (formatted.transcription == raw.transcription)
            //   {
            //      log.LogWarning("Unable to format conversation.");
            //   }
            //   else
            //   {
            //      log.LogInformation("Saving formatted conversation");
            //      await fileHandler.SaveTranscriptionFile(fileName, formatted.transcription, settings.Storage.TargetContainerUrl);
            //   }
            //   transcriptions.Add(formatted);
            //}
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

