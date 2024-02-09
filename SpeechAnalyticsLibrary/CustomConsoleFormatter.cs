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

         var messages = logEntry.State.ToString().Split("|", StringSplitOptions.RemoveEmptyEntries);
         string parsedMessage = "";

         if (logEntry.LogLevel != LogLevel.Information)
         {
            Console.Write("[");
            Console.ForegroundColor = color;
            Console.Write($"{level}");
            Console.ResetColor();
            Console.Write("] ");
         }
         foreach (var msg in messages)
         {
            (Console.ForegroundColor, parsedMessage) = GetLogEntryColor(msg);
            Console.Write($"{parsedMessage} ");
            Console.ResetColor();
         }
         Console.WriteLine();

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
         logger.LogInformation(FormatMessage(message, color));
      }

      public static void LogDebug(this ILogger logger, string message, ConsoleColor color)
      {
         logger.LogDebug(FormatMessage(message, color));
      }

      public static void LogError(this ILogger logger, string message, ConsoleColor color)
      {
         logger.LogError(FormatMessage(message, color));
      }

      public static void LogWarning(this ILogger logger, string message, ConsoleColor color)
      {
         logger.LogWarning(FormatMessage(message, color));
      }

      public static void LogCritical(this ILogger logger, string message, ConsoleColor color)
      {
         logger.LogCritical(FormatMessage(message, color));
      }

      public static void LogTrace(this ILogger logger, string message, ConsoleColor color)
      {
         logger.LogTrace(FormatMessage(message, color));
      }

      public static void LogInformation(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogInformation(FormatMessages(messages));
      }

      public static void LogDebug(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogDebug(FormatMessages(messages));
      }

      public static void LogError(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogError(FormatMessages(messages));
      }

      public static void LogWarning(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogWarning(FormatMessages(messages));
      }

      public static void LogCritical(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogCritical(FormatMessages(messages));
      }

      public static void LogTrace(this ILogger logger, Dictionary<string, ConsoleColor> messages)
      {
         logger.LogTrace(FormatMessages(messages));
      }
      private static string FormatMessages(Dictionary<string, ConsoleColor> messages)
      {
         var formattedMessages = string.Empty;
         foreach (var message in messages)
         {
            formattedMessages += $"{message.Key} **COLOR:{message.Value.ToString()}|";
         }
         return formattedMessages;
      }
      private static string FormatMessage(string message, ConsoleColor color)
      {
         return message + " **COLOR:" + color.ToString();
      }

   }
}

