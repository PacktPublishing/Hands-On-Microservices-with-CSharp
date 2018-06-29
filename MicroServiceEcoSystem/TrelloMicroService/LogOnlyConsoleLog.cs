using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrelloMicroService
{
    using Manatee.Trello;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A local only console log. </summary>
    ///
    /// <seealso cref="T:Manatee.Trello.ILog"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class LocalOnlyConsoleLog : ILog
    {
        /// <summary>   True to provide output. </summary>
        private static readonly bool ProvideOutput;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes static members of the TrelloMicroService.LocalOnlyConsoleLog class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static LocalOnlyConsoleLog()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("TRELLO_TEST_OUTPUT"), out ProvideOutput);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes a debug level log entry. </summary>
        ///
        /// <param name="message">      The message or message format. </param>
        /// <param name="parameters">   A list of parameters. </param>
        ///
        /// <seealso cref="M:Manatee.Trello.ILog.Debug(string,params object[])"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Debug(string message, params object[] parameters)
        {
            if (!ProvideOutput) return;

            var output = $"Debug: {message}";
            Post(output);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes an information level log entry. </summary>
        ///
        /// <param name="message">      The message or message format. </param>
        /// <param name="parameters">   A list of paramaters. </param>
        ///
        /// <seealso cref="M:Manatee.Trello.ILog.Info(string,params object[])"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Info(string message, params object[] parameters)
        {
            if (!ProvideOutput) return;

            var output = string.Format($"Info: {message}", parameters);
            Post(output);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes an error level log entry. </summary>
        ///
        /// <param name="e">    The exception that will be or was thrown. </param>
        ///
        /// <seealso cref="M:Manatee.Trello.ILog.Error(Exception)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Error(Exception e)
        {
            if (!ProvideOutput) return;

            var output = BuildMessage($"Error: An exception of type {e.GetType().Name} occurred:",
                                      e.Message,
                                      e.StackTrace);
            Post(output);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Builds a message. </summary>
        ///
        /// <param name="lines">    A variable-length parameters list containing lines. </param>
        ///
        /// <returns>   A string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static string BuildMessage(params string[] lines)
        {
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Post this message. </summary>
        ///
        /// <param name="output">   The output. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void Post(string output)
        {
            Console.WriteLine(output);
        }
    }
}
