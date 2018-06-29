namespace Messaging_MicroService
{
    using System;
    using System.IO;
    using Autofac;
    using log4net.Config;
    using log4net.Repository.Hierarchy;
    using MicroServiceEcoSystem.Messaging_MicroService;
    using Topshelf;

    using Topshelf.Autofac;
    using Topshelf.Diagnostics;

    class Program
    {

        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            // Service itself
            builder.RegisterType<MessagingMicroService>()
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

                c?.Service<MessagingMicroService>(s =>
                {
                    s.ConstructUsingAutofacContainer<MessagingMicroService>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<MessagingMicroService>();
                        return service;
                    });
                    s?.ConstructUsing(name => new MessagingMicroService());

                    s?.WhenStarted((MessagingMicroService server, HostControl host) => server.OnStart(host));
                    s?.WhenPaused(server => server.OnPause());
                    s?.WhenContinued(server => server.OnResume());
                    s?.WhenStopped(server => server.OnStop());
                    s?.WhenShutdown(server => server.OnShutdown());
                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("Messaging Microservice Sample"));
                c?.SetDisplayName(string.Intern("MessagingMicroService"));
                c?.SetServiceName(string.Intern("MessagingMicroService"));

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