using Topshelf;
using Topshelf.Autofac;

namespace MicroServiceEcoSystem
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using Autofac;
    using Autofac.Extras.Quartz;
    using JetBrains.Annotations;
    using log4net.Config;
    using Quartz;
    using Topshelf.Dashboard;
    using Topshelf.Quartz;


    public class programforscheduling
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SampleDependency>()?.As<ISampleDependency>();
            builder.RegisterType<SampleMicroService>();
            builder.RegisterModule<Logger>();
            builder.RegisterModule(new QuartzAutofacFactoryModule
            {
                ConfigurationProvider = QuartzConfigurationProvider
            });
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(Program).Assembly));
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
            var container = builder.Build();
            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c.UseAutofacContainer(container);
                c.UseSerilog();
                //c.EnableDashboard();

                c.Service<SampleMicroService>(s =>
                {
                    s.ConstructUsingAutofacContainer();
                    s?.WhenStarted((service, control) => service.Start(control));
                    s?.WhenStopped((service, control) => service.Stop());

                    s.ScheduleQuartzJob(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<SampleJob>()?.Build())
                            .AddTrigger(() => TriggerBuilder.Create()
                                .WithSimpleSchedule(b => b
                                    .WithIntervalInSeconds(10)
                                    .RepeatForever())
                                ?.Build()));
                });

                c.RunAsLocalSystem();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern("MicroService Sample using C#"));
                c?.SetDisplayName(string.Intern("MicroServiceSample"));
                c?.SetServiceName(string.Intern("MicroServiceSample"));

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


        [NotNull]
        private static NameValueCollection QuartzConfigurationProvider([NotNull] IComponentContext arg)
        {
            return new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = "XmlConfiguredInstance",
                ["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz",
                ["quartz.threadPool.threadCount"] = "5",
                ["quartz.plugin.xml.type"] = "Quartz.Plugin.Xml.XMLSchedulingDataProcessorPlugin, Quartz.Plugins",
                ["quartz.plugin.xml.fileNames"] = "quartz-jobs.config",
                ["quartz.plugin.xml.FailOnFileNotFound"] = "true",
                ["quartz.plugin.xml.failOnSchedulingError"] = "true"
            };
        }
    }
}
