using System;
using System.Collections.Generic;
using Quartz;

namespace Scheduling_MicroService
{
    /// <summary>   A quartz configurator. </summary>
    public class QuartzConfigurator
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the job. </summary>
        ///
        /// <value> The job. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Func<IJobDetail> Job { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the triggers. </summary>
        ///
        /// <value> The triggers. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<Func<ITrigger>> Triggers { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the job listeners. </summary>
        ///
        /// <value> The job listeners. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<Func<QuartzJobListenerConfig>> JobListeners { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the trigger listeners. </summary>
        ///
        /// <value> The trigger listeners. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<Func<QuartzTriggerListenerConfig>> TriggerListeners { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the schedule listeners. </summary>
        ///
        /// <value> The schedule listeners. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IList<Func<ISchedulerListener>> ScheduleListeners { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the job enabled. </summary>
        ///
        /// <value> The job enabled. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Func<bool> JobEnabled { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the Scheduling_MicroService.QuartzConfigurator class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator()
        {
            Triggers = new List<Func<ITrigger>>();
            TriggerListeners = new List<Func<QuartzTriggerListenerConfig>>();
            JobListeners = new List<Func<QuartzJobListenerConfig>>();
            ScheduleListeners = new List<Func<ISchedulerListener>>();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   With job. </summary>
        ///
        /// <param name="jobDetail">    The job detail. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator WithJob(Func<IJobDetail> jobDetail)
        {
            Job = jobDetail;
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds a trigger. </summary>
        ///
        /// <param name="jobTrigger">   The job trigger. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator AddTrigger(Func<ITrigger> jobTrigger)
        {
            Triggers.Add(jobTrigger);
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   With triggers. </summary>
        ///
        /// <param name="jobTriggers">  The job triggers. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator WithTriggers(IEnumerable<ITrigger> jobTriggers)
        {
            foreach (var jobTrigger in jobTriggers)
            {
                var trigger = jobTrigger;
                AddTrigger(() => trigger);
            }
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enables the job when. </summary>
        ///
        /// <param name="jobEnabled">   The job enabled. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator EnableJobWhen(Func<bool> jobEnabled)
        {
            JobEnabled = jobEnabled;
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   With job listener. </summary>
        ///
        /// <param name="jobListener">  The job listener. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator WithJobListener(Func<QuartzJobListenerConfig> jobListener)
        {
            JobListeners.Add(jobListener);
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   With trigger listener. </summary>
        ///
        /// <param name="triggerListener">  The trigger listener. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator WithTriggerListener(Func<QuartzTriggerListenerConfig> triggerListener)
        {
            TriggerListeners.Add(triggerListener);
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   With schedule listener. </summary>
        ///
        /// <param name="scheduleListener"> The schedule listener. </param>
        ///
        /// <returns>   A QuartzConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public QuartzConfigurator WithScheduleListener(Func<ISchedulerListener> scheduleListener)
        {
            ScheduleListeners.Add(scheduleListener);
            return this;
        }
    }
}