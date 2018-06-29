namespace Deployment_Monitor
{
    using System;
    using System.IO;
    using Autofac;
    using log4net.Config;
    using log4net.Repository.Hierarchy;
    using MicroServiceEcoSystem.Deployment_Monitor;
    using Topshelf;

    using Topshelf.Autofac;
    using Topshelf.Diagnostics;

    /// <summary>   A program. </summary>
    class Program
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
            builder.RegisterType<Logger>().SingleInstance();
            builder.RegisterType<DeploymentMonitorMicroservice>()
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
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

                c?.Service<DeploymentMonitorMicroservice>(s =>
                {
                    s.ConstructUsingAutofacContainer<DeploymentMonitorMicroservice>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<DeploymentMonitorMicroservice>();
                        return service;
                    });
                    s?.ConstructUsing(name => new DeploymentMonitorMicroservice());

                    s?.WhenStarted((DeploymentMonitorMicroservice server, HostControl host) => server.OnStart(host));
                    s?.WhenPaused(server => server.OnPause());
                    s?.WhenContinued(server => server.OnResume());
                    s?.WhenStopped(server => server.OnStop());
                    s?.WhenShutdown(server => server.OnShutdown());
                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("Deployment Monitor Microservice Sample"));
                c?.SetDisplayName(string.Intern("DeploymentMonitorMicroservice"));
                c?.SetServiceName(string.Intern("DeploymentMonitorMicroservice"));

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