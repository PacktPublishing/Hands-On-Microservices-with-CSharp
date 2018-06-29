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
    using EasyNetQ;
    using JetBrains.Annotations;
    using RabbitMQ.Client;
    using Topshelf;
    using ExchangeType = EasyNetQ.Topology.ExchangeType;

    public class Message
    {
        public string Text { get; set; }
        public int RandomNumber { get; set; }
        public DateTime Date { get; set; }
    }

    class MessagingMicroService : BaseMicroService
    {
        private const string Exchange = "fanout-exchange-example";

        public new bool Start([CanBeNull] HostControl host)
        {
            base.Start(host);
            SimplePublish();
            AdvancedPublish();
            return (true);
        }

        public new bool Stop()
        {
            base.Stop();
            return (true);
        }

        void SimplePublish()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            bus?.Subscribe<string>("consumer1", Console.WriteLine);
        }

        void AdvancedPublish()
        {
            var connectionFactory = new ConnectionFactory();
            IConnection connection = connectionFactory.CreateConnection();
            IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(Exchange, ExchangeType.Fanout);

            // publish a message
            Message msg = new Message();
            msg.Text = "Hello";
            msg.Date = DateTime.UtcNow;
            msg.RandomNumber = 1;

            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, msg);
                channel.BasicPublish(Exchange, "", true, null, ms.ToArray());
            }

            channel.Close();
            connection.Close();
        }
    }
}
