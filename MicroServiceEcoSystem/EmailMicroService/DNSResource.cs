using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailMicroService
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// The answer, authority, and additional sections all share the same format: a variable number
    /// of resource records.
    /// </summary>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class DnsResource
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// an owner name, i.e., the name of the node to which this resource record pertains.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Name;

        /// <summary>   Type of the query. </summary>
        public QueryType QueryType;

        /// <summary>   The query class. </summary>
        public QueryClass QueryClass;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// TTL - the time interval that the resource record may be cached before the source of the
        /// information should again be consulted. Zero values are interpreted to mean that the RR can
        /// only be used for the transaction in progress, and should not be cached.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Int32 TimeToLive;

        /// <summary>   the length in bytes of the RDATA field. </summary>
        public UInt16 DataLength;

        /// <summary>   The data. </summary>
        public byte[] Data;

        /// <summary>   The content. </summary>
        public object Content;
    }

}
