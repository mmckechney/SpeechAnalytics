using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using SpeechAnalyticsLibrary;

namespace SpeechAnalyticsLibrary
{

   public sealed class CustomConsoleFormatter : ConsoleFormatter
   {
      public CustomConsoleFormatter() : base("custom")
      {
      }

      public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
      {
         (var color, var level) = LogLevelShort(logEntry.LogLevel);
         switch (logEntry.LogLevel)
         {
            case LogLevel.Information:
               (Console.ForegroundColor, string message) = GetLogEntryColor(logEntry.State.ToString());
               Console.WriteLine($"{message}");
               Console.ResetColor();
               break;
            default:
               Console.Write("[");
               Console.ForegroundColor = color;
               Console.Write($"{level}");
               Console.ResetColor();
               Console.Write("] ");
               (Console.ForegroundColor, string message1) = GetLogEntryColor(logEntry.State.ToString());
               Console.WriteLine($"{message1}");
               Console.ResetColor();
               break;
         }
      }
      private (ConsoleColor, string) LogLevelShort(LogLevel level)
      {
         switch (level)
         {
            case LogLevel.Trace:
               return (ConsoleColor.Blue, "TRC");
            case LogLevel.Debug:
               return (ConsoleColor.Blue, "DBG");
            case LogLevel.Information:
               return (ConsoleColor.White, "INF");
            case LogLevel.Warning:
               return (ConsoleColor.DarkYellow, "WRN");
            case LogLevel.Error:
               return (ConsoleColor.Red, "ERR");
            case LogLevel.Critical:
               return (ConsoleColor.DarkRed, "CRT");
            default:
               return (ConsoleColor.Cyan, "UNK");

         }
      }
      public (ConsoleColor color, string message) GetLogEntryColor(string message)
      {
         var color = ConsoleColor.White;
         if (message.Contains("**COLOR:"))
         {
            var colorString = message.Split("**COLOR:")[1];
            if (Enum.TryParse(colorString, out color))
            {
               return (color, message.Split("**COLOR:")[0].Trim());
            }
         }
         return (color, message);
      }

   }
   public static class ILoggerExtensions
   {
      public static void LogInformation(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogInformation(message);
      }

      public static void LogDebug(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogDebug(message);
      }

      public static void LogError(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogError(message);
      }

      public static void LogWarning(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogWarning(message);
      }

      public static void LogCritical(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogCritical(message);
      }

      public static void LogTrace(this ILogger logger, string message, ConsoleColor color)
      {
         message = message + " **COLOR:" + color.ToString();
         logger.LogTrace(message);
      }

   }
}

