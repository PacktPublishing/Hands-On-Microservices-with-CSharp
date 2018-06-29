using System.Collections.Generic;
using Quartz;

namespace Scheduling_MicroService
{
    /// <summary>   A quartz trigger listener configuration. </summary>
    public class QuartzTriggerListenerConfig
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the listener. </summary>
        ///
        /// <value> The listener. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ITriggerListener Listener { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the matchers. </summary>
        ///
        /// <value> The matchers. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<IMatcher<TriggerKey>> Matchers { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the Scheduling_MicroService.QuartzTriggerListenerConfig
        /// class.
        /// </summary>
        ///
        /// <param name="listener"> The listener. </param>
        /// <param name="matchers"> A variable-length parameters list containing matchers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzTriggerListenerConfig(ITriggerListener listener, params IMatcher<TriggerKey>[] matchers)
        {
            Listener = listener;
            Matchers = matchers;
        }
    }
}