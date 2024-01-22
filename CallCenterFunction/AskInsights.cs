using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpeechAnalyticsLibrary;
using System.Net;
namespace CallCenterFunction
{
   public class AskInsights
   {
      private readonly ILogger log;
      private SkAi skAi;

      public AskInsights(ILoggerFactory loggerFactory, SkAi skAi)
      {
         log = loggerFactory.CreateLogger<AskInsights>();
         this.skAi = skAi;
      }

      [Function("AskInsights")]
      public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
      {
         log.LogInformation("C# HTTP trigger AskInsights function started");
         var response = req.CreateResponse(HttpStatusCode.OK);
         response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
         var question = await GetQuestion(req);
         string result = "";
         if (string.IsNullOrWhiteSpace(question))
         {
            result = "Please ask a question via GET querystring '?question=<question>' or POST with JSON body of { \"question\"=\"<question>\" }";
         }
         else
         {
            result = await skAi.AskQuestions(question);
         }
         response.WriteString(result); ;

         return response;
      }

      public async Task<string> GetQuestion(HttpRequestData req)
      {
         string question = req.Query["question"];
         string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
         log.LogInformation(requestBody);
         dynamic data = JsonConvert.DeserializeObject(requestBody);
         question = question ?? data?.question;

         log.LogInformation("question = " + question);

         return question;
      }
   }
}
