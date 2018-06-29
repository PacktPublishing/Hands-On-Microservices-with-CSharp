using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MicroServiceEcoSystem;
using Topshelf;

namespace MicroServiceManager
{
    using System.Reflection;
    using System.Threading;
    using System.Timers;
    using Autofac;
    using CacheManager.Core;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.AutoSubscribe;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.MultipleExchange;
    using EasyNetQ.Scheduling;
    using EasyNetQ.Topology;
    using LiteDB;
    using ReflectSoftware.Insight;
    using Topshelf.Leader;
    using Timer = System.Timers.Timer;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Manager for micro services. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{MicroServiceManager.MicroServiceManager, CommonMessages.HealthStatusMessage}"/>
    /// <seealso cref="T:Topshelf.Leader.ILeaseManager"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class MicroServiceManager : BaseMicroService<MicroServiceManager, HealthStatusMessage>, ILeaseManager
    {
        /// <summary>   The bus. </summary>
        private IBus _bus = null;
        /// <summary>   The timer. </summary>
        private Timer _timer = null;
        /// <summary>   The container. </summary>
        private Autofac.IContainer _container;
        /// <summary>   Identifier for the owning node. </summary>
        private string _owningNodeId;
        /// <summary>   The connection string. </summary>
        const string connectionString = "c:\\Temp\\microservice_manager.litedb";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceManager.MicroServiceManager class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MicroServiceManager()
        {

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the MicroServiceManager.MicroServiceManager class.
        /// </summary>
        ///
        /// <param name="container">    The container. </param>
        /// <param name="owningNode">   The owning node. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MicroServiceManager(Autofac.IContainer container, string owningNode)
        {
            _container = container;
            _owningNodeId = owningNode;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Assign leader. </summary>
        ///
        /// <param name="newLeaderId">  Identifier for the new leader. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void AssignLeader(string newLeaderId)
        {
            this._owningNodeId = newLeaderId;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Acquires the lease. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>
        /// The asynchronous result that yields true if it succeeds, false if it fails.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task<bool> AcquireLease(LeaseOptions options, CancellationToken token)
        {
            return Task.FromResult(options.NodeId == _owningNodeId);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Renew lease. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        /// <param name="token">    The token. </param>
        ///
        /// <returns>
        /// The asynchronous result that yields true if it succeeds, false if it fails.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task<bool> RenewLease(LeaseOptions options, CancellationToken token)
        {
            return Task.FromResult(options.NodeId == _owningNodeId);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Release. </summary>
        ///
        /// <param name="options">  Options for controlling the operation. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Task ReleaseLease(LeaseReleaseOptions options)
        {
            _owningNodeId = string.Empty;
            return Task.FromResult(true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Starts the given stop token. </summary>
        ///
        /// <param name="stopToken">    The stop token. </param>
        ///
        /// <returns>   The asynchronous result. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task Start(CancellationToken stopToken)
        {

            while (!stopToken.IsCancellationRequested)
            {
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="host"> The host. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStart([CanBeNull] HostControl host)
        {
            Host = host;
            Name = "Microservice Manager_" + Environment.MachineName;


            Start(host);
            Subscribe();

            using (var scope = _container?.BeginLifetimeScope())
            {
                var logger = scope?.Resolve<MSBaseLogger>();
                logger?.LogInformation(Name + " Microservice Starting");
            }
            Cache = CacheFactory.Build("MicroServiceCache", settings => settings.WithSystemRuntimeCacheHandle("MicroServiceCache"));

            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _timer.Start();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            base.Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            base.Continue();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            base.Pause();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            base.Resume();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            base.Shutdown();
            return true;
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
                logger?.LogInformation(Name + string.Intern(" Reclaiming Memory"));
            }

            ReclaimMemory();
        }

        /// <summary>   Subscribes this object. </summary>
        private void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });


            IExchange exchange = Bus?.Advanced?.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus?.Advanced?.QueueDeclare("HealthStatus");
            Bus?.Advanced?.Bind(exchange, queue, "");
            queue = Bus?.Advanced?.QueueDeclare("Memory");
            Bus?.Advanced?.Bind(exchange, queue, "");
            queue = Bus?.Advanced?.QueueDeclare("Deployments");
            Bus?.Advanced?.Bind(exchange, queue, "");
            queue = Bus?.Advanced?.QueueDeclare("FileSystem");
            Bus?.Advanced?.Bind(exchange, queue, "");

            Bus?.Subscribe<HealthStatusMessage>(Environment.MachineName, msg => ProcessHealthMessage(msg),
                config => config?.WithTopic("HealthStatus"));
            Bus?.Subscribe<MemoryUpdateMessage>(Environment.MachineName, msg => ProcessMemoryMessage(msg),
                config => config?.WithTopic("MemoryStatus"));
            Bus?.Subscribe<DeploymentStartMessage>(Environment.MachineName, msg => ProcessDeploymentStartMessage(msg),
                config => config?.WithTopic("Deployments.Start"));
            Bus?.Subscribe<DeploymentStopMessage>(Environment.MachineName, msg => ProcessDeploymentStopMessage(msg),
                config => config?.WithTopic("Deployments.Stop"));
            Bus?.Subscribe<FileSystemChangeMessage>(Environment.MachineName, msg => ProcessFileSystemMessage(msg),
                config => config?.WithTopic("FileSystemChanges"));
            Bus?.Subscribe<CreditDefaultSwapResponseMessage>(Environment.MachineName, msg => ProcessCDSMessage(msg),
                config => config?.WithTopic("CDSResponse"));
            Bus?.Subscribe<BondsResponseMessage>(Environment.MachineName, msg => ProcessBondMessage(msg),
                config => config?.WithTopic("BondResponse"));
            Bus?.Subscribe<MLMessage>(Environment.MachineName, msg => ProcessMachineLearningMessage(msg),
                config => config?.WithTopic("MachineLearning"));
            Bus?.Subscribe<BitcoinSpendReceipt>(Environment.MachineName, msg => ProcessBitcoinSpendReceiptMessage(msg),
                config => config?.WithTopic("Bitcoin"));

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the bitcoin spend receipt message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessBitcoinSpendReceiptMessage(BitcoinSpendReceipt msg)
        {
            WriteLineInColor("Received Bitcoin Spent Receipt", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Bitcoin Spent Receipt Message");
            if (msg.success)
                WriteLineInColor("Someone spent " + msg.amount + " of my bitcoins", ConsoleColor.Green);
            else
            {
                WriteLineInColor("Someone tried to spend " + msg.amount + " of my bitcoins", ConsoleColor.Red);
            }

            msg.ID = new Random().Next(1,1000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<BitcoinSpendReceipt>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the machine learning message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessMachineLearningMessage(MLMessage msg)
        {
            WriteLineInColor("Received Machine Learning Response Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Machine Learning Response Message");

            msg.ID = new Random().Next(1000001, 2000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<MLMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the bond message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessBondMessage([NotNull] BondsResponseMessage msg)
        {
            WriteLineInColor("Received Bonds Response Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Bonds Response Message");
            WriteLineInColor(msg.message + ":\n"
                              + "    issue:     " + msg.issue + "\n"
                              + "    maturity:  " + msg.maturity + "\n"
                              + "    coupon:    " + msg.coupon + "\n"
                              + "    frequency: " + msg.frequency + "\n\n"
                              + "    yield:  " + msg.yield + " "
                              + msg.compounding + "\n"
                              + "    price:  " + msg.price + "\n"
                              + "    yield': " + msg.calcYield + "\n"
                              + "    price': " + msg.price2, ConsoleColor.Yellow);


            msg.ID = new Random().Next(2000001, 3000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<BondsResponseMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the cds message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessCDSMessage([NotNull] CreditDefaultSwapResponseMessage msg)
        {
            WriteLineInColor("Received Credit Default Swap Response Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Credit Default Swap Response Message");
            WriteLineInColor("calculated spread: " + msg.fairRate, ConsoleColor.Yellow);
            WriteLineInColor("calculated NPV: " + msg.fairNPV, ConsoleColor.Yellow);

            msg.ID = new Random().Next(3000001, 4000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<CreditDefaultSwapResponseMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the file system message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessFileSystemMessage([NotNull] FileSystemChangeMessage msg)
        {
            WriteLineInColor("Received FileSystemChange Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received FileSystemChange Message");

            Console.WriteLine("*********************");
            Console.WriteLine("Changed Date = {0}", msg.ChangeDate);
            Console.WriteLine("ChangeType = {0}", msg.ChangeType);
            Console.WriteLine("FullPath = {0}", msg.FullPath);
            Console.WriteLine("OldPath = {0}", msg.OldPath);
            Console.WriteLine("Name = {0}", msg.Name);
            Console.WriteLine("Old Name = {0}", msg.OldName);
            Console.WriteLine("FileSystemEventType {0}", (int)msg.EventType);
            Console.WriteLine("*********************");

            msg.ID = new Random().Next(4000001, 5000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<FileSystemChangeMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the deployment start message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessDeploymentStartMessage([NotNull] DeploymentStartMessage msg)
        {
            WriteLineInColor("Received DeploymentStart Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received DeploymentStart Message");

            msg.ID = new Random().Next(5000001, 6000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<DeploymentStartMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the deployment stop message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessDeploymentStopMessage([NotNull] DeploymentStopMessage msg)
        {
            WriteLineInColor("Received DeploymentStop Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received DeploymentStop Message");

            msg.ID = new Random().Next(6000001, 7000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<DeploymentStopMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the memory message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessMemoryMessage([NotNull] MemoryUpdateMessage msg)
        {
            WriteLineInColor("Received Memory Update Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Memory Update Message");

            msg.ID = new Random().Next(7000001, 8000000);
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<MemoryUpdateMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Process the health message described by msg. </summary>
        ///
        /// <param name="msg">  The message. This cannot be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        bool ProcessHealthMessage([NotNull] HealthStatusMessage msg)
        {
            WriteLineInColor("Received Health Status Message", ConsoleColor.Yellow);
            RILogManager.Default?.SendInformation("Received Health Status Message");

            if (msg.message.ToString().ToUpper() != "OK" && ((MSStatus)msg.status) != MSStatus.Healthy)
            {
                WriteLineInColor("Health Warning", ConsoleColor.Red);
                RILogManager.Default?.SendInformation("Health Warning");
            }

            RILogManager.Default?.SendInformation(msg.serviceName);
            RILogManager.Default?.SendInformation(msg.status.ToString());
            RILogManager.Default?.SendInformation(ToBytes(msg.memoryUsed));
            RILogManager.Default?.SendInformation(msg.CPU.ToString());
            RILogManager.Default?.SendInformation(msg.message);

            WriteLineInColor("Service: " + msg.serviceName, ConsoleColor.Yellow);
            WriteLineInColor("Status: " + msg.status.ToString(), ConsoleColor.Yellow);
            WriteLineInColor("Memory Used: " + ToBytes(msg.memoryUsed), ConsoleColor.Yellow);
            WriteLineInColor("CPU Used: " + msg.CPU.ToString(), ConsoleColor.Yellow);
            WriteLineInColor("Message: " +  msg.message, ConsoleColor.Yellow);

            Cache?.Add("MicroServiceID_" + msg.ID + "_" + msg.date + "_Status", (msg.status == 1 ? "Healthy" : "Unhealthy"), "MicroServiceStatus");
            Cache?.Add("MicroServiceID_" + msg.ID + "_" + msg.date + "_Memory", msg.memoryUsed, "MicroServiceStatus");
            Cache?.Add("MicroServiceID_" + msg.ID + "_" + msg.date + "_CPU", msg.CPU, "MicroServiceStatus");

            msg.ID = new Random().Next(8000001, 9000000).ToString();
            using (var _db = new LiteDatabase(connectionString))
            {
                Thread.Sleep(5);
                _db.Shrink();
                var collection = _db.GetCollection<HealthStatusMessage>();
                collection.EnsureIndex(x => x.ID);
                collection.Insert(msg);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Writes a line in color. </summary>
        ///
        /// <param name="message">          The message. </param>
        /// <param name="foregroundColor">  The foreground color. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void WriteLineInColor(string message, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
