using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedFileMonitoringMicroservice
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Exception for signalling event queue overflow errors. </summary>
    ///
    /// <seealso cref="T:System.Exception"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    class EventQueueOverflowException : Exception
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the
        /// AdvancedFileMonitoringMicroservice.EventQueueOverflowException class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public EventQueueOverflowException()
            : base() { }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the
        /// AdvancedFileMonitoringMicroservice.EventQueueOverflowException class.
        /// </summary>
        ///
        /// <param name="message">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public EventQueueOverflowException(string message)
            : base(message) { }
    }
}
