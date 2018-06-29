using System;
using Quartz.Spi;
using Topshelf;
using Topshelf.HostConfigurators;

namespace Scheduling_MicroService
{
    /// <summary>   A schedule job host configurator extensions. </summary>
	public static class ScheduleJobHostConfiguratorExtensions
	{
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the new. </summary>
        ///
        /// <typeparam name="TJobFactory">  Type of the job factory. </typeparam>
        /// <param name="configurator"> The configurator to act on. </param>
        /// <param name="jobFactory">   The job factory. </param>
        ///
        /// <returns>   A HostConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static HostConfigurator UsingQuartzJobFactory<TJobFactory>(this HostConfigurator configurator, Func<TJobFactory> jobFactory)
			where TJobFactory : IJobFactory
		{
			ScheduleJobServiceConfiguratorExtensions.JobFactory = jobFactory();
			return configurator;
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the new. </summary>
        ///
        /// <value> The new. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static HostConfigurator UsingQuartzJobFactory<TJobFactory>(this HostConfigurator configurator)
			where TJobFactory : IJobFactory, new()
		{
			return UsingQuartzJobFactory(configurator, () => new TJobFactory());
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// A HostConfigurator extension method that schedule quartz job as service.
        /// </summary>
        ///
        /// <param name="configurator">     The configurator to act on. </param>
        /// <param name="jobConfigurator">  The job configurator. </param>
        /// <param name="replaceJob">       (Optional) True to replace job. </param>
        ///
        /// <returns>   A HostConfigurator. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

		public static HostConfigurator ScheduleQuartzJobAsService(this HostConfigurator configurator, Action<QuartzConfigurator> jobConfigurator, bool replaceJob = false)
		{
			configurator.Service<NullService>(s => s
													.ScheduleQuartzJob(jobConfigurator, replaceJob)									   
				                                    .WhenStarted(p => p.Start())
				                                    .WhenStopped(p => p.Stop())
													.ConstructUsing(settings => new NullService())
													);

			return configurator;
		}
	}
}
