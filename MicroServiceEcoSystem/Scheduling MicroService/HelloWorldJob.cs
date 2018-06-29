using System;
using System.Threading.Tasks;

namespace Scheduling_MicroService
{
    using JetBrains.Annotations;
    using log4net;
    using Quartz;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A hello world job. </summary>
    ///
    /// <seealso cref="T:Quartz.IJob"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    class HelloWorldJob : IJob
    {
        /// <summary>   The log. </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(HelloWorldJob));

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the Scheduling_MicroService.HelloWorldJob class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public HelloWorldJob()
        {

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// fires that is associated with the <see cref="T:Quartz.IJob" />.
        /// </summary>
        ///
        /// <remarks>
        /// The implementation may wish to set a  result object on the JobExecutionContext before this
        /// method exits.  The result itself is meaningless to Quartz, but may be informative to
        /// <see cref="T:Quartz.IJobListener" />s or
        /// <see cref="T:Quartz.ITriggerListener" />s that are watching the job's
        /// execution.
        /// </remarks>
        ///
        /// <param name="context">  The execution context. </param>
        ///
        /// <returns>   The asynchronous result.. This will never be null. </returns>
        ///
        /// <seealso cref="M:Quartz.IJob.Execute(IJobExecutionContext)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        [NotNull]
        public Task Execute([NotNull] IJobExecutionContext context)
        {
            try
            {
                Log?.DebugFormat("{0}****{0}Job {1} fired @ {2} next scheduled for {3}{0}***{0}",
                    Environment.NewLine, context.JobDetail?.Key,
                    context.FireTimeUtc.LocalDateTime.ToString("r"),
                    context.NextFireTimeUtc?.ToString("r"));

                Console.WriteLine("{0}****{0}Job {1} fired @ {2} next scheduled for {3}{0}***{0}",
                    Environment.NewLine, context.JobDetail?.Key,
                    context.FireTimeUtc.LocalDateTime.ToString("r"),
                    context.NextFireTimeUtc?.ToString("r"));

                Log?.DebugFormat("{0}***{0}Hello World!{0}***{0}", Environment.NewLine);
                Console.WriteLine("{0}***{0}Hello World!{0}***{0}", Environment.NewLine);
            }
            catch (Exception ex)
            {
                Log?.DebugFormat("{0}***{0}Failed: {1}{0}***{0}", Environment.NewLine, ex.Message);
            }

            return Task.CompletedTask;
        }
    }
}
