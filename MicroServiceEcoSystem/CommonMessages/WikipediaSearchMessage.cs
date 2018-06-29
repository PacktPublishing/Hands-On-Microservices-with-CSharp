using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonMessages
{
    using EasyNetQ;

    /// <summary>   (Serializable) a wikipedia search message. </summary>
    [Queue("Speech", ExchangeName = "EvolvedAI")]
    [Serializable]
    public class WikipediaSearchMessage
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the maximum returns. </summary>
        ///
        /// <value> The maximum returns. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int maxReturns { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the search term. </summary>
        ///
        /// <value> The search term. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string searchTerm { get; set; }
    }
}
