using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechAnalyticsLibrary
{
#pragma warning disable SKEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
   public class FunctionInvocationFilter : IFunctionInvocationFilter
   {
      ILogger<FunctionInvocationFilter> log;
      public FunctionInvocationFilter(ILogger<FunctionInvocationFilter> log)
      {
         this.log = log;
      }
      public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
      {
         log.LogDebug($"{Environment.NewLine}INVOKING :{context.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, context.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}");
         await next(context);
         log.LogDebug($"{Environment.NewLine}INVOKED :{context.Function.Name}{Environment.NewLine}Arguments:{Environment.NewLine}{string.Join(Environment.NewLine, context.Arguments.Select(a => a.Key + ":" + a.Value.ToString()))}{Environment.NewLine}Result:{context.Result}");
      }

   }
}
