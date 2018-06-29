using Autofac;
using JetBrains.Annotations;
using Topshelf.HostConfigurators;

namespace Topshelf.Quartz.Autofac
{
    /// <summary>
    /// Allows to use Quartz with Autofac within topshelf configurator
    /// </summary>
    public static class AutofacScheduleJobHostConfiguratorExtensions
    {
        /// <summary>
        /// Bind Quartz to Topshelf host configurator and Autofac
        /// </summary>
        /// <param name="configurator">Topshelf host configurator</param>
        /// <param name="lifetimeScope">Autofac lifetime scope</param>
        /// <returns>Topshelf host configurator</returns>
        [NotNull]
        public static HostConfigurator UseQuartzAutofac([NotNull] this HostConfigurator configurator, [NotNull] ILifetimeScope lifetimeScope)
        {
            AutofacScheduleJobServiceConfiguratorExtensions.SetupAutofac(lifetimeScope);
            return configurator;
        }
    }
}