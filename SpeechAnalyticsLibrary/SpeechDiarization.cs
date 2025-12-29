using Azure.Core;
using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary.Models;
using System.Text;

namespace SpeechAnalyticsLibrary
{
   public class SpeechDiarization
   {
      private readonly ILogger<SpeechDiarization> log;
      private readonly AiServices settings;
      private readonly TokenCredential credential;
      private readonly TokenRequestContext speechScope;

      public SpeechDiarization(ILogger<SpeechDiarization> log, AnalyticsSettings settings, IdentityHelper identityHelper)
      {
         this.log = log;
         this.settings = settings.AiServices;
         credential = identityHelper?.TokenCredential ?? new DefaultAzureCredential();
         var tenantId = string.IsNullOrWhiteSpace(identityHelper?.TenantId) ? null : identityHelper.TenantId;
         speechScope = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }, tenantId: tenantId);
      }

      public async Task<(string source, string transcription)> TranscribeWAVAudio(string transcriptionFileName, string wavFileaPath)
      {
         StringBuilder sb = new();
         var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
         var speechConfig = await CreateSpeechConfigAsync();

         using (var audioConfig = AudioConfig.FromWavFileInput(wavFileaPath))
         {
            using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
            {
               conversationTranscriber.Transcribing += (s, e) =>
               {
                  log.LogTrace($"TRANSCRIBING: Text={e.Result.Text}");
               };

               conversationTranscriber.Transcribed += (s, e) =>
               {
                  if (e.Result.Reason == ResultReason.RecognizedSpeech)
                  {
                     var x = $"{e.Result.SpeakerId}: {e.Result.Text}";
                     sb.AppendLine(x);
                     log.LogDebug(x);
                  }
                  else if (e.Result.Reason == ResultReason.NoMatch)
                  {
                     log.LogError($"NOMATCH: Speech could not be TRANSCRIBED.");
                  }
               };

               conversationTranscriber.Canceled += (s, e) =>
               {
                  // log.LogWarning($"CANCELED: Reason={e.Reason}");

                  if (e.Reason == CancellationReason.Error)
                  {
                     log.LogError($"CANCELED: ErrorCode={e.ErrorCode}");
                     log.LogError($"CANCELED: ErrorDetails={e.ErrorDetails}");
                     log.LogError($"CANCELED: Did you set the speech resource key and region values?");
                     stopRecognition.TrySetResult(0);
                  }

                  stopRecognition.TrySetResult(0);
               };

               conversationTranscriber.SessionStopped += (s, e) =>
               {
                  //Console.WriteLine("\n    Session stopped event.");
                  stopRecognition.TrySetResult(0);
               };

               await conversationTranscriber.StartTranscribingAsync();

               // Waits for completion. Use Task.WaitAny to keep the task rooted. 
               Task.WaitAny(new[] { stopRecognition.Task });

               await conversationTranscriber.StopTranscribingAsync();
            }
         }
         return (transcriptionFileName, sb.ToString());
      }

      private async Task<SpeechConfig> CreateSpeechConfigAsync(CancellationToken cancellationToken = default)
      {
         var token = await credential.GetTokenAsync(speechScope, cancellationToken);
         var endpoint = new Uri($"https://{settings.Region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1");
         var config = SpeechConfig.FromEndpoint(endpoint);
         config.AuthorizationToken = token.Token;
         config.SpeechRecognitionLanguage = "en-US";
         return config;
      }

   }

}
