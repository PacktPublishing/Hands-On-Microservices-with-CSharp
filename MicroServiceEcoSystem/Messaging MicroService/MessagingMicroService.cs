using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem.Messaging_MicroService
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using CommonMessages;
    using EasyNetQ;
    using JetBrains.Annotations;
    using RabbitMQ.Client;
    using Topshelf;
    using ExchangeType = EasyNetQ.Topology.ExchangeType;



    class MessagingMicroService : BaseMicroService<MessagingMicroService>
    {
        private const string Exchange = "fanout-exchange-example";

        public new bool OnStart([CanBeNull] HostControl host)
        {
            base.Start(host);

            // publish a message
            BasicMessage msg = new BasicMessage();
            msg.Text = "Hello";
            msg.Date = DateTime.UtcNow;
            msg.RandomNumber = 1;

            PublishMessage(msg, "EvolvedAI");
            CreateTopology("EvolvedAI", "Messaging");
            return (true);
        }

        public new bool OnStop()
        {
            base.Stop();
            return (true);
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

    }
}
