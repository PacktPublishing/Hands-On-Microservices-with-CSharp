using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMessages
{
    using EasyNetQ;

    /// <summary>   (Serializable) the bitcoin spend request message. </summary>
    [Queue("Bitcoin", ExchangeName = "EvolvedAI")]
    [Serializable]
    public class BitcoinSpendMessage
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the amount. </summary>
        ///
        /// <value> The amount. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public decimal amount { get; set; }
    }
}
