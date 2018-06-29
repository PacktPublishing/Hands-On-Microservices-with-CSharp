namespace MicroServiceEcoSystem
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using System.Timers;
    using Autofac;
    using CircuitBreaker.Net;
    using CircuitBreaker.Net.Exceptions;
    using CodeContracts;
    using EasyNetQ;
    using JetBrains.Annotations;
    using log4net;
    using Topshelf;
    using Timer = System.Timers.Timer;
    using NodaTime;
    using CacheManager.Core;
    using EasyNetQ.Topology;
    using log4net.Config;
    using Topshelf.Autofac;
    using Topshelf.Diagnostics;
    using ExchangeType = RabbitMQ.Client.ExchangeType;
    using System.Runtime;
    using System.Threading;
    using CommonMessages;
    using EasyNetQ.MessageVersioning;
    using IContainer = Autofac.IContainer;
    using Grumpy.ServiceBase;
    using Console = Colorful.Console;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A base micro service. </summary>
    ///
    /// <typeparam name="T">        Generic type parameter. </typeparam>
    /// <typeparam name="TMessage"> Type of the message. </typeparam>
    ///
    /// <seealso cref="T:Grumpy.ServiceBase.TopshelfServiceBase"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class BaseMicroService<T, TMessage> : TopshelfServiceBase
        where T : class, new()
        where TMessage : class, new()
    {
        /// <summary>   The timer. </summary>
        private static Timer _timer = null;
        /// <summary>   The log. </summary>
        readonly static ILog _log = LogManager.GetLogger(typeof(BaseMicroService<T, TMessage>));
        /// <summary>   Identifier for the worker. </summary>
        private string _workerId;
        /// <summary>   The lifetimescope. </summary>
        private static ILifetimeScope _lifetimescope;
        /// <summary>   The name. </summary>
        private static string _name;
        /// <summary>   The host. </summary>
        private static HostControl _host;
        /// <summary>   The type. </summary>
        private static T _type;

        /// <summary>   The logger. </summary>
        private static ILogger _logger;

        /// <summary>   The di container. </summary>
        private IContainer _diContainer;

        /// <summary>   The bus. </summary>
        private IBus _bus;
        /// <summary>   The cache. </summary>
        private ICacheManager<object> _cache = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the bus. </summary>
        ///
        /// <value> The bus. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IBus Bus
        {
            get { return _bus; }
            set { _bus = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the cache. </summary>
        ///
        /// <value> The cache. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ICacheManager<object> Cache
        {
            get { return _cache; }
            set { _cache = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the identifier. </summary>
        ///
        /// <value> The identifier. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string ID
        {
            get { return _workerId.ToString(); }
            set { _workerId = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the name. </summary>
        ///
        /// <value> The name. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the host. </summary>
        ///
        /// <value> The host. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public HostControl Host
        {
            get
            {
                return _host;
            }
            set { _host = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the IOC container. </summary>
        ///
        /// <value> The IOC container. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public IContainer IOCContainer
        {
            get { return _diContainer; }
            set { _diContainer = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the lifetime scope. </summary>
        ///
        /// <value> The lifetime scope. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ILifetimeScope LifetimeScope
        {
            get { return _lifetimescope; }
            set { _lifetimescope = value; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.BaseMicroService&lt;T&gt; class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BaseMicroService() 
        {
            double interval = 60000;
            _timer = new Timer(interval);
            Assumes.True(_timer != null, "_timer is null");

            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _workerId = Guid.NewGuid().ToString();
            _name = nameof(T);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceEcoSystem.BaseMicroService&lt;T&gt; class.
        /// </summary>
        ///
        /// <param name="lifetimescope">    The lifetime scope. This may be null. </param>
        /// <param name="name">             The name. This cannot be null. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public BaseMicroService([CanBeNull] ILifetimeScope lifetimescope, [NotNull]string name) 
            : this()
        {
            _lifetimescope = lifetimescope;
            _name = name;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the given cancellationToken. </summary>
        ///
        /// <param name="cancellationToken">    The cancellation token. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Process(CancellationToken cancellationToken)
        {
            Start();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Resumes this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Resume()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Resuming");
                Console.WriteLine(Name + " Microservice Resuming", Color.Yellow);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Stops this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Stop()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Stopping");
                Console.WriteLine(Name + " Microservice Stopping", Color.Yellow);
            }
            Assumes.True(_log != null, string.Intern("_log is null"));
            _log?.Info(_name + string.Intern(" Service is Stopped"));

            Assumes.True(_timer != null, string.Intern("_timer is null"));
            _timer.AutoReset = false;
            _timer.Enabled = false;
            _timer.Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Pauses this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Pause()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Pausing");
                Console.WriteLine(Name + " Microservice Pausing", Color.Yellow);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Continues this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Continue()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Continuing");
                Console.WriteLine(Name + " Microservice Continuing", Color.Yellow);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Shuts down this object and frees any resources it is using. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Shutdown()
        {
            using (var scope = IOCContainer?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Shutting Down");
                Console.WriteLine(Name + " Microservice Shutting Down", Color.Yellow);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Starts the given hc. </summary>
        ///
        /// <param name="hc">   The hc. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public virtual bool Start(HostControl hc)
        {
            _host = hc;
            Console.WriteLine(_name + string.Intern(" Service Started."), Color.Yellow);

            Assumes.True(_timer!= null, string.Intern("_timer is null"));
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer.Start();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Raises the elapsed event. </summary>
        ///
        /// <param name="sender">   Source of the event. This cannot be null. </param>
        /// <param name="e">        Event information to send to registered event handlers. This cannot
        ///                         be null. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void OnTick([NotNull] object sender, [NotNull] ElapsedEventArgs e)
        {
            Requires.NotNull<ILog>(_log, string.Intern("log is null"));

            _log?.Debug(_name + " (" + _workerId.ToString() + string.Intern("): ") +
               SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().ToLongTimeString() + string.Intern(": Heartbeat"));
            Console.WriteLine(_name + " (" + _workerId.ToString() + string.Intern("): ") +
               SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().ToLongTimeString() + string.Intern(": Heartbeat"), Color.Aqua);

            HealthStatusMessage h = new HealthStatusMessage
            {
                ID = _workerId,
                memoryUsed = Environment.WorkingSet,
                CPU = Convert.ToDouble(getCPUCounter()),
                date = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                serviceName = Name,
                message = "OK",
                status = (int)MSStatus.Healthy
            };
            Bus.Publish(h, "HealthStatus");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Try request. </summary>
        ///
        /// <param name="action">           The action. This cannot be null. </param>
        /// <param name="maxFailures">      The maximum failures. </param>
        /// <param name="startTimeoutMS">   (Optional) The start timeout milliseconds. </param>
        /// <param name="resetTimeout">     (Optional) The reset timeout. </param>
        /// <param name="OnError">          (Optional) The on error. This may be null. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void TryRequest([NotNull] Action action, int maxFailures, int startTimeoutMS = 100, int resetTimeout = 10000,
            [CanBeNull] Action<Exception> OnError = null)
        {
            Requires.True(maxFailures >= 1, string.Intern("maxFailures must be >= 1"));
            Requires.True(startTimeoutMS >= 1, string.Intern("startTimeoutMS must be >= 1"));
            Requires.True(resetTimeout >= 1, string.Intern("resetTimeout must be >= 1"));
            Requires.True(action != null, string.Intern("action is null"));

            try
            {
                // Initialize the circuit breaker
                var circuitBreaker = new CircuitBreaker(
                    TaskScheduler.Default,
                    maxFailures: maxFailures,
                    invocationTimeout: TimeSpan.FromMilliseconds(startTimeoutMS),
                    circuitResetTimeout: TimeSpan.FromMilliseconds(resetTimeout));

                Assumes.True(circuitBreaker != null, string.Intern("circuitBreaker is null"));
                circuitBreaker.Execute(() => action);
            }
            catch (CircuitBreakerOpenException e1)
            {
                OnError?.Invoke(e1);
                Console.WriteLine(e1.Message, Color.Red);
            }
            catch (CircuitBreakerTimeoutException e2)
            {
                OnError?.Invoke(e2);
                Console.WriteLine(e2.Message, Color.Red);
            }
            catch (Exception e3)
            {
                OnError?.Invoke(e3);
                Console.WriteLine(e3.Message, Color.Red);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Try request asynchronous. </summary>
        ///
        /// <param name="action">           The action. This cannot be null. </param>
        /// <param name="maxFailures">      The maximum failures. </param>
        /// <param name="startTimeoutMS">   (Optional) The start timeout milliseconds. </param>
        /// <param name="resetTimeout">     (Optional) The reset timeout. </param>
        /// <param name="OnError">          (Optional) The on error. This may be null. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task TryRequestAsync([NotNull] Func<Task> action, int maxFailures, int startTimeoutMS = 100, int resetTimeout = 10000,
             [CanBeNull] Action<Exception> OnError = null)
        {
            Requires.True(maxFailures >= 1, string.Intern("maxFailures must be >= 1"));
            Requires.True(startTimeoutMS >= 1, string.Intern("startTimeoutMS must be >= 1"));
            Requires.True(resetTimeout >= 1, string.Intern("resetTimeout must be >= 1"));
            Requires.True(action != null, string.Intern("action is null"));

            try
            {
                // Initialize the circuit breaker
                var circuitBreaker = new CircuitBreaker(
                    TaskScheduler.Default,
                    maxFailures: maxFailures,
                    invocationTimeout: TimeSpan.FromMilliseconds(startTimeoutMS),
                    circuitResetTimeout: TimeSpan.FromMilliseconds(resetTimeout));
                Assumes.True(circuitBreaker != null, string.Intern("circuitBreaker is null"));

                await circuitBreaker.ExecuteAsync(action);

            }
            catch (CircuitBreakerOpenException e1)
            {
                OnError?.Invoke(e1);
                Console.WriteLine(e1.Message, Color.Red);
            }
            catch (CircuitBreakerTimeoutException e2)
            {
                OnError?.Invoke(e2);
                Console.WriteLine(e2.Message, Color.Red);
            }
            catch (Exception e3)
            {
                OnError?.Invoke(e3);
                Console.WriteLine(e3.Message, Color.Red);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Subscribes. </summary>
        ///
        /// <param name="queueName">        Name of the queue. </param>
        /// <param name="subscriptionID">   Identifier for the subscription. </param>
        /// <param name="msgHandler">       The message handler. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Subscribe(string queueName, string subscriptionID, Action<TMessage> msgHandler) 
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });
            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare(queueName);
            Bus.Advanced.Bind(exchange, queue, Environment.MachineName);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="topic">        The topic. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishMessage(TMessage msg, string topic, string routingID="")
        {
            Bus.Publish(msg, topic);
        }

        /// <summary>   Reclaim memory. </summary>
        public void ReclaimMemory()
        {
            long mem2 = GC.GetTotalMemory(false);

            Console.WriteLine(string.Intern("*** Memory at ***" +
                SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().ToLongDateString() + " " +
                SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().ToLongTimeString()), Color.Aqua);

            Console.WriteLine(string.Intern("\tMemory before GC: ") + ToBytes(mem2), Color.Aqua);
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long mem3 = GC.GetTotalMemory(false);
            Console.WriteLine(string.Intern("\tMemory after GC: ") + ToBytes(mem3), Color.Aqua);
            Console.WriteLine("\tApp memory being used: " + ToBytes(Environment.WorkingSet), Color.Aqua);
            int gen1 = 0;
            int gen2 = 0;
            for (int x = 0; x < GC.MaxGeneration; x++)
            {
                if (x == 0)
                    gen1 = GC.CollectionCount(x);
                else if (x == 1)
                    gen2 = GC.CollectionCount(x);
                Console.WriteLine("\t\tGeneration " + (x) + " Collection Count: " + GC.CollectionCount(x), Color.Aqua);
            }

            const string category = ".NET CLR Memory";
            const string counter = "% Time in GC";
            string instance = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            float percent = 0.0F;

            if (PerformanceCounterCategory.Exists(category) && PerformanceCounterCategory.CounterExists(counter, category) &&
                PerformanceCounterCategory.InstanceExists(instance, category))
            {
                var gcPerf = new PerformanceCounter(category, counter, instance);
                percent = gcPerf.NextValue();

                string suffix = "%";
                if (percent > 50.0)
                {
                    suffix += " <- High Watermark Warning";
                }
                Console.WriteLine("\t\tTime Spent in GC: " + $"{percent:00.##}" + suffix, Color.Aqua);
            }

            PublishMemoryUpdateMessage(gen1, gen2, percent, ToBytes(mem2), ToBytes(mem3));
            Console.WriteLine(string.Intern("*** Memory ***"), Color.Aqua);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish memory update message. </summary>
        ///
        /// <param name="gen1">         The first generate. </param>
        /// <param name="gen2">         The second generate. </param>
        /// <param name="timeSpent">    The time spent. </param>
        /// <param name="MemoryBefore"> The memory before. </param>
        /// <param name="MemoryAfter">  The memory after. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PublishMemoryUpdateMessage(int gen1, int gen2, float timeSpent, string MemoryBefore, string MemoryAfter)
        {
            // publish a message
            MemoryUpdateMessage msg = new MemoryUpdateMessage
            {
                Text = "Memory MicroService Ran",
                Date = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc(),
                Gen1CollectionCount = gen1,
                Gen2CollectionCount = gen2,
                TimeSpentPercent = timeSpent,
                MemoryBeforeCollection = MemoryBefore,
                MemoryAfterCollection = MemoryAfter
            };

            Bus.Publish(msg, "MemoryStatus");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets CPU counter. </summary>
        ///
        /// <returns>   The CPU counter. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public float getCPUCounter()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter
            {
                CategoryName = string.Intern("Processor"),
                CounterName = string.Intern("% Processor Time"),
                InstanceName = string.Intern("_Total")
            };

            // will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            Thread.Sleep(1000);
            // now matches task manager reading
            float secondValue = cpuCounter.NextValue();
            return secondValue;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts a value to the bytes. </summary>
        ///
        /// <param name="value">    The value. </param>
        ///
        /// <returns>   Value as a string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string ToBytes(double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " + suffixes[suffixes.Length - 1];
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Three non zero digits. </summary>
        ///
        /// <param name="value">    The value. </param>
        ///
        /// <returns>   A string. This will never be null. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        [NotNull]
        private static string ThreeNonZeroDigits(double value)
        {
            return value >= 100 ? value.ToString("0,0") : value.ToString(value >= 10 ? "0.0" : "0.00");

            // Two digits after the decimal.
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Default main. </summary>
        ///
        /// <param name="serviceName">          Name of the service. </param>
        /// <param name="serviceDesc">          Information describing the service. </param>
        /// <param name="serviceDisplayName">   Name of the service display. </param>
        /// <param name="onStart">              The on start. </param>
        /// <param name="onStop">               The on stop. </param>
        /// <param name="onPause">              The on pause. </param>
        /// <param name="onResume">             The on resume. </param>
        /// <param name="onShutDown">           The on shut down. </param>
        /// <param name="onException">          The on exception. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void DefaultMain(string serviceName, string serviceDesc, string serviceDisplayName,
            Func<HostControl, bool> onStart, Func<bool> onStop, Func<bool> onPause, Func<bool> onResume,
            Func<bool> onShutDown, Action<Exception> onException)
        {
            var builder = new ContainerBuilder();

            // Service itself
            builder.RegisterType<MSBaseLogger>().SingleInstance();
            builder.RegisterType<T>()
                .AsImplementedInterfaces()
                .AsSelf()
                ?.InstancePerLifetimeScope();
            var container = builder.Build();

            XmlConfigurator.ConfigureAndWatch(new FileInfo(@".\log4net.config"));

            HostFactory.Run(c =>
            {
                c?.UseAutofacContainer(container);
                c?.UseLog4Net();
                c?.ApplyCommandLineWithDebuggerSupport();
                c?.EnablePauseAndContinue();
                c?.EnableShutdown();
                c?.OnException(ex => { onException(ex); });

                c?.Service<T>(s =>
                {
                    s.ConstructUsingAutofacContainer<T>();

                    s?.ConstructUsing(settings =>
                    {
                        var service = AutofacHostBuilderConfigurator.LifetimeScope.Resolve<T>();
                        return service;
                    });
                    s?.ConstructUsing(name => new T());

                    s?.WhenStarted((T server, HostControl host) => onStart(host));
                    s?.WhenPaused(server => onPause());
                    s?.WhenContinued(server => onResume());
                    s?.WhenStopped(server => onStop());
                    s?.WhenShutdown(server => onShutDown());
                });

                c?.RunAsNetworkService();
                c?.StartAutomaticallyDelayed();
                c?.SetDescription(string.Intern(serviceDesc));
                c?.SetDisplayName(string.Intern(serviceDisplayName));
                c?.SetServiceName(string.Intern(serviceName));

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


