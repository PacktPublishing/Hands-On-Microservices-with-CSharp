namespace MicroServiceEcoSystem.Deployment_Monitor
{
    using System;
    using System.Diagnostics;
    using System.Net.Configuration;
    using System.Reflection;
    using System.Timers;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.AutoSubscribe;
    using JetBrains.Annotations;
    using CommonMessages;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using NodaTime;
    using Topshelf;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A deployment monitor microservice. This class cannot be inherited. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{MicroServiceEcoSystem.Deployment_Monitor.DeploymentMonitorMicroservice, CommonMessages.HealthStatusMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public sealed class DeploymentMonitorMicroservice : BaseMicroService<DeploymentMonitorMicroservice, HealthStatusMessage>
    {
        /// <summary>   The bus. </summary>
        private IBus _bus;
        /// <summary>   True to deployment in progress. </summary>
        private bool _deploymentInProgress;
        /// <summary>   The deployment timer. </summary>
        private Timer _deploymentTimer;
        /// <summary>   The health timer. </summary>
        private Timer _healthTimer;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the
        /// MicroServiceEcoSystem.Deployment_Monitor.DeploymentMonitorMicroservice class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public DeploymentMonitorMicroservice()
        {
            Name = "Deployment Monitor Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="hc">   The hc. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStart([CanBeNull] HostControl hc)
        { 
            base.Start(hc);
            Subscribe();
            _deploymentInProgress = false;

            if (_deploymentTimer == null)
                _deploymentTimer = new Timer();
            _deploymentTimer.Interval = 6000 * 15;  // give it 15 minutes
            _deploymentTimer.Enabled = true;
            _deploymentTimer.Elapsed += _deploymentTimer_Elapsed;
            _deploymentTimer.AutoReset = true;
            _deploymentTimer.Start();

            if(_healthTimer == null)
                _healthTimer = new Timer();
            _healthTimer.Interval = 60000;
            _healthTimer.Enabled = true;
            _healthTimer.AutoReset = true;
            _healthTimer.Elapsed += _healthTimer_Elapsed;
            _healthTimer.Start();
            Subscribe();


            return (true);
        }

        /// <summary>   Subscribes this object. </summary>
        public void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });

            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare("Deployments");
            Bus.Advanced.Bind(exchange, queue, "");

            Bus.Subscribe<DeploymentStartMessage>("Deployment.Start", msg => { ProcessDeploymentStartMessage(msg); },
                config => config.WithTopic("Deployments"));
            Bus.Subscribe<DeploymentStopMessage>("Deployment.Stop", msg => { ProcessDeploymentStopMessage(msg); },
                config => config.WithTopic("Deployments"));

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the deployment start message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void ProcessDeploymentStartMessage(DeploymentStartMessage msg)
        {
            _deploymentTimer.Stop();
            _deploymentTimer.Start();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the deployment stop message described by msg. </summary>
        ///
        /// <param name="msg">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void ProcessDeploymentStopMessage(DeploymentStopMessage msg)
        {
            _deploymentTimer.Stop();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by _healthTimer for elapsed events. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Elapsed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void _healthTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            HealthStatusMessage h = new HealthStatusMessage
            {
                ID = base.ID,
                memoryUsed = Environment.WorkingSet,
                CPU = Convert.ToDouble(getCPUCounter()),
                date = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                serviceName = "Deployment Monitor MicroService",
                message = "OK",
                status = (int) MSStatus.Healthy
            };
            Bus.Publish(h, "HealthStatus");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            base.Stop();
            _deploymentTimer.Stop();
            _deploymentTimer.Enabled = false;
            _deploymentTimer.Elapsed -= _deploymentTimer_Elapsed;
            return (true);
        }

         ////////////////////////////////////////////////////////////////////////////////////////////////////
         /// <summary>  Continues this object. </summary>
         ///
         /// <returns>  True if it succeeds, false if it fails. </returns>
         ////////////////////////////////////////////////////////////////////////////////////////////////////

         public new bool Continue()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Pauses this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool Pause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Resumes this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool Resume()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by _deploymentTimer for elapsed events. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Elapsed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void _deploymentTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_deploymentInProgress)
                Console.WriteLine("ERROR: Deployment is taking too long");
        }
    }
}
