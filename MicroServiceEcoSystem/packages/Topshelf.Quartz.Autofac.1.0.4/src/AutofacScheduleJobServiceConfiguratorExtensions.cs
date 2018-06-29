using System;
using Autofac;
using JetBrains.Annotations;
using Quartz;
using Topshelf.Logging;
using Topshelf.ServiceConfigurators;

namespace Topshelf.Quartz.Autofac
{
    /// <summary>
    /// Allows to use Quartz with Autofac within topshelf configurator
    /// </summary>
    public static class AutofacScheduleJobServiceConfiguratorExtensions
    {
        /// <summary>
        /// Bind Quartz to Topshelf service configurator and Autofac
        /// </summary>
        /// <param name="configurator">Topshelf service configurator</param>
        /// <param name="lifetimeScope">Autofac lifetime scope</param>
        /// <typeparam name="T">Type of host</typeparam>
        /// <returns>Topshelf service configurator</returns>
        public static ServiceConfigurator<T> UseQuartzAutofac<T>(this ServiceConfigurator<T> configurator, [NotNull] ILifetimeScope lifetimeScope)
            where T : class
        {
            SetupAutofac(lifetimeScope);
            return configurator;
        }

        internal static void SetupAutofac([NotNull] ILifetimeScope container)
        {
            Func<IScheduler> schedulerFactory = container.Resolve<IScheduler>;
            ScheduleJobServiceConfiguratorExtensions.SchedulerFactory = schedulerFactory;
            var log = HostLogger.Get(typeof(AutofacScheduleJobServiceConfiguratorExtensions));
            log.Info("[Topshelf.Quartz.Autofac] Quartz configured to construct jobs with Autofac.");
        }
    }
}