using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpeechAnalytics
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
         if(regex.IsMatch(input))
         {
            return regex.Match(input).Value;
         }else
         {
            return input;
         }
      }
   }
}
