using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SpeechAnalytics.Models;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace SpeechAnalytics
{
   public class SkAi
   {
      private IConfiguration _config;
      private Kernel sk;
      private ILogger log;
      ILoggerFactory loggerFactory;
      private bool transposeAdaptiveCardColumns = false;
      Dictionary<string, KernelFunction> yamlPrompts = new();
      AzureOpenAi aiSettings;




      private HttpClient _client;
      public SkAi(ILogger<SkAi> log, IConfiguration config, ILoggerFactory loggerFactory, AzureOpenAi aiSettings)
      {
         _config = config;
         this.log = log;
         this.loggerFactory = LoggerFactory.Create(builder =>
         {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddConsole(options =>
            {
               options.FormatterName = "custom";

            });
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
         }); ;
         this.aiSettings = aiSettings;
         InitPlugins();
      }

      private bool InitPlugins()
      {
         HttpClientHandler handler = new HttpClientHandler()
         {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
         };
         _client = new HttpClient(handler);

         if (aiSettings != null)
         {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(deploymentName: aiSettings.ChatDeploymentName, modelId: aiSettings.ChatModel, endpoint: aiSettings.EndPoint, apiKey: aiSettings.Key);
            builder.Services.AddSingleton(loggerFactory);
            this.sk = builder.Build();
         }
         else
         {
            throw new ArgumentNullException("AzureOpenAi Settings is null");
         }

         var assembly = Assembly.GetExecutingAssembly();
         var resources = assembly.GetManifestResourceNames().ToList();
         resources.ForEach(r =>
         {
            if (r.ToLower().EndsWith("yaml"))
            {
               var count = r.Split('.').Count();
               var key = count > 3 ? $"{r.Split('.')[count - 3]}_{r.Split('.')[count - 2]}" : r.Split('.')[count - 2];
               using StreamReader reader = new(Assembly.GetExecutingAssembly().GetManifestResourceStream(r)!);
               var func = sk.CreateFunctionFromPromptYaml(reader.ReadToEnd(), promptTemplateFactory: new HandlebarsPromptTemplateFactory());
               yamlPrompts.Add(key, func);
            }
         });
         var plugin = KernelPluginFactory.CreateFromFunctions("YAMLPlugins", yamlPrompts.Select(y => y.Value).ToArray());
         sk.Plugins.Add(plugin);


#pragma warning disable SKEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
         sk.FunctionInvoked += Sk_FunctionInvoked;
         sk.FunctionInvoking += Sk_FunctionInvoking;

         return true;
      }

      private void Sk_FunctionInvoking(object? sender, FunctionInvokingEventArgs e)
      {
         log.LogDebug($"{Environment.NewLine}INVOKING :{e.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, e.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}");
      }

      private void Sk_FunctionInvoked(object? sender, FunctionInvokedEventArgs e)
      {
         log.LogDebug($"{Environment.NewLine}INVOKED :{e.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, e.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}{Environment.NewLine}Result:{e.Result}");
      }
#pragma warning restore SKEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
      public async Task<string> GetSentimentFromTranscription(string transcription)
      {

         var result = await this.sk.InvokeAsync(yamlPrompts["Transcription_Sentiment"], new() { { "transcription", transcription } });
         return result.GetValue<string>();

      }

      public async Task<string> GetFollowUpActionItems(string transcription)
      {

         var result = await this.sk.InvokeAsync(yamlPrompts["Transcription_FollowUps"], new() { { "transcription", transcription } });
         return result.GetValue<string>();

      }

      internal async Task<string> GetProblemRootCause(string transcription)
      {
         var result = await this.sk.InvokeAsync(yamlPrompts["Transcription_RootCause"], new() { { "transcription", transcription } });
         return result.GetValue<string>();

      }
   }
}

