using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeechAnalyticsLibrary;
using SpeechAnalyticsLibrary.Models;

namespace DocumentQuestionsFunction
{
   public class Program
   {
      public static AnalyticsSettings settings;

      static async Task Main(string[] args)
      {
         string basePath = IsDevelopmentEnvironment() ?
            Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot") :
            $"{Environment.GetEnvironmentVariable("HOME")}\\site\\wwwroot";

         var builder = new HostBuilder()
             .ConfigureFunctionsWorkerDefaults();


         builder.ConfigureLogging((hostContext, logging) =>
            {
               logging.SetMinimumLevel(LogLevel.Debug);
               logging.AddFilter("System", LogLevel.Warning);
               logging.AddFilter("Microsoft", LogLevel.Warning);
            });

         builder.ConfigureAppConfiguration(b =>
            {
               b.SetBasePath(basePath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)  // common settings go here.
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)  // environment specific settings go here
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)  // secrets go here. This file is excluded from source control.
               .AddEnvironmentVariables()
               .Build();

            });
         builder.ConfigureServices(ConfigureServices);

         var host = builder.Build();

         await host.RunAsync();
      }

      public static bool IsDevelopmentEnvironment()
      {
         return "Development".Equals(Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT"), StringComparison.OrdinalIgnoreCase);
      }
      private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
      {
         services.AddApplicationInsightsTelemetryWorkerService();
         services.ConfigureFunctionsApplicationInsights();
         services.AddSingleton<SemanticMemory>();
         services.AddSingleton<BatchTranscription>();
         services.AddSingleton<IdentityHelper>();
         services.AddSingleton<FileHandling>();
         services.AddSingleton<SkAi>();
         services.AddSingleton<CosmosHelper>();
         services.AddSingleton<SpeechDiarization>();

       services.AddSingleton<AnalyticsSettings>(sp =>
         {
            var config = sp.GetRequiredService<IConfiguration>();
            var settings = new AnalyticsSettings();
            config.Bind(settings);
            Program.settings = settings;
            return settings;
         });

         services.AddHttpClient();

      }
   }
}
