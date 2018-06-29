using System.Collections.Generic;
using Quartz;

namespace Scheduling_MicroService
{
    /// <summary>   A quartz job listener configuration. </summary>
    public class QuartzJobListenerConfig
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the listener. </summary>
        ///
        /// <value> The listener. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IJobListener Listener { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the matchers. </summary>
        ///
        /// <value> The matchers. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<IMatcher<JobKey>> Matchers { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the Scheduling_MicroService.QuartzJobListenerConfig class.
        /// </summary>
        ///
        /// <param name="listener"> The listener. </param>
        /// <param name="matchers"> A variable-length parameters list containing matchers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzJobListenerConfig(IJobListener listener, params IMatcher<JobKey>[] matchers)
        {
            Listener = listener;
            Matchers = matchers;
        }

    }
}