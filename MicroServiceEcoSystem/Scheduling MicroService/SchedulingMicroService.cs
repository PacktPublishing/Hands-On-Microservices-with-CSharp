using Quartz;
using Serilog;

namespace MicroServiceEcoSystem
{
    using System;
    using CommonMessages;
    using JetBrains.Annotations;
    using Quartz.Impl;
    using Topshelf;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A scheduling micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{MicroServiceEcoSystem.SchedulingMicroService, CommonMessages.HealthStatusMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class SchedulingMicroService : BaseMicroService<SchedulingMicroService, HealthStatusMessage>
    {
        /// <summary>   The job scheduler. </summary>
        private readonly IScheduler _jobScheduler;
        /// <summary>   The logger. </summary>
        private readonly ILogger _logger;
        /// <summary>   The hc. </summary>
        private HostControl _hc;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.SchedulingMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public SchedulingMicroService()
        {
            Name = "Scheduling Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.SchedulingMicroService class.
        /// </summary>
        ///
        /// <param name="jobScheduler"> The job scheduler. This cannot be null. </param>
        /// <param name="logger">       The logger. This cannot be null. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public SchedulingMicroService([NotNull] IScheduler jobScheduler, [NotNull] ILogger logger)
            : base()
        {

            _jobScheduler = jobScheduler;
            _logger = logger;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="host"> The host. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStart([CanBeNull] HostControl host)
        {
            base.Start(host);
            _hc = host;
            _jobScheduler?.Start();
            //_logger?.SendInformation(string.Intern("Job scheduler started"));

            // construct a scheduler factory
            ISchedulerFactory schedFact = new StdSchedulerFactory();

            // get a scheduler
            IScheduler sched = schedFact.GetScheduler().Result;
            sched.Start();

            // define the job and tie it to our HelloJob class
            IJobDetail job = JobBuilder.Create<SampleJob>()
                .WithIdentity("myJob", "group1")
                .Build();

            // Trigger the job to run now, and then every 40 seconds
            ITrigger trigger = TriggerBuilder.Create()
              .WithIdentity("myTrigger", "group1")
              .StartNow()
              .WithSimpleSchedule(x => x
                  .WithIntervalInSeconds(40)
                  .RepeatForever())
              .Build();

            sched.ScheduleJob(job, trigger);

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            base.Stop();
            _jobScheduler?.Shutdown(true);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            return true;
        }
    }
}