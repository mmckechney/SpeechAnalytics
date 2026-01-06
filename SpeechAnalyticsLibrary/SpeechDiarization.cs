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
        private readonly AnalyticsSettings settings;
        private readonly FoundryAgentSettings foundrySettings;
        private readonly TokenCredential credential;
        private readonly string? tenantId;


        public SpeechDiarization(ILogger<SpeechDiarization> log, AnalyticsSettings settings, IdentityHelper identityHelper)
        {
            this.log = log;
            this.settings = settings;
            this.foundrySettings = settings.FoundryAgent;
            credential = identityHelper?.TokenCredential ?? new DefaultAzureCredential();
            this.tenantId = string.IsNullOrWhiteSpace(identityHelper?.TenantId) ? null : identityHelper.TenantId;

        }

        public async Task<(string source, string transcription)> TranscribeWAVAudio(string transcriptionFileName, string wavFileaPath)
        {
            StringBuilder sb = new();
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var speechConfig = await CreateSpeechConfigAsync(this.settings.FoundryAgent);

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

        private async Task<SpeechConfig> CreateSpeechConfigAsync(FoundryAgentSettings foundrySettings, CancellationToken cancellationToken = default)
        {
            var speechScope = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }, tenantId: tenantId);
            var token = await credential.GetTokenAsync(speechScope, cancellationToken);
            string authorizationToken = $"aad#{foundrySettings.ResourceId}#{token.Token}";
            var config = SpeechConfig.FromAuthorizationToken(authorizationToken, foundrySettings.Region);
            config.OutputFormat = OutputFormat.Detailed;
            config.SpeechRecognitionLanguage = "en-US";

            return config;
        }

    }

}
