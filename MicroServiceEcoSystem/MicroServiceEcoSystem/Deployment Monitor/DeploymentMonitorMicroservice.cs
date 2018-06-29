namespace MicroServiceEcoSystem.Deployment_Monitor
{
    using System;
    using System.Reflection;
    using System.Timers;
    using EasyNetQ;
    using EasyNetQ.AutoSubscribe;
    using JetBrains.Annotations;
    using CommonMessages;
    using Topshelf;

    public class DeploymentMonitorMicroservice : BaseMicroService, IConsume<DeploymentStartMessage>, IConsume<DeploymentStopMessage>
    {
        private IBus _bus;
        private bool _deploymentInProgress;
        private Timer _deploymentTimer;


        public new bool Start([CanBeNull] HostControl hc)
        { 
            base.Start(hc);
            _bus = RabbitHutch.CreateBus("host=localhost");
            _deploymentInProgress = false;
            _deploymentTimer.Interval = 6000 * 15;  // give it 15 minutes
            _deploymentTimer.Enabled = true;
            _deploymentTimer.Elapsed += _deploymentTimer_Elapsed;
            _deploymentTimer.AutoReset = true;
            _deploymentTimer.Start();
            Subscribe();
            return (true);
        }

        public new bool Stop()
        {
            base.Stop();
            _deploymentTimer.Stop();
            _deploymentTimer.Enabled = false;
            _deploymentTimer.Elapsed -= _deploymentTimer_Elapsed;
            return (true);
        }

        private void _deploymentTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_deploymentInProgress)
                Console.WriteLine("ERROR: Deployment is taking too long");
        }

        private void Subscribe()
        {
            var subscriber = new AutoSubscriber(_bus, "DeploymentMonitor");
            subscriber.Subscribe(Assembly.GetExecutingAssembly());
        }

        [AutoSubscriberConsumer(SubscriptionId = "DeploymentMonitor")]
        [ForTopic("Deployment.Start")]
        [SubscriptionConfiguration(CancelOnHaFailover = true, PrefetchCount = 10)]
        public void Consume([NotNull] DeploymentStartMessage message)
        {
            Console.WriteLine("Deployment started");
            _deploymentInProgress = true;
        }

        [AutoSubscriberConsumer(SubscriptionId = "DeploymentMonitor")]
        [ForTopic("Deployment.Stop")]
        [SubscriptionConfiguration(CancelOnHaFailover = true, PrefetchCount = 10)]
        public void Consume([NotNull] DeploymentStopMessage message)
        {
            Console.WriteLine("Deployment completed");
            _deploymentInProgress = false;
        }
    }
}
