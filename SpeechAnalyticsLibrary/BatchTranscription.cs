using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary.Models;
using SpeechAnalyticsLibrary.Models.SpeechToText;
using SpeechAnalyticsLibrary.Models.SpeechToText.API;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;


namespace SpeechAnalyticsLibrary
{
   public class BatchTranscription
   {
      private readonly ILogger<BatchTranscription> logger;
      private readonly FileHandling fileHandler;
      private readonly FoundryAgentClient agentClient;
      private readonly AiServices settings;
      private readonly TokenCredential credential;
      private readonly TokenRequestContext speechScope;

      public BatchTranscription(ILogger<BatchTranscription> logger, FileHandling fileHandler, FoundryAgentClient agentClient, AnalyticsSettings settings, IdentityHelper identityHelper)
      {
         this.logger = logger;
         this.fileHandler = fileHandler;
         this.agentClient = agentClient;
         this.settings = settings.AiServices;
         credential = identityHelper?.TokenCredential ?? new DefaultAzureCredential();
         var tenantId = string.IsNullOrWhiteSpace(identityHelper?.TenantId) ? null : identityHelper.TenantId;
         speechScope = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }, tenantId: tenantId);
      }
      private static HttpClient client = new HttpClient();

      public async Task<TranscriptionResponse?> StartBatchTranscription(string transcriptionEndpoint, string sourceContainerUrl, string destinationContainerUrl, Uri? blobFile = null)
      {
         try
         {
         _ = destinationContainerUrl;
            //var whisper = await GetWhisperModel(transcriptionEndpoint, transcriptionKey);
            //if (whisper.name != "")
            //{
            //   logger.LogInformation($"Using Whisper model: {whisper}");
            //}

            if(blobFile == null)
            {
               var containerFile = await fileHandler.GetListOfAudioFilesInContainer(sourceContainerUrl);
               if (containerFile.Count == 0)
               {
                  logger.LogError($"No files found in container '{sourceContainerUrl}'");
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
               transcriptionReq.ContentContainerUrl = new Uri(sourceContainerUrl);
            }

            var transcrReqJson = JsonSerializer.Serialize(transcriptionReq, new JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            logger.LogDebug(transcrReqJson);
            var createTime = DateTime.UtcNow;
            using HttpRequestMessage request = new HttpRequestMessage();
            {

               StringContent requestContent = new StringContent(transcrReqJson, Encoding.UTF8, "application/json");

               request.Method = HttpMethod.Post;
               request.RequestUri = new Uri($"{transcriptionEndpoint}/speechtotext/{settings.ApiVersion}/transcriptions");
               var token = await credential.GetTokenAsync(speechScope, CancellationToken.None);
               request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
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
                  logger.LogError($"Status code: {response.StatusCode} for transcription request to '{transcriptionEndpoint}'. Response: {result}");
               }
            }
         }
         catch (Exception ex)
         {
            logger.LogError($"Error in StartBatchTranscription: {ex.Message}");
         }
         return null;
      }
      public async Task<TranscriptionResponse?> StartBatchTranscription(string transcriptionEndpoint, string sourceContainerUrl, string destinationContainerUrl, FileInfo? localFile = null)
      {
         try
         {
         _ = destinationContainerUrl;
            Uri blobFile = null;
            if (localFile != null)
            {
               blobFile = new Uri(await fileHandler.UploadBlobForTranscription(localFile, sourceContainerUrl));
            }
            return await StartBatchTranscription(transcriptionEndpoint, sourceContainerUrl, destinationContainerUrl, blobFile);
         }
         catch (Exception ex)
         {
            logger.LogError($"Error in StartBatchTranscription (FileInfo overload): {ex.Message}");
            return null;
         }
      }
      public async Task<TranscriptionResponse?> CheckTranscriptionStatus(string operationUrl)
      {
         int sleepTime = 4000;
         logger.LogInformation("Checking status of document translation:");
         while (true)
         {
            try
            {
               using HttpRequestMessage request = new HttpRequestMessage();
               {

                  request.Method = HttpMethod.Get;
                  request.RequestUri = new Uri(operationUrl);
                  var token = await credential.GetTokenAsync(speechScope, CancellationToken.None);
                  request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                  HttpResponseMessage response = await client.SendAsync(request);
                  string result = response.Content.ReadAsStringAsync().Result;
                  if (response.IsSuccessStatusCode)
                  {
                     TranscriptionResponse resObj = null;
                     try
                     {
                        resObj = JsonSerializer.Deserialize<TranscriptionResponse>(result);
                     }
                     catch (Exception ex)
                     {
                        logger.LogError($"Error deserializing transcription status response: {ex.Message}");
                        logger.LogInformation("Waiting for response");
                        Thread.Sleep(sleepTime);
                        continue;
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
                     logger.LogError($"Status code: {response.StatusCode} for CheckTranscriptionStatus at '{operationUrl}'. Response: {result}");
                     return null;
                  }
               }
            }
            catch (Exception ex)
            {
               logger.LogError($"Error in CheckTranscriptionStatus: {ex.Message}");
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
                     var speakers = await agentClient.GetSpeakerNames(tmpSource, output.SpeakerTranscriptionText);
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
                     logger.LogError($"Status code: {response.StatusCode} for GetTranscriptionText at '{translationSasUrl}'. Response: {result}");
                  }
               }
            }
            catch (Exception exe)
            {
               logger.LogError($"Error in GetTranscriptionText for '{translationSasUrl}': {exe.Message}");
            }
         }
         return transcriptions;
      }
      public async Task<List<string>?> GetTranslationOutputLinks(string filesUrl)
      {
         try
         {
            using HttpRequestMessage request = new HttpRequestMessage();
            {
               request.Method = HttpMethod.Get;
               request.RequestUri = new Uri(filesUrl);
               var token = await credential.GetTokenAsync(speechScope, CancellationToken.None);
               request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

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
                  logger.LogError($"Status code: {response.StatusCode} for GetTranslationOutputLinks at '{filesUrl}'. Response: {result}");
                  return null;
               }
            }
         }
         catch (Exception exe)
         {
            logger.LogError($"Error in GetTranslationOutputLinks for '{filesUrl}': {exe.Message}");
            return null;
         }
      }
      public async Task<(string name, string url)> GetWhisperModel(string operationUrl, string url = "")
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
               var token = await credential.GetTokenAsync(speechScope, CancellationToken.None);
               request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

               HttpResponseMessage response = await client.SendAsync(request);
               string result = response.Content.ReadAsStringAsync().Result;
               if (response.IsSuccessStatusCode)
               {
                  var models = JsonSerializer.Deserialize<SpeechModels>(result);
                  var whisper = models.Values.Where(models => models.DisplayName.Contains("Whisper")).OrderBy(m => m.LastActionDateTime).FirstOrDefault();
                  if (whisper == null)
                  {
                     return await GetWhisperModel(operationUrl, models.NextLink);
                  }
                  return (whisper.DisplayName,whisper.Self);
               }
               else
               {
                  logger.LogError($"Status code: {response.StatusCode} for GetWhisperModel at '{operationUrl}'. Response: {result}");
                  return (string.Empty, string.Empty);
               }
            }
         }
         catch (Exception exe)
         {
            logger.LogError($"Error in GetWhisperModel for '{operationUrl}': {exe.Message}");
            return (string.Empty, string.Empty);
         }
      }
   }
}
