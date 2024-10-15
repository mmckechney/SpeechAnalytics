using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary.Models;
using SpeechAnalyticsLibrary.Models.SpeechToText;
using SpeechAnalyticsLibrary.Models.SpeechToText.API;
using System.Text;
using System.Text.Json;


namespace SpeechAnalyticsLibrary
{
   public class BatchTranscription
   {
      ILogger<BatchTranscription> logger;
      FileHandling fileHandler;
      SkAi skAi;
      AiServices settings;
      public BatchTranscription(ILogger<BatchTranscription> logger, FileHandling fileHandler, SkAi skAi, AnalyticsSettings settings)
      {
         this.logger = logger;
         this.fileHandler = fileHandler;
         this.skAi = skAi;
         this.settings = settings.AiServices;
      }
      private static HttpClient client = new HttpClient();
      public async Task<TranscriptionResponse?> StartBatchTranscription(string transcriptionEndpoint, string transcriptionKey, string sourceSas, string destinationSas, Uri? blobFile = null)
      {
         //var whisper = await GetWhisperModel(transcriptionEndpoint, transcriptionKey);
         //if (whisper.name != "")
         //{
         //   logger.LogInformation($"Using Whisper model: {whisper}");
         //}

         if(blobFile == null)
         {
            var containerFile = await fileHandler.GetListOfAudioFilesInContainer(sourceSas);
            if (containerFile.Count == 0)
            {
               logger.LogError("No files found in container");
               return null;
            }
            else
            {
               logger.LogInformation("Files to transcribe:");
               containerFile.ForEach(c => logger.LogInformation(c, ConsoleColor.DarkYellow));
            }
         }
         var transcriptionReq = new Transcription()
         {
            DisplayName = Guid.NewGuid().ToString(),
            Locale = "en-US",
            //Model = new EntityReference()
            //{
            //   Self = new Uri(whisper.url)
            //},
            Properties = new TranscriptionProperties()
            {
               TimeToLive = "PT4H",
               PunctuationMode = Models.SpeechToText.API.PunctuationMode.DictatedAndAutomatic,
               ProfanityFilterMode = Models.SpeechToText.API.ProfanityFilterMode.Masked,
               LanguageIdentification = new Models.SpeechToText.API.LanguageIdentificationProperties()
               {
                  CandidateLocales = new List<string>() { "en-US", "es-ES" }
               },
               DiarizationEnabled = true,
               Diarization = new Models.SpeechToText.API.DiarizationProperties()
               {
                  Speakers = new Models.SpeechToText.API.DiarizationSpeakersProperties()
                  {
                     MinCount = 1,
                     MaxCount = 10
                  }


               }

            }
         };

         if (blobFile != null)
         {
            transcriptionReq.ContentUrls = new List<Uri>() { blobFile };
         }
         else
         {
            transcriptionReq.ContentContainerUrl = new Uri(sourceSas);
         }

         var transcrReqJson = JsonSerializer.Serialize(transcriptionReq, new JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

         logger.LogDebug(transcrReqJson);
         var createTime = DateTime.UtcNow;
         using HttpRequestMessage request = new HttpRequestMessage();
         {

            StringContent requestContent = new StringContent(transcrReqJson, Encoding.UTF8, "application/json");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri($"{transcriptionEndpoint}/speechtotext/{settings.ApiVersion}/transcriptions");
            request.Headers.Add("Ocp-Apim-Subscription-Key", transcriptionKey);
            request.Content = requestContent;

            HttpResponseMessage response = await client.SendAsync(request);
            string result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
               var transcrResponse = JsonSerializer.Deserialize<TranscriptionResponse>(result);
               logger.LogInformation("Transcription accepted");
               return transcrResponse;
            }
            else
            {
               logger.LogInformation($"Status code: {response.StatusCode}");
               logger.LogError(result);
            }

         }
         return null;
      }
      public async Task<TranscriptionResponse?> StartBatchTranscription(string transcriptionEndpoint, string transcriptionKey, string sourceSas, string destinationSas, FileInfo? localFile = null)
      {
         Uri blobFile = null;
         if (localFile != null)
         {
            blobFile = new Uri(await fileHandler.UploadBlobForTranscription(localFile, sourceSas));
         }
         return await StartBatchTranscription(transcriptionEndpoint, transcriptionKey, sourceSas, destinationSas, blobFile);
      }
      public async Task<TranscriptionResponse?> CheckTranscriptionStatus(string operationUrl, string transcriptionKey)
      {
         int sleepTime = 4000;
         logger.LogInformation("Checking status of document translation:");
         while (true)
         {
            using HttpRequestMessage request = new HttpRequestMessage();
            {

               request.Method = HttpMethod.Get;
               request.RequestUri = new Uri(operationUrl);
               request.Headers.Add("Ocp-Apim-Subscription-Key", transcriptionKey);

               HttpResponseMessage response = await client.SendAsync(request);
               string result = response.Content.ReadAsStringAsync().Result;
               if (response.IsSuccessStatusCode)
               {
                  TranscriptionResponse resObj = null;
                  try
                  {
                     resObj = JsonSerializer.Deserialize<TranscriptionResponse>(result);
                  }
                  catch (Exception)
                  {
                     logger.LogInformation("Waiting for response");
                     Thread.Sleep(sleepTime);
                  }
                  var status = resObj?.Status?.ToLower();
                   switch (status)
                  {
                     case "succeeded":
                        logger.LogInformation("\tTranscription complete");
                        return resObj;
                     case "notstarted":
                        logger.LogInformation("\tWaiting for translation to start");
                        break;
                     case "running":
                        logger.LogInformation("\tTranscription in progress");
                        break;
                     case "failed":
                        logger.LogError($"\tTranscription failed: {resObj.Properties?.Error?.Message}");
                        return null;
                     default:
                        logger.LogInformation($"\tStatus: {resObj?.Status}");
                        if (resObj?.Properties?.Error != null)
                        {
                           logger.LogError($"\tError message: {resObj.Properties.Error.Message}");
                           return null;
                        }
                        break;

                  }
               }
               else
               {
                  logger.LogError($"Status code: {response.StatusCode}");
                  return null;
               }

            }

            Thread.Sleep(sleepTime);
         }
      }
      public async Task<List<(string source, string transcription)>>? GetTranscriptionText(List<string> translationSasUrls)
      {
         var sb = new StringBuilder();
         List<(string source, string transcription)> transcriptions = new();
         foreach (var translationSasUrl in translationSasUrls)
         {
            try
            {
               using HttpRequestMessage request = new HttpRequestMessage();
               {
                  request.Method = HttpMethod.Get;
                  request.RequestUri = new Uri(translationSasUrl);

                  HttpResponseMessage response = await client.SendAsync(request);
                  string result = response.Content.ReadAsStringAsync().Result;
                  if (response.IsSuccessStatusCode)
                  {
                     logger.LogTrace(result);
                     var output = JsonSerializer.Deserialize<TranscriptionOutput>(result);
                     var tmpSource = fileHandler.GetTranscriptionFileName(new Uri(output.Source).LocalPath);
                     logger.LogTrace($"Raw Transcription: {output.RawTranscriptionText}", ConsoleColor.DarkBlue);

                     logger.LogInformation("Attempting to identify speakers by name");
                     var speakers = await skAi.GetSpeakerNames(tmpSource, output.SpeakerTranscriptionText);
                     if (speakers != null)
                     {
                        var tmp = output.SpeakerTranscriptionText;
                        foreach (var item in speakers)
                        {
                           tmp = tmp.Replace(item.Key, item.Value);
                        }
                        transcriptions.Add((tmpSource, tmp));
                     }
                     else
                     {
                        transcriptions.Add((tmpSource, output.SpeakerTranscriptionText));
                     }
                  }
                  else
                  {
                     logger.LogError($"Status code: {response.StatusCode}");
                     logger.LogError(result);
                  }
               }
            }
            catch (Exception exe)
            {
               logger.LogError($"Error: {exe.Message}");
            }
         }

         return transcriptions;
      }
      public async Task<List<string>?> GetTranslationOutputLinks(string filesUrl, string transcriptionKey)
      {
         try
         {
            using HttpRequestMessage request = new HttpRequestMessage();
            {
               request.Method = HttpMethod.Get;
               request.RequestUri = new Uri(filesUrl);
               request.Headers.Add("Ocp-Apim-Subscription-Key", transcriptionKey);

               HttpResponseMessage response = await client.SendAsync(request);
               string result = response.Content.ReadAsStringAsync().Result;
               if (response.IsSuccessStatusCode)
               {
                  var sb = new StringBuilder();
                  var links = JsonSerializer.Deserialize<Models.TranscriptionLinks>(result);
                  var contentSasUrls = links.Values.Where(x => x.Kind == "Transcription").Select(x => x.Links.ContentUrl).ToList();
                  return contentSasUrls;
               }
               else
               {
                  logger.LogInformation($"Status code: {response.StatusCode}");
                  logger.LogError(result);
                  return null;
               }
            }
         }
         catch (Exception exe)
         {
            logger.LogError($"Error: {exe.Message}");
            return null;
         }
      }

