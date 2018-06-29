using System;
using System.Linq;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Topshelf.Logging;
using Topshelf.ServiceConfigurators;

namespace Scheduling_MicroService
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>   A schedule job service configurator extensions. </summary>
    public static class ScheduleJobServiceConfiguratorExtensions
	{
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Default scheduler factory. </summary>
        ///
        /// <returns>   The asynchronous result that yields an IScheduler. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static Task<IScheduler> DefaultSchedulerFactory ()
	   {
		   var schedulerFactory = new StdSchedulerFactory();
		   return schedulerFactory.GetScheduler();
	   }

        /// <summary>   The custom scheduler factory. </summary>
		private static Task<IScheduler> _customSchedulerFactory;
        /// <summary>   The scheduler. </summary>
		private static IScheduler Scheduler;
        /// <summary>   The job factory. </summary>
		internal static IJobFactory JobFactory;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the scheduler factory. </summary>
        ///
        /// <value> The asynchronous result that yields the scheduler factory. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static Task<IScheduler> SchedulerFactory
		{
			get { return _customSchedulerFactory ?? DefaultSchedulerFactory(); }
			set { _customSchedulerFactory = value; }
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the scheduler. </summary>
        ///
        /// <returns>   The scheduler. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		private static IScheduler GetScheduler()
		{
			var scheduler = SchedulerFactory.Result;
			
			if(JobFactory != null)
				scheduler.JobFactory = JobFactory;

			return scheduler;
		}

		public static ServiceConfigurator<T> UsingQuartzJobFactory<T, TJobFactory>(this ServiceConfigurator<T> configurator, Func<TJobFactory> jobFactory)
			where T : class
			where TJobFactory : IJobFactory
		{
			JobFactory = jobFactory();
			return configurator;
		}

		public static ServiceConfigurator<T> UsingQuartzJobFactory<T,TJobFactory>(this ServiceConfigurator<T> configurator) where T : class where TJobFactory : IJobFactory, new()
		{
			return UsingQuartzJobFactory(configurator, () => new TJobFactory());
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// A ServiceConfigurator&lt;T&gt; extension method that schedule quartz job.
        /// </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="configurator">     The configurator to act on. </param>
        /// <param name="jobConfigurator">  The job configurator. </param>
        /// <param name="replaceJob">       (Optional) True to replace job. </param>
        ///
        /// <returns>   A ServiceConfigurator&lt;T&gt; </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static ServiceConfigurator<T> ScheduleQuartzJob<T>(this ServiceConfigurator<T> configurator, Action<QuartzConfigurator> jobConfigurator, bool replaceJob = false) where T : class
		{
			ConfigureJob<T>(configurator, jobConfigurator, replaceJob);
			return configurator;
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure job. </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="configurator">     The configurator. </param>
        /// <param name="jobConfigurator">  The job configurator. </param>
        /// <param name="replaceJob">       (Optional) True to replace job. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		private static void ConfigureJob<T>(ServiceConfigurator<T> configurator, Action<QuartzConfigurator> jobConfigurator, bool replaceJob = false) where T : class
		{
			var log = HostLogger.Get(typeof(ScheduleJobServiceConfiguratorExtensions));

			var jobConfig = new QuartzConfigurator();
			jobConfigurator(jobConfig);

			if (jobConfig.JobEnabled == null || jobConfig.JobEnabled() || (jobConfig.Job == null || jobConfig.Triggers == null))
			{
				var jobDetail = jobConfig.Job();
				var jobTriggers = jobConfig.Triggers.Select(triggerFactory => triggerFactory()).Where(trigger => trigger != null);
                var jobListeners = jobConfig.JobListeners;
                var triggerListeners = jobConfig.TriggerListeners;
                var scheduleListeners = jobConfig.ScheduleListeners;
				configurator.BeforeStartingService((context) =>
					                                   {
														   log.Debug("[Topshelf.Quartz] Scheduler starting up...");
														   if (Scheduler == null)
															   Scheduler = GetScheduler();
														   
														   
															if (Scheduler != null && jobDetail != null && jobTriggers.Any())
															{
																var triggersForJob = new HashSet<ITrigger>(jobTriggers);
																Scheduler.ScheduleJob(jobDetail, triggersForJob, replaceJob);
																log.Info(string.Format("[Topshelf.Quartz] Scheduled Job: {0}", jobDetail.Key));
					
																foreach(var trigger in triggersForJob)
																	log.Info(string.Format("[Topshelf.Quartz] Job Schedule: {0} - Next Fire Time (local): {1}", trigger, trigger.GetNextFireTimeUtc().HasValue ? trigger.GetNextFireTimeUtc().Value.ToLocalTime().ToString() : "none"));

                                                                if (jobListeners.Any())
                                                                {
                                                                    foreach (var listener in jobListeners)
                                                                    {
                                                                        var config = listener();
                                                                        Scheduler.ListenerManager.AddJobListener(
                                                                            config.Listener, config.Matchers as IReadOnlyCollection<IMatcher<JobKey>>);
                                                                        log.Info(
                                                                            $"[Topshelf.Quartz] Added Job Listener: {config.Listener.Name}");
                                                                    }
                                                                }

                                                                if (triggerListeners.Any())
                                                                {
                                                                    foreach (var listener in triggerListeners)
                                                                    {
                                                                        var config = listener();
                                                                        Scheduler.ListenerManager.AddTriggerListener(config.Listener, config.Matchers as IReadOnlyCollection<IMatcher<TriggerKey>>);
                                                                        log.Info(
                                                                        string.Format(
                                                                            "[Topshelf.Quartz] Added Trigger Listener: {0}",
                                                                            config.Listener.Name));
                                                                    }
                                                                }
                                                                if (scheduleListeners.Any())
                                                                {
                                                                    foreach (var listener in scheduleListeners)
                                                                    {
                                                                        var schedListener = listener();
                                                                        Scheduler.ListenerManager.AddSchedulerListener(schedListener);
                                                                        string.Format(
                                                                            "[Topshelf.Quartz] Added Schedule Listener: {0}",
                                                                            schedListener.GetType());
                                                                    }

                                                                }

																Scheduler.Start();
																log.Info("[Topshelf.Quartz] Scheduler started...");
															}

					                                   });

				configurator.BeforeStoppingService((context) =>
						                {
											log.Debug("[Topshelf.Quartz] Scheduler shutting down...");
											if(Scheduler != null)
                                                if(!Scheduler.IsShutdown)
												    Scheduler.Shutdown();
											log.Info("[Topshelf.Quartz] Scheduler shut down...");
						                });

			}
		}
	}
}
