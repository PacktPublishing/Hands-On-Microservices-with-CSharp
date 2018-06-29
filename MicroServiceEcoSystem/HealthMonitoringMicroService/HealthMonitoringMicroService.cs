using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitoringMicroService
{
    using System.Threading;
    using CacheManager.Core;
    using CircuitBreaker.Net;
    using CircuitBreaker.Net.Exceptions;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.Topology;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using ReflectSoftware.Insight;
    using Topshelf;

    public class HealthMonitoringMicroService : BaseMicroService<HealthMonitoringMicroService, HealthStatusMessage>
    {
        public new bool OnStart([CanBeNull] HostControl host)
        {
            base.Start(host);

            TryRequest(() => Thread.Sleep(100), 3, 100, 10000, (ex) => Console.WriteLine(ex?.Message));
            TryRequestAsync(() => Task.Delay(100), 2, 100, 10000, (ex) => Console.WriteLine(ex?.Message)).Wait();

            Cache = CacheFactory.Build("MicroServiceCache", settings => settings.WithSystemRuntimeCacheHandle("MicroServiceCache"));

            return true;
        }

        public new bool OnStop()
        {
            base.Stop();
            return true;
        }

        public new bool OnContinue()
        {
            return true;
        }

        public new bool OnPause()
        {
            return true;
        }

        public new bool OnResume()
        {
            return true;
        }

        public new bool OnShutdown()
        {
            return true;
        }

        public void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x => x.Register<IConventions, AttributeBasedConventions>());

            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare("HealthStatus");
            Bus.Advanced.Bind(exchange, queue, "");

            Bus.Subscribe<HealthStatusMessage>("HealthStatus", msg => { ProcessMessage(msg, null); },
                config => config.WithTopic("HealthStatus"));
        }

        bool ProcessMessage([NotNull] HealthStatusMessage msg, [NotNull] MessageReceivedInfo mri)
        {
            Console.WriteLine("Received Health Status Message");
            RILogManager.Default?.SendInformation("Received Health Status Message");
            RILogManager.Default?.SendInformation(msg.serviceName);
            RILogManager.Default?.SendInformation(msg.status.ToString());
            RILogManager.Default?.SendInformation(ToBytes(msg.memoryUsed));
            RILogManager.Default?.SendInformation(msg.CPU.ToString());
            RILogManager.Default?.SendInformation(msg.message);
            Cache.Add(msg.ID + msg.date.ToLongTimeString() + "_" + msg.date.ToLongTimeString(), msg, "HealthMonitoring");
            return true;
        }
    }
}
