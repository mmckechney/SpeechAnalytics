using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechAnalyticsLibrary
{
#pragma warning disable SKEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
   public class FunctionFilter : IFunctionFilter
   {
      ILogger<FunctionFilter> log;
      public FunctionFilter(ILogger<FunctionFilter> log)
      {
         this.log = log;
      }
      public void OnFunctionInvoked(FunctionInvokedContext context)

      {
         log.LogDebug($"{Environment.NewLine}INVOKED :{context.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, context.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}{Environment.NewLine}Result:{context.Result}");
      }

      public void OnFunctionInvoking(FunctionInvokingContext context)
      {
         log.LogDebug($"{Environment.NewLine}INVOKING :{context.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, context.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}");
      }
   }
}
