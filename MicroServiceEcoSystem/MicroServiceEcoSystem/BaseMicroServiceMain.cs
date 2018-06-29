using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem
{
    using System.IO;
    using System.Timers;
    using Autofac;
    using JetBrains.Annotations;
    using log4net;
    using log4net.Config;
    using Topshelf;
    using Topshelf.Autofac;
    using Topshelf.Diagnostics;

    public static class BaseMicroServiceMain<T> where T : class
    {

        public static bool Run([NotNull] string displayName, [NotNull] string serviceName, [NotNull] string description)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SampleDependency>()?.As<ISampleDependency>();
            builder.RegisterType<T>()
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
            builder.RegisterModule<Logger>();
            var container = builder.Build();

            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c?.UseAutofacContainer(container);
                c?.UseLog4Net();
                c?.ApplyCommandLineWithDebuggerSupport();
                c?.EnablePauseAndContinue();
                c?.EnableShutdown();
                c?.OnException(ex =>
                {
                });


                c?.Service<T>(s =>
                {
                    s.ConstructUsingAutofacContainer<T>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<T>();
                        return service;
                    });
                    s?.ConstructUsing(name => new T());

                    s?.WhenStarted((T server, HostControl host) => OnStart(host));
                    s?.WhenPaused(server => OnPause());
                    s?.WhenContinued(server => OnResume());
                    s?.WhenStopped(server => OnStop());
                    s?.WhenShutdown(service => OnShutdown());
                });



                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern(description));
                c?.SetDisplayName(string.Intern(displayName));
                c?.SetServiceName(string.Intern(serviceName));

                c?.EnableServiceRecovery(r =>
                {
                    r?.OnCrashOnly();
                    r?.RestartService(1); //first
                    r?.RestartService(1); //second
                    r?.RestartService(1); //subsequents
                    r?.SetResetPeriod(0);
                });
            });
            return (true);
        }
    }
}
