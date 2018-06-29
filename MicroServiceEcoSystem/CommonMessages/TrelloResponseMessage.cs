using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMessages
{
    using EasyNetQ;

    /// <summary>   (Serializable) a trello response message. </summary>
    [Serializable]
    [Queue("Trello", ExchangeName = "EvolvedAI")]
    public class TrelloResponseMessage
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets a value indicating whether the success. </summary>
        ///
        /// <value> True if success, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool Success { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Message { get; set; }
    }
}
