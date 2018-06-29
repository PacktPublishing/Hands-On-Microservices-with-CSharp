using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem
{
    using System.Diagnostics;
    using ReflectSoftware.Insight;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   The milliseconds base logger. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.ILogger"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class MSBaseLogger : ILogger
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes a line in color. </summary>
        ///
        /// <param name="message">          The message. </param>
        /// <param name="foregroundColor">  The foreground color. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WriteLineInColor(string message, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Logs an information. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogInformation(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogInformation(string message)
        {
            RILogManager.Default?.SendInformation(message);
            WriteLineInColor(message, ConsoleColor.White);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Logs a warning. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogWarning(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogWarning(string message)
        {
            RILogManager.Default?.SendWarning(message);
            WriteLineInColor(message, ConsoleColor.Yellow);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Loads an error. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogError(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogError(string message)
        {
            RILogManager.Default?.SendError(message);
            WriteLineInColor(message, ConsoleColor.Red);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Loads an exception. </summary>
        ///
        /// <param name="message">  The message. </param>
        /// <param name="ex">       The ex. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogException(string,Exception)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogException(string message, Exception ex)
        {
            RILogManager.Default?.SendException(message, ex);
            WriteLineInColor(message, ConsoleColor.Red);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Logs a debug. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogDebug(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogDebug(string message)
        {
            RILogManager.Default?.SendDebug(message);
            WriteLineInColor(message, ConsoleColor.Blue);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Logs a trace. </summary>
        ///
        /// <param name="message">  The message. </param>
        ///
        /// <seealso cref="M:MicroServiceEcoSystem.ILogger.LogTrace(string)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void LogTrace(string message)
        {
            RILogManager.Default?.SendTrace(message);
            WriteLineInColor(message, ConsoleColor.Cyan);
            Trace.WriteLine(message);
        }
    }
}
