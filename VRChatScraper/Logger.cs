using Pastel;
using System;
using System.Drawing;

namespace VRChatScraper
{
    /// <summary>
    /// Represents a class for logging output.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs a message to output.
        /// </summary>
        /// <param name="type">The type of message and the corresponding tag & colour to output.</param>
        /// <param name="message">The message to be output.</param>
        public static void Log(LogType type, string message)
        {
            switch (type)
            {
                case LogType.Error:
                    Console.WriteLine($"{"[ERR]".Pastel(Color.Red)} {message}");
                    break;
                case LogType.Info:
                    Console.WriteLine($"{"[INF]".Pastel(Color.Green)} {message}");
                    break;
                case LogType.Debug:
                    Console.WriteLine($"{"[DEV]".Pastel(Color.Orange)} {message}");
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Represents a collection of options for logging.
    /// </summary>
    public enum LogType
    {
        Error,
        Info,
        Debug
    }
}
