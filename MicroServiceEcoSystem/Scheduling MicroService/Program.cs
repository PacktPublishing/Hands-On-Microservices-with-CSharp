using System;
using System.IO;
using Autofac;
using log4net.Config;
using Topshelf;
using Topshelf.Autofac;
using Topshelf.Diagnostics;


namespace Scheduling_MicroService
{
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Threading;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using Quartz;
    using Quartz.Impl.Matchers;
    using Topshelf.Quartz;
    using Logger = log4net.Repository.Hierarchy.Logger;

    /// <summary>   A program. </summary>
    public class Program  
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this application. </summary>
        ///
        /// <param name="args"> An array of command-line argument strings. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            // Service itself
            builder.RegisterType<SchedulingMicroService>()
                 .AsImplementedInterfaces()
                 .AsSelf()
                 ?.InstancePerLifetimeScope();
            builder.RegisterType<Logger>().SingleInstance();

            var container = builder.Build();

            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c?.UseAutofacContainer(container);
                c?.UseLog4Net();
                c?.ApplyCommandLineWithDebuggerSupport();
                c?.EnablePauseAndContinue();
                c?.EnableShutdown();
                c?.OnException(ex => { Console.WriteLine(ex.Message); });

                c.ScheduleQuartzJobAsService(q =>
                    q.WithJob(() => JobBuilder.Create<SampleJob>().Build())
                        .AddTrigger(() => TriggerBuilder.Create().WithSimpleSchedule(
                            build => build.WithIntervalInSeconds(30).RepeatForever()).Build())
                ).StartAutomatically();



                c?.Service<SchedulingMicroService>(s =>
                {
                    s.ConstructUsingAutofacContainer<SchedulingMicroService>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<SchedulingMicroService>();
                        return service;
                    });
                    s?.ConstructUsing(name => new SchedulingMicroService());

                    s?.WhenStarted((SchedulingMicroService server, HostControl host) => server.OnStart(host));
                    s?.WhenPaused(server => server.OnPause());
                    s?.WhenContinued(server => server.OnResume());
                    s?.WhenStopped(server => server.OnStop());
                    s?.WhenShutdown(server => server.OnShutdown());
                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("Scheduling Microservice Sample"));
                c?.SetDisplayName(string.Intern("SchedulingMicroService"));
                c?.SetServiceName(string.Intern("SchedulingMicroService"));

                c?.EnableServiceRecovery(r =>
                {
                    r?.OnCrashOnly();
                    r?.RestartService(1); //first
                    r?.RestartService(1); //second
                    r?.RestartService(1); //subsequents
                    r?.SetResetPeriod(0);
                });
            });
        }
    }
}