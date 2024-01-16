using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using SpeechAnalytics.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SpeechAnalytics
{
   public class BatchTranscription
   {
      ILogger<BatchTranscription> logger;
      FileHandling fileHandler;
      public BatchTranscription(ILogger<BatchTranscription> logger, FileHandling fileHandler)
      {
         this.logger = logger;
         this.fileHandler = fileHandler;
      }
      private static HttpClient client = new HttpClient();
      public async Task<TranscriptionResponse?> StartBatchTranscription(string transcriptionEndpoint, string transcriptionKey, string sourceSas, FileInfo? localFile = null)
      {
         var transcript = new TranscriptionRequest()
         {
            DisplayName = Guid.NewGuid().ToString()
         };
         if (localFile != null)
         {
            string fileUri = await fileHandler.UploadBlobForTranscription(localFile, sourceSas);
            transcript.ContentUrls = new List<string>() { fileUri };

         }
         else
         {
            transcript.ContentContainerUrl = sourceSas;
         }
         var transcrReqJson = JsonSerializer.Serialize<TranscriptionRequest>(transcript, new JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

         logger.LogDebug(transcrReqJson);
         var createTime = DateTime.UtcNow;
         using HttpRequestMessage request = new HttpRequestMessage();
         {

            StringContent requestContent = new StringContent(transcrReqJson, Encoding.UTF8, "application/json");

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(transcriptionEndpoint + "/speechtotext/v3.1/transcriptions");
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
                        logger.LogError($"\tTranslation failed: {resObj.Properties?.Error?.Message}");
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
         List<(string source, string transcription)>transcriptions = new();
         try
         {
            foreach (var translationSasUrl in translationSasUrls)
            {
               using HttpRequestMessage request = new HttpRequestMessage();
               {
                  request.Method = HttpMethod.Get;
                  request.RequestUri = new Uri(translationSasUrl);

                  HttpResponseMessage response = await client.SendAsync(request);
                  string result = response.Content.ReadAsStringAsync().Result;
                  if (response.IsSuccessStatusCode)
                  {
                     var output = JsonSerializer.Deserialize<TranscriptionOutput>(result);
                     var tmpSource = fileHandler.GetTranscriptionFileName(new Uri(output.Source).LocalPath);
                     transcriptions.Add((tmpSource, output.TranscriptionText));
                  }
                  else
                  {
                     logger.LogError($"Status code: {response.StatusCode}");
                     logger.LogError(result);
                   }
               }
            }
            return transcriptions;
         }
         catch (Exception exe)
         {
            logger.LogError($"Error: {exe.Message}");
            return transcriptions;
         }

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
                  var links = JsonSerializer.Deserialize<TranscriptionLinks>(result);
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
   }
}
