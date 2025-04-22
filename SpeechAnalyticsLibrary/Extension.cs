using DocumentFormat.OpenXml.Drawing.Charts;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SpeechAnalyticsLibrary
{
   public static class Extensions
   {
      /// <summary>  
      /// This finds a full JSON document inside of a string.   
      /// Used just in case the LLM returns extra characters at the beginning or end of the JSON document.  
      /// </summary>  
      /// <param name="input"></param>  
      /// <returns></returns>  
      public static string ExtractJson(this string input)
      {
         var regex = new Regex(@"{[^{}]*}");
         if (regex.IsMatch(input))
         {
            return regex.Match(input).Value;
         }
         else
         {
            return input;
         }
      }

      public static string CleanMd(this string input)
      {
         var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
         var cleaned = lines.Where(line => !line.TrimStart().StartsWith("```"));
         return string.Join(Environment.NewLine, cleaned);
      }
   }
}
