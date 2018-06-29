using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Specialized;
using Quartz.Impl;

namespace MicroServiceEcoSystem
{
    using System.Threading;
    using NodaTime;
    using Serilog;
    using Quartz;
    using Scheduling_MicroService;

    /// <summary>   Interface for scheduled job. </summary>
    public interface IScheduledJob
    {
        /// <summary>   Runs this object. </summary>
        void Run();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A sample job. </summary>
    ///
    /// <seealso cref="T:Quartz.IJob"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class SampleJob : IJob
    {
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
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.IJob.Execute(IJobExecutionContext)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("The current time is: {0}", SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime());
            return Task.CompletedTask;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A scheduled job. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.IScheduledJob"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    class ScheduledJob : IScheduledJob
    {
        /// <summary>   Runs this object. </summary>
        public void Run()
        {
            // Get an instance of the Quartz.Net scheduler
            var schd = GetScheduler();

            // Start the scheduler if its in standby
            if (!schd.IsStarted)
                schd.Start();

            // Define the Job to be scheduled
            var job = JobBuilder.Create<HelloWorldJob>()
                .WithIdentity("WriteHelloToLog", "IT")
                .RequestRecovery()
                .Build();

            // Associate a trigger with the Job
            var trigger = (ICronTrigger)TriggerBuilder.Create()
                .WithIdentity("WriteHelloToLog", "IT")
                .WithCronSchedule("0 0/1 * 1/1 * ? *") // visit http://www.cronmaker.com/ Queues the job every minute
                .StartAt(DateTime.UtcNow)
                .WithPriority(1)
                .Build();

            // Validate that the job doesn't already exists
            if (schd.CheckExists(new JobKey("WriteHelloToLog", "IT")).Result)
            {
                schd.DeleteJob(new JobKey("WriteHelloToLog", "IT"));
            }

            var schedule = schd.ScheduleJob(job, trigger);
            Console.WriteLine("Job '{0}' scheduled for '{1}'", "WriteHelloToLog", schedule.Result.ToString("r"));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get an instance of the Quartz.Net scheduler. </summary>
        ///
        /// <returns>   The scheduler. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static IScheduler GetScheduler()
        {
            try
            {
                var properties = new NameValueCollection
                {
                    ["quartz.scheduler.instanceName"] = "ServerScheduler",
                    ["quartz.scheduler.proxy"] = "true",
                    ["quartz.scheduler.proxy.address"] = $"tcp://{"localhost"}:{"555"}/{"QuartzScheduler"}"
                };

                // Get a reference to the scheduler
                var sf = new StdSchedulerFactory(properties);

                return sf.GetScheduler().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Scheduler not available: '{0}'", ex.Message);
                throw;
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A simple schedule listener. This class cannot be inherited. </summary>
    ///
    /// <seealso cref="T:Quartz.ISchedulerListener"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class SimpleScheduleListener : ISchedulerListener
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// is scheduled.
        /// </summary>
        ///
        /// <param name="trigger">  The trigger. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobScheduled(ITrigger,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobScheduled(ITrigger trigger, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Scheduled from trigger: " + trigger.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// is unscheduled.
        /// </summary>
        ///
        /// <param name="triggerKey">   The trigger key. </param>
        /// <param name="token">        The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobUnscheduled(TriggerKey,CancellationToken)"/>
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulingDataCleared(System.Threading.CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobUnscheduled(TriggerKey triggerKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Unscheduled from trigger: " + triggerKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has reached the condition in which it will never fire again.
        /// </summary>
        ///
        /// <param name="trigger">  The trigger. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.TriggerFinalized(ITrigger,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerFinalized(ITrigger trigger, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger Finalized from trigger: " + trigger.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> a <see cref="T:Quartz.ITrigger" />s has been
        /// paused.
        /// </summary>
        ///
        /// <param name="triggerKey">   The trigger key. </param>
        /// <param name="token">        The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.TriggerPaused(TriggerKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerPaused(TriggerKey triggerKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger Paused from trigger: " + triggerKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> a group of
        /// <see cref="T:Quartz.ITrigger" />s has been paused.
        /// </summary>
        ///
        /// <remarks>
        /// If a all groups were paused, then the <see param="triggerName" /> parameter will be null.
        /// </remarks>
        ///
        /// <param name="triggerGroup"> The trigger group. </param>
        /// <param name="token">        The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.TriggersPaused(string,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggersPaused(string triggerGroup, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger Paused for group: " + triggerGroup);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has been un-paused.
        /// </summary>
        ///
        /// <param name="triggerKey">   The trigger key. </param>
        /// <param name="token">        The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.TriggerResumed(TriggerKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerResumed(TriggerKey triggerKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger Paused from trigger: " + triggerKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a group of
        /// <see cref="T:Quartz.ITrigger" />s has been un-paused.
        /// </summary>
        ///
        /// <remarks>
        /// If all groups were resumed, then the <see param="triggerName" /> parameter will be null.
        /// </remarks>
        ///
        /// <param name="triggerGroup"> The trigger group. </param>
        /// <param name="token">        The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.TriggersResumed(string,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggersResumed(string triggerGroup, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger Resumed for group: " + triggerGroup);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been added.
        /// </summary>
        ///
        /// <param name="jobDetail">    The job detail. </param>
        /// <param name="token">        The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobAdded(IJobDetail,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobAdded(IJobDetail jobDetail, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Added: " + jobDetail.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been deleted.
        /// </summary>
        ///
        /// <param name="jobKey">   The job key. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobDeleted(JobKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobDeleted(JobKey jobKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Deleted: " + jobKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been interrupted.
        /// </summary>
        ///
        /// <param name="jobKey">   The job key. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobInterrupted(JobKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobInterrupted(JobKey jobKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Interrupted: " + jobKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been  paused.
        /// </summary>
        ///
        /// <param name="jobKey">   The job key. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobPaused(JobKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobPaused(JobKey jobKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Paused: " + jobKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a group of
        /// <see cref="T:Quartz.IJobDetail" />s has been  paused.
        /// <para>
        /// If all groups were paused, then the <see param="jobName" /> parameter will be null. If all
        /// jobs were paused, then both parameters will be null.
        /// </para>
        /// </summary>
        ///
        /// <param name="jobGroup"> The job group. </param>
        /// <param name="token">    The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobsPaused(string,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobsPaused(string jobGroup, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Jobs Paused for group: " + jobGroup);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been  un-paused.
        /// </summary>
        ///
        /// <param name="jobKey">   The job key. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobResumed(JobKey,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobResumed(JobKey jobKey, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job Resumed: " + jobKey.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// has been  un-paused.
        /// </summary>
        ///
        /// <param name="jobGroup"> The job group. </param>
        /// <param name="token">    The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.JobsResumed(string,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobsResumed(string jobGroup, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Jobs Resumed for group: " + jobGroup);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a serious error has occurred within the
        /// scheduler - such as repeated failures in the <see cref="T:Quartz.Spi.IJobStore" />, or the
        /// inability to instantiate a <see cref="T:Quartz.IJob" /> instance when its
        /// <see cref="T:Quartz.ITrigger" /> has fired.
        /// </summary>
        ///
        /// <param name="msg">      The message. </param>
        /// <param name="cause">    Details of the exception. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerError(string,SchedulerException,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerError(string msg, SchedulerException cause, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler error: " + msg + " with exception: " + cause.Message);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that it has move to
        /// standby mode.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerInStandbyMode(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerInStandbyMode(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler is in standby");
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that it has started.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerStarted(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerStarted(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler started");
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that it is starting.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerStarting(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerStarting(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler starting...");
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that it has Shutdown.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerShutdown(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerShutdown(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler shutdown");
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that it has begun the
        /// shutdown sequence.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulerShuttingdown(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulerShuttingdown(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduler shutting down...");
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> to inform the listener that all jobs,
        /// triggers and calendars were deleted.
        /// </summary>
        ///
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ISchedulerListener.SchedulingDataCleared(CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task SchedulingDataCleared(CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Scheduling data cleared");
            return Task.CompletedTask;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A simple job listener. </summary>
    ///
    /// <seealso cref="T:Quartz.IJobListener"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class SimpleJobListener : IJobListener
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// is about to be executed (an associated <see cref="T:Quartz.ITrigger" />
        /// has occurred).
        /// <para>
        /// This method will not be invoked if the execution of the Job was vetoed by a
        /// <see cref="T:Quartz.ITriggerListener" />.
        /// </para>
        /// </summary>
        ///
        /// <param name="context">  The context. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.IJobListener.JobToBeExecuted(IJobExecutionContext,CancellationToken)"/>
        /// <seealso cref="M:Quartz.IJobListener.JobExecutionVetoed(Quartz.IJobExecutionContext,System.Threading.CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job is about to execute: " + context.JobDetail.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.IJobDetail" />
        /// was about to be executed (an associated <see cref="T:Quartz.ITrigger" />
        /// has occurred), but a <see cref="T:Quartz.ITriggerListener" /> vetoed it's execution.
        /// </summary>
        ///
        /// <param name="context">  The context. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.IJobListener.JobExecutionVetoed(IJobExecutionContext,CancellationToken)"/>
        /// <seealso cref="M:Quartz.IJobListener.JobToBeExecuted(Quartz.IJobExecutionContext,System.Threading.CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job execution vetoed: " + context.JobDetail.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> after a <see cref="T:Quartz.IJobDetail" />
        /// has been executed, and be for the associated <see cref="T:Quartz.Spi.IOperableTrigger" />'s
        /// <see cref="M:Quartz.Spi.IOperableTrigger.Triggered(Quartz.ICalendar)" /> method has been
        /// called.
        /// </summary>
        ///
        /// <param name="context">      The context. </param>
        /// <param name="jobException"> Details of the exception. </param>
        /// <param name="token">        The token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.IJobListener.JobWasExecuted(IJobExecutionContext,JobExecutionException,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Job was executed: " + context.JobDetail.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get the name of the <see cref="T:Quartz.IJobListener" />. </summary>
        ///
        /// <value> The name. </value>
        ///
        /// <seealso cref="P:Quartz.IJobListener.Name"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Name { get { return "SAMPLE JOB LISTENER"; } }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A simple trigger listener. </summary>
    ///
    /// <seealso cref="T:Quartz.ITriggerListener"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class SimpleTriggerListener : ITriggerListener
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has fired, and it's associated <see cref="T:Quartz.IJobDetail" />
        /// is about to be executed.
        /// <para>
        /// It is called before the
        /// <see cref="M:Quartz.ITriggerListener.VetoJobExecution(Quartz.ITrigger,Quartz.IJobExecutionContext,System.Threading.CancellationToken)" />
        /// method of this interface.
        /// </para>
        /// </summary>
        ///
        /// <param name="trigger">  The <see cref="T:Quartz.ITrigger" /> that has fired. </param>
        /// <param name="context">  The <see cref="T:Quartz.IJobExecutionContext" /> that will be passed
        ///                         to the
        ///                         <see cref="T:Quartz.IJob" />'s<see cref="M:Quartz.IJob.Execute(Quartz.IJobExecutionContext)" />
        ///                         method. </param>
        /// <param name="token">    The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ITriggerListener.TriggerFired(ITrigger,IJobExecutionContext,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger {0} fired for job {1}", trigger.Key.Name, context.JobDetail.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has fired, and it's associated <see cref="T:Quartz.IJobDetail" />
        /// is about to be executed.
        /// <para>
        /// It is called after the
        /// <see cref="M:Quartz.ITriggerListener.TriggerFired(Quartz.ITrigger,Quartz.IJobExecutionContext,System.Threading.CancellationToken)" />
        /// method of this interface.  If the implementation vetoes the execution (via returning
        /// <see langword="true" />), the job's execute method will not be called.
        /// </para>
        /// </summary>
        ///
        /// <param name="trigger">  The <see cref="T:Quartz.ITrigger" /> that has fired. </param>
        /// <param name="context">  The <see cref="T:Quartz.IJobExecutionContext" /> that will be passed
        ///                         to the
        ///                         <see cref="T:Quartz.IJob" />'s<see cref="M:Quartz.IJob.Execute(Quartz.IJobExecutionContext)" />
        ///                         method. </param>
        /// <param name="token">    The cancellation instruction. </param>
        ///
        /// <returns>   Returns true if job execution should be vetoed, false otherwise. </returns>
        ///
        /// <seealso cref="M:Quartz.ITriggerListener.VetoJobExecution(ITrigger,IJobExecutionContext,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task<bool> VetoJobExecution(ITrigger trigger, IJobExecutionContext context, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger {0} vetoed for job {1}", trigger.Key.Name, context.JobDetail.Key.Name);
            return Task.FromResult(false);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has misfired.
        /// <para>
        /// Consideration should be given to how much time is spent in this method, as it will affect all
        /// triggers that are misfiring.  If you have lots of triggers misfiring at once, it could be an
        /// issue it this method does a lot.
        /// </para>
        /// </summary>
        ///
        /// <param name="trigger">  The <see cref="T:Quartz.ITrigger" /> that has misfired. </param>
        /// <param name="token">    The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ITriggerListener.TriggerMisfired(ITrigger,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerMisfired(ITrigger trigger, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger {0} misfired", trigger.Key.Name);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        /// has fired, it's associated <see cref="T:Quartz.IJobDetail" />
        /// has been executed, and it's
        /// <see cref="M:Quartz.Spi.IOperableTrigger.Triggered(Quartz.ICalendar)" /> method has been
        /// called.
        /// </summary>
        ///
        /// <param name="trigger">                  The <see cref="T:Quartz.ITrigger" /> that was fired. </param>
        /// <param name="context">                  The <see cref="T:Quartz.IJobExecutionContext" /> that
        ///                                         was passed to the
        ///                                         <see cref="T:Quartz.IJob" />'s<see cref="M:Quartz.IJob.Execute(Quartz.IJobExecutionContext)" />
        ///                                         method. </param>
        /// <param name="triggerInstructionCode">   The result of the call on the
        ///                                         <see cref="T:Quartz.ITrigger" />'s<see cref="M:Quartz.Spi.IOperableTrigger.Triggered(Quartz.ICalendar)" />
        ///                                         method. </param>
        /// <param name="token">                    The cancellation instruction. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ///
        /// <seealso cref="M:Quartz.ITriggerListener.TriggerComplete(ITrigger,IJobExecutionContext,SchedulerInstruction,CancellationToken)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, 
            SchedulerInstruction triggerInstructionCode, CancellationToken token)
        {
            Console.WriteLine("SAMPLE: Trigger {0} completed for job {1} with code {2}", trigger.Key.Name, context.JobDetail.Key.Name, triggerInstructionCode);
            return Task.CompletedTask;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Get the name of the <see cref="T:Quartz.ITriggerListener" />. </summary>
        ///
        /// <value> The name. </value>
        ///
        /// <seealso cref="P:Quartz.ITriggerListener.Name"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Name { get { return "SAMPLE TRIGGER LISTENER"; } }
    }

}
