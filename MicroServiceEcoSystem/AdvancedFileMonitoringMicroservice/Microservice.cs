using System;

namespace AdvancedFileMonitoringMicroservice
{
    using System.Drawing;
    using System.Threading;
    using System.Timers;
    using Autofac;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using Grumpy.ServiceBase;
    using MicroServiceEcoSystem;
    using NodaTime;
    using Timer = System.Timers.Timer;
    using Console = Colorful.Console;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A microservice. </summary>
    ///
    /// <seealso cref="T:Grumpy.ServiceBase.TopshelfServiceBase"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Microservice : TopshelfServiceBase
    {
        /// <summary>   The bus. </summary>
        private IBus _bus;
        /// <summary>   The timer. </summary>
        private Timer _timer;
        /// <summary>   The container. </summary>
        private IContainer _container;
        /// <summary>   Full pathname of the test file. </summary>
        const string TestPath = @"C:\Temp";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the AdvancedFileMonitoringMicroservice.Microservice class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Microservice()
        {

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the AdvancedFileMonitoringMicroservice.Microservice class.
        /// </summary>
        ///
        /// <param name="container">    The container. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Microservice(IContainer container)
        {
            _container = container;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the given cancellationToken. </summary>
        ///
        /// <param name="cancellationToken">    The cancellation token. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Process(CancellationToken cancellationToken)
        {
            Name = "Microservice Manager_" + Environment.MachineName;


            Start();
            Subscribe();

            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Starting");
                Console.WriteLine(Name + " Microservice Starting", Color.Yellow);
            }

            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _timer.Start();

            RunRecoveringWatcher();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Raises the elapsed event. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void OnTick(object sender, ElapsedEventArgs e)
        {
            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + string.Intern(" Heartbeat"));
                Console.WriteLine(Name + string.Intern(" Heartbeat"), Color.Yellow);
            }
        }

        /// <summary>   Subscribes this object. </summary>
        private void Subscribe()
        {
            _bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });


            IExchange exchange = _bus?.Advanced?.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = _bus?.Advanced?.QueueDeclare("FileSystem");
            _bus?.Advanced?.Bind(exchange, queue, "");
        }

        /// <summary>   Executes the recovering watcher operation. </summary>
        void RunRecoveringWatcher()
        {
            Console.WriteLine("Will auto-detect unavailability of watched directory");
            Console.WriteLine(" - Windows timeout accessing network shares: ~110 sec on start, ~45 sec while watching.");
            using (var watcher = new RecoveringFileSystemWatcher(TestPath))
            {
                watcher.All += (_, e) => { Console.WriteLine($"{e.ChangeType} {e.Name}", ConsoleColor.Yellow); };
                watcher.Error += (_, e) =>
                {
                    Console.WriteLine(e.Error.Message, ConsoleColor.Red);
                };
                watcher.Existed += (_, e) =>
                {
                    Console.WriteLine($"Existed {e.Name}", ConsoleColor.Yellow);
                };
                watcher.Created += (_, e) =>
                {
                    Console.WriteLine($"Created {e.Name}", ConsoleColor.Yellow);
                    FileSystemChangeMessage m = new FileSystemChangeMessage
                    {
                        ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                        ChangeType = (int)e.ChangeType,
                        FullPath = e.FullPath,
                        Name = e.Name
                    };
                    _bus.Publish(m, "FileSystemChanges");
                };
                watcher.Deleted += (_, e) =>
                {
                    Console.WriteLine($"Deleted {e.Name}", ConsoleColor.Yellow);
                    FileSystemChangeMessage m = new FileSystemChangeMessage
                    {
                        ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                        ChangeType = (int)e.ChangeType,
                        FullPath = e.FullPath,
                        Name = e.Name
                    };
                    _bus.Publish(m, "FileSystemChanges");
                };
                watcher.Renamed += (_, e) =>
                {
                    Console.WriteLine($"Renamed {e.OldName} to {e.Name}", ConsoleColor.Yellow);
                    FileSystemChangeMessage m = new FileSystemChangeMessage
                    {
                        ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                        ChangeType = (int)e.ChangeType,
                        FullPath = e.FullPath,
                        OldPath = e.OldFullPath,
                        Name = e.Name,
                        OldName = e.OldName
                    };
                    _bus.Publish(m, "FileSystemChanges");

                };
                watcher.Changed += (_, e) =>
                {
                    Console.WriteLine($"Changed {e.Name}", ConsoleColor.Yellow);
                    FileSystemChangeMessage m = new FileSystemChangeMessage
                    {
                        ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                        ChangeType = (int)e.ChangeType,
                        FullPath = e.FullPath,
                        Name = e.Name
                    };
                    _bus.Publish(m, "FileSystemChanges");
                };

                watcher.DirectoryMonitorInterval = TimeSpan.FromSeconds(10);
                watcher.OrderByOldestFirst = false;
                watcher.DirectoryRetryInterval = TimeSpan.FromSeconds(5);
                watcher.IncludeSubdirectories = false;

                //watcher.EventQueueCapacity = 1;
                watcher.EnableRaisingEvents = true;

                PromptUser("Processing...");
                Console.WriteLine("Stopping...");
            }
            PromptUser("Stopped.");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Prompt user. </summary>
        ///
        /// <param name="message">          The message. </param>
        /// <param name="foregroundColor">  (Optional) The foreground color. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void PromptUser(string message, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            Console.WriteLine($"? {message} !Press <Enter> to continue.", foregroundColor);
            do
            {

            } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the message described by message. </summary>
        ///
        /// <param name="message">  The message. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void ProcessMessage(string message)
        {
            Console.WriteLine(message, ConsoleColor.Green);

        }
    }
}