      public async Task<(string name, string url)> GetWhisperModel(string operationUrl, string transcriptionKey, string url = "")
      {
         try
         {
            using HttpRequestMessage request = new HttpRequestMessage();
            {
               request.Method = HttpMethod.Get;
               if (string.IsNullOrWhiteSpace(url))
               {
                  request.RequestUri = new Uri($"{operationUrl}/speechtotext/{settings.ApiVersion}/models/base");
               }
               else
               {
                  request.RequestUri = new Uri(url);
               }
               request.Headers.Add("Ocp-Apim-Subscription-Key", transcriptionKey);

               HttpResponseMessage response = await client.SendAsync(request);
               string result = response.Content.ReadAsStringAsync().Result;
               if (response.IsSuccessStatusCode)
               {
                  var models = JsonSerializer.Deserialize<SpeechModels>(result);
                  var whisper = models.Values.Where(models => models.DisplayName.Contains("Whisper")).OrderBy(m => m.LastActionDateTime).FirstOrDefault();
                  if (whisper == null)
                  {
                     return await GetWhisperModel(operationUrl, transcriptionKey, models.NextLink);
                  }
                  return (whisper.DisplayName,whisper.Self);
               }
               else
               {
                  logger.LogInformation($"Status code: {response.StatusCode}");
                  logger.LogError(result);
                  return (string.Empty, string.Empty);
               }
            }
         }
         catch (Exception exe)
         {
            logger.LogError($"Error: {exe.Message}");
            return (string.Empty, string.Empty);
         }
      }
   }
}
