using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitcoinMicroService
{

    using System.IO;
    using Autofac;
    using EasyNetQ.Logging;
    using log4net.Config;
    using MicroServiceEcoSystem;
    using Topshelf;
    using Topshelf.Autofac;
    using Topshelf.Diagnostics;
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
            builder.RegisterType<BitcoinMS>()
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
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

                c?.Service<BitcoinMS>(s =>
                {
                    s.ConstructUsingAutofacContainer<BitcoinMS>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<BitcoinMS>();
                        return service;
                    });
                    s?.ConstructUsing(name => new BitcoinMS(_container, new Guid().ToString()));

                    s.WhenStartedAsLeader(b =>
                    {
                        b.WhenStarted(async (service, token) =>
                        {
                            await service.Start(token);
                        });

                        b.Lease(lcb => lcb.RenewLeaseEvery(TimeSpan.FromSeconds(2))
                            .AquireLeaseEvery(TimeSpan.FromSeconds(5))
                            .LeaseLength(TimeSpan.FromDays(1))
                            .WithLeaseManager(new BitcoinMS()));

                        b.WithHeartBeat(TimeSpan.FromSeconds(30), (isLeader, token) => Task.CompletedTask);
                    });

                    s?.WhenStarted((server, host) => server.OnStart(host));
                    s?.WhenPaused(server => server?.OnPause());
                    s?.WhenContinued(server => server?.OnResume());
                    s?.WhenStopped(server => server?.OnStop());
                    s?.WhenShutdown(server => server?.OnShutdown());
                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("Bitcoin Manager Sample"));
                c?.SetDisplayName(string.Intern("BitcoinMicroService"));
                c?.SetServiceName(string.Intern("BitcoinMicroService"));

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