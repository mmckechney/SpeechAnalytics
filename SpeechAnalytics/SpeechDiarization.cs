using Microsoft.Extensions.Logging;
using SpeechAnalytics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Graph;

namespace SpeechAnalytics
{
   public class SpeechDiarization
   {
      ILogger<SpeechDiarization> log;
      AiServices settings;
      SpeechConfig speechConfig;
      public SpeechDiarization(ILogger<SpeechDiarization> log, AiServices settings)
      {
         this.log = log;
         this.settings = settings;

         speechConfig = SpeechConfig.FromSubscription(settings.Key, settings.Region);
         speechConfig.SpeechRecognitionLanguage = "en-US";

      }

      public async Task<(string source, string transcription)> TranscribeWAVAudio(string transcriptionFileName, string wavFileaPath)
      {
         StringBuilder sb = new();
         var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

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

   }
  
}
