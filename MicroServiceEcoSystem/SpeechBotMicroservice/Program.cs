using System;
using System.IO;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechBotMicroservice
{
    using Autofac;
    using log4net.Config;
    using MicroServiceEcoSystem;
    using Topshelf;
    using Topshelf.Autofac;
    using Topshelf.Diagnostics;
    using Topshelf.Leader;


    /// <summary>   A program. </summary>
    public class Program
    {
        /// <summary>   The voice. </summary>
        SpeechSynthesizer voice = new SpeechSynthesizer();
        /// <summary>   The speech recognition engine. </summary>
        SpeechRecognitionEngine speechRecognitionEngine = null;
        /// <summary>   The completed. </summary>
        ManualResetEvent _completed = new ManualResetEvent(false);

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
            builder.RegisterType<SpeechBot>()
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
                c?.UseWindowsHostEnvironmentWithDebugSupport();

                c?.Service<SpeechBot>(s =>
                {
                    s.ConstructUsingAutofacContainer<SpeechBot>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<SpeechBot>();
                        return service;
                    });
                    s?.ConstructUsing(name => new SpeechBot(_container, Guid.NewGuid().ToString()));

                    s?.WhenStartedAsLeader(b =>
                    {
                        b.WhenStarted(async (service, token) =>
                        {
                            await service.Start(token);
                        });

                        b.Lease(lcb => lcb.RenewLeaseEvery(TimeSpan.FromSeconds(2))
                            .AquireLeaseEvery(TimeSpan.FromSeconds(5))
                            .LeaseLength(TimeSpan.FromDays(1))
                            .WithLeaseManager(new SpeechBot()));

                        b.WithHeartBeat(TimeSpan.FromSeconds(30), (isLeader, token) => Task.CompletedTask);
                        b.Build();
                    });
                    s?.WhenStarted((SpeechBot server, HostControl host) => server.OnStart(host));
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
                c?.SetDescription(string.Intern("Speech Bot Microservice"));
                c?.SetDisplayName(string.Intern("SpeechBotMicroservice"));
                c?.SetServiceName(string.Intern("SpeechBotMicroservice"));

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
