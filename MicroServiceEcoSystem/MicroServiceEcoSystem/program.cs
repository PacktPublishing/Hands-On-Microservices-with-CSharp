using Quartz.Simpl;
using Quartz.Spi;
using Fooidity;
using Topshelf;
using Topshelf.Autofac;

namespace MicroServiceEcoSystem
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel.Design;
    using System.IO;
    using Autofac;
    using Autofac.Extras.Quartz;
    using JetBrains.Annotations;
    using log4net.Config;
    using Topshelf.Dashboard;
    using Topshelf.Diagnostics;


    public class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SampleDependency>()?.As<ISampleDependency>();
            builder.RegisterType<SampleMicroService>();
            builder.RegisterModule<Logger>();
            builder.RegisterAssemblyTypes(typeof(Program).Assembly)
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
            var container = builder.Build();

            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c.UseAutofacContainer(container);
                c.ApplyCommandLineWithDebuggerSupport();

                c.Service<SampleMicroService>(s =>
                {
                    s.ConstructUsingAutofacContainer();
                    s?.ConstructUsing(() => container.Resolve<SampleMicroService>());
                    s?.WhenStarted((service, control) => service.Start(control));
                    s?.WhenStopped((service, control) => service.Stop());
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
    }
}
