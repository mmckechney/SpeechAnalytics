using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using SpeechAnalytics.Models;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpeechAnalytics
{
   public class SkAi
   {
      private IConfiguration _config;
      private Kernel sk;
      private ILogger log;
      ILoggerFactory loggerFactory;
      Dictionary<string, KernelFunction> yamlPrompts = new();
      AzureOpenAi aiSettings;
      HttpClient _client;

      public SkAi(ILogger<SkAi> log, IConfiguration config, ILoggerFactory loggerFactory, AzureOpenAi aiSettings, LogLevel logLevel)
      {
         _config = config;
         this.log = log;
         this.loggerFactory = LoggerFactory.Create(builder =>
         {
            builder.SetMinimumLevel(logLevel);
            builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
            builder.AddConsole(options =>
            {
               options.FormatterName = "custom";

            });
           // builder.AddFilter("Microsoft", LogLevel.Warning);
            //builder.AddFilter("System", LogLevel.Warning);
         }); ;
         this.aiSettings = aiSettings;
         InitPlugins();
      }

      private bool InitPlugins()
      {
         HttpClientHandler handler = new HttpClientHandler()
         {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
         };

         _client = new HttpClient(handler)
         {
            Timeout = TimeSpan.FromSeconds(120),
         };

         if (aiSettings != null)
         {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton(loggerFactory);
            builder.AddAzureOpenAIChatCompletion(deploymentName: aiSettings.ChatDeploymentName, modelId: aiSettings.ChatModel, endpoint: aiSettings.EndPoint, apiKey: aiSettings.Key, httpClient: _client);
            
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
      internal async Task<string> GetTranscriptionInsights(string transcription, string callid)
      {
         try
         {
            var result = await this.sk.InvokeAsync(yamlPrompts["Transcription_Insights"], new() { { "transcription", transcription }, { "callid", callid } });
            return result.GetValue<string>();
         }
         catch (HttpOperationException httpExe)
         {
            string seconds = "XX";
            var resp = JsonSerializer.Deserialize<HttpOperationExceptionResponseContent>(httpExe.ResponseContent);
            if (resp.error.Code == "429")
            {
               Regex regex = new Regex(@"(\d{1,3})\sseconds");
               Match match = regex.Match(resp.error.Message);
               if (match.Success)
               {
                  seconds = match.Groups[1].Value; // This will contain the extracted seconds value
               }
               log.LogError($"Too many requests to Azure Open AI. Available again in {seconds} seconds");
            }
            return transcription;
         }
         catch (Exception exe)
         {

            log.LogError($"Error getting insights: {exe.Message}");
            return "";
         }
      }

      internal async Task<Dictionary<string, string>?> GetSpeakerNames(string sourceFileName, string transcription)
      {
         Dictionary<string, string> speakerDict = null;
         try
         {
            var result = await this.sk.InvokeAsync(yamlPrompts["Transcription_SpeakerId"], new() { { "transcription", transcription } });
            try
            {
               speakerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(result.GetValue<string>());
               return speakerDict;
            }
            catch (Exception exe)
            {
               log.LogError($"Error getting speaker names: {exe.Message}");
               return speakerDict;
            }  
         }
         catch(HttpOperationException httpExe)
         {
            if(httpExe.ResponseContent == null)
            {
               log.LogError(httpExe.Message);
               return speakerDict;
            }
            string seconds ="XX";
            var resp = JsonSerializer.Deserialize<HttpOperationExceptionResponseContent>(httpExe.ResponseContent);
            if(resp.error.Code == "429")
            {
               Regex regex = new Regex(@"(\d{1,3})\sseconds");
               Match match = regex.Match(resp.error.Message);
               if (match.Success)
               {
                   seconds = match.Groups[1].Value; // This will contain the extracted seconds value
               }
               log.LogError($"Too many requests to Azure Open AI. Unable to format transcript as a conversation. Sleeping for {seconds} seconds");
               Thread.Sleep(int.Parse(seconds) * 1000);
               return await GetSpeakerNames(sourceFileName, transcription);
            }
            return speakerDict;
         }
         catch(KernelFunctionCanceledException kExe)
         {
            log.LogError(kExe.Message);
         }
         catch (Exception exe)
         {
            log.LogError($"Error formatting conversation: {exe.Message}");
         }
         return speakerDict;
      }
   }
}

