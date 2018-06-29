namespace MicroServiceEcoSystem
{
    using System;
    using System.Timers;
    using Autofac;
    using JetBrains.Annotations;
    using log4net;
    using Topshelf;
    using Timer = System.Timers.Timer;

    public class BaseMicroService : IBaseMicroService
    {
        private HostControl _hostControl;
        private Timer _timer = null;
        readonly ILog _log = LogManager.GetLogger(typeof(BaseMicroService));
        private Guid _workerId;
        readonly ILifetimeScope _lifetimescope;

        public BaseMicroService()
        {
            double interval = 5000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _workerId = Guid.NewGuid();
        }

        public BaseMicroService([CanBeNull] ILifetimeScope lifetimescope)
            : this()
        {
            _lifetimescope = lifetimescope;
        }

        public bool Start([CanBeNull] HostControl hc)
        {
            _hostControl = hc;
            Console.WriteLine(string.Intern("Sample Service Started."));

            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
            return (true);
        }

        public bool Stop()
        {
            _log?.Info(string.Intern("SampleService is Stopped"));

            _timer.AutoReset = false;
            _timer.Enabled = false;
            return (true);
        }

        protected virtual void OnTick(object sender, ElapsedEventArgs e)
        {
            _log?.Debug(string.Intern("Tick:" + DateTime.Now.ToLongTimeString()));
            Console.WriteLine(string.Intern("Heartbeat"));
            _log.Debug(_workerId.ToString() + string.Intern(": ") + DateTime.Now.ToLongTimeString() + string.Intern(": Heartbeat"));
        }


    }
 
}


