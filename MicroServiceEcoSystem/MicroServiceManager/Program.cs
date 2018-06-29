using System;

namespace MicroServiceManager
{
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.SmartInterceptors.Interceptors;
    using log4net.Config;
    using log4net.Repository.Hierarchy;
    using MicroServiceEcoSystem;
    using Topshelf;
    using Topshelf.Autofac;
    using Topshelf.Diagnostics;
    using Topshelf.FileSystemWatcher;
    using Topshelf.Leader;

    /// <summary>   A program. </summary>
    class Program
    {
        /// <summary>   The container. </summary>
        private static IContainer _container;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this application. </summary>
        ///
        /// <param name="args"> An array of command-line argument strings. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();

            // Service itself
            builder.RegisterType<MSBaseLogger>()?.SingleInstance();
            builder.RegisterType<MicroServiceManager>()
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
            builder.AttachInterceptorsToRegistrations(new LogInterceptor(Console.Out));
            
            _container = builder.Build();

            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c?.UseAutofacContainer(_container);
                c?.UseLog4Net();
                c?.ApplyCommandLineWithDebuggerSupport();
                c?.EnablePauseAndContinue();
                c?.EnableShutdown();
                c?.OnException(ex => Console.WriteLine(ex.Message));
                c?.UseWindowsHostEnvironmentWithDebugSupport();
                
                c?.Service<MicroServiceManager>(s =>
                {
                    s.ConstructUsingAutofacContainer<MicroServiceManager>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<MicroServiceManager>();
                        return service;
                    });
                    s?.ConstructUsing(name => new MicroServiceManager(_container, Guid.NewGuid().ToString()));

                    s?.WhenStartedAsLeader(b =>
                    {
                        b.WhenStarted(async (service, token) =>
                        {
                            await service.Start(token);
                        });

                        b.Lease(lcb => lcb.RenewLeaseEvery(TimeSpan.FromSeconds(2))
                            .AquireLeaseEvery(TimeSpan.FromSeconds(5))
                            .LeaseLength(TimeSpan.FromDays(1))
                            .WithLeaseManager(new MicroServiceManager()));

                        b.WithHeartBeat(TimeSpan.FromSeconds(30), (isLeader, token) => Task.CompletedTask);
                        b.Build();
                    });
                    s?.WhenStarted((MicroServiceManager server, HostControl host) => server.OnStart(host));
                    s?.WhenPaused(server => server?.OnPause());
                    s?.WhenContinued(server => server?.OnResume());
                    s?.WhenStopped(server => server?.OnStop());
                    s?.WhenShutdown(server => server?.OnShutdown());
                    s?.WhenCustomCommandReceived((server, host, code) => { });
                    s?.AfterStartingService(() => { });
                    s?.AfterStoppingService(() => { });
                    s?.BeforeStartingService(() => { });
                    s?.BeforeStoppingService(() => { });

                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("MicroService Manager Sample"));
                c?.SetDisplayName(string.Intern("MicroServiceManager"));
                c?.SetServiceName(string.Intern("MicroServiceManager"));

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