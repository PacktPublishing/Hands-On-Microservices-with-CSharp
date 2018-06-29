namespace FileSystemMonitorMicroService
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using CodeContracts;
    using CommonMessages;
    using Topshelf;
    using Topshelf.FileSystemWatcher;
    using RabbitMQ.Client;
    using EasyNetQ;
    using EasyNetQ.Topology;
    using NodaTime;
    using ExchangeType = RabbitMQ.Client.ExchangeType;
    using EasyNetQ.Migrations;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A program. </summary>
    ///
    /// <seealso cref="T:EasyNetQ.Migrations.Migration"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    internal class Program : Migration
    {
        /// <summary>   The test dir. </summary>
        private static readonly string _testDir = Directory.GetCurrentDirectory() + @"\test\";
        /// <summary>   True to include, false to exclude the sub directories. </summary>
        private static readonly bool _includeSubDirectories = true;
        /// <summary>   True to exclude, false to include the duplicate events. </summary>
        private static readonly bool _excludeDuplicateEvents = true;
        /// <summary>   The connection factory. </summary>
        static ConnectionFactory _connectionFactory;
        /// <summary>   The connection. </summary>
        static IConnection _connection;
        /// <summary>   The channel. </summary>
        static IModel _channel;
        /// <summary>   The bus. </summary>
        private static IBus _bus;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this application. </summary>
        ///
        /// <param name="args"> An array of command-line argument strings. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void Main(string[] args)
        {
            HostFactory.Run(config =>
            {
                config.Service<Program>(s =>
                {
                    s.ConstructUsing(() => new Program());
                    s.BeforeStartingService((hostStart) =>
                    {
                        _bus = RabbitHutch.CreateBus("host=localhost");
                        if (!Directory.Exists(_testDir))
                            Directory.CreateDirectory(_testDir);
                    });
                    s.WhenStarted((service, host) => true);
                    s.WhenStopped((service, host) => true);
                    s.WhenFileSystemCreated(ConfigureDirectoryWorkCreated, FileSystemCreated);
                    s.WhenFileSystemChanged(ConfigureDirectoryWorkChanged, FileSystemCreated);
                    s.WhenFileSystemRenamed(ConfigureDirectoryWorkRenamedFile, FileSystemRenamedFile);
                    s.WhenFileSystemRenamed(ConfigureDirectoryWorkRenamedDirectory, FileSystemRenamedDirectory);
                    s.WhenFileSystemDeleted(ConfigureDirectoryWorkDeleted, FileSystemCreated);
                });
            });
            Console.ReadKey();
        }

        /// <summary>   Applies this object. </summary>
        public override void Apply()
        {
            Declare.Exchange("EvolvedAI")
                .OnVirtualHost("/")
                .AsType(EasyNetQ.Migrations.ExchangeType.Topic)
                .Durable();

            Declare.Queue("FileSystem")
                .OnVirtualHost("/")
                .Durable();

            Declare.Binding()
                .OnVirtualHost("/")
                .FromExchange("EvolvedAI")
                .ToQueue("FileSystem")
                .RoutingKey("#");
        }

        /// <summary>   Subscribes this object. </summary>
        public void Subscribe()
        {
            _bus = RabbitHutch.CreateBus("host=localhost");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure directory work created. </summary>
        ///
        /// <param name="obj">  The object. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void ConfigureDirectoryWorkCreated(FileSystemWatcherConfigurator obj)
        {
            obj.AddDirectory(dir =>
            {
                dir.Path = _testDir;
                dir.IncludeSubDirectories = _includeSubDirectories;
                dir.NotifyFilters = NotifyFilters.DirectoryName | NotifyFilters.FileName;
                dir.ExcludeDuplicateEvents = _excludeDuplicateEvents;
            });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure directory work changed. </summary>
        ///
        /// <param name="obj">  The object. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void ConfigureDirectoryWorkChanged(FileSystemWatcherConfigurator obj)
        {
            obj.AddDirectory(dir =>
            {
                dir.Path = _testDir;
                dir.IncludeSubDirectories = _includeSubDirectories;
                dir.NotifyFilters = NotifyFilters.LastWrite;
                dir.ExcludeDuplicateEvents = _excludeDuplicateEvents;
            });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure directory work renamed file. </summary>
        ///
        /// <param name="obj">  The object. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void ConfigureDirectoryWorkRenamedFile(FileSystemWatcherConfigurator obj)
        {
            obj.AddDirectory(dir =>
            {
                dir.Path = _testDir;
                dir.IncludeSubDirectories = _includeSubDirectories;
                dir.NotifyFilters = NotifyFilters.FileName;
                dir.ExcludeDuplicateEvents = _excludeDuplicateEvents;
            });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure directory work renamed directory. </summary>
        ///
        /// <param name="obj">  The object. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void ConfigureDirectoryWorkRenamedDirectory(FileSystemWatcherConfigurator obj)
        {
            obj.AddDirectory(dir =>
            {
                dir.Path = _testDir;
                dir.IncludeSubDirectories = _includeSubDirectories;
                dir.NotifyFilters = NotifyFilters.DirectoryName;
                dir.ExcludeDuplicateEvents = _excludeDuplicateEvents;
            });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Configure directory work deleted. </summary>
        ///
        /// <param name="obj">  The object. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void ConfigureDirectoryWorkDeleted(FileSystemWatcherConfigurator obj)
        {
            obj.AddDirectory(dir =>
            {
                dir.Path = _testDir;
                dir.IncludeSubDirectories = _includeSubDirectories;
                dir.NotifyFilters = NotifyFilters.DirectoryName | NotifyFilters.FileName;
                dir.ExcludeDuplicateEvents = _excludeDuplicateEvents;
            });
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   File system created. </summary>
        ///
        /// <param name="topshelfFileSystemEventArgs">  Topshelf file system event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void FileSystemCreated(TopshelfFileSystemEventArgs topshelfFileSystemEventArgs)
        {
            FileSystemChangeMessage m = new FileSystemChangeMessage
            {
                ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                ChangeType = (int)topshelfFileSystemEventArgs.ChangeType,
                EventType = (int)topshelfFileSystemEventArgs.FileSystemEventType,
                FullPath = topshelfFileSystemEventArgs.FullPath,
                OldPath = topshelfFileSystemEventArgs.OldFullPath,
                Name = topshelfFileSystemEventArgs.Name,
                OldName = topshelfFileSystemEventArgs.OldName
            };
            _bus.Publish(m, "FileSystemChanges");

            Console.WriteLine("*********************");
            Console.WriteLine("ChangeType = {0}", topshelfFileSystemEventArgs.ChangeType);
            Console.WriteLine("FullPath = {0}", topshelfFileSystemEventArgs.FullPath);
            Console.WriteLine("Name = {0}", topshelfFileSystemEventArgs.Name);
            Console.WriteLine("FileSystemEventType {0}", topshelfFileSystemEventArgs.FileSystemEventType);
            Console.WriteLine("*********************");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   File system renamed file. </summary>
        ///
        /// <param name="topshelfFileSystemEventArgs">  Topshelf file system event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void FileSystemRenamedFile(TopshelfFileSystemEventArgs topshelfFileSystemEventArgs)
        {
            FileSystemChangeMessage m = new FileSystemChangeMessage
            {
                ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                ChangeType = (int)topshelfFileSystemEventArgs.ChangeType,
                EventType = (int)topshelfFileSystemEventArgs.FileSystemEventType,
                FullPath = topshelfFileSystemEventArgs.FullPath,
                OldPath = topshelfFileSystemEventArgs.OldFullPath,
                Name = topshelfFileSystemEventArgs.Name,
                OldName = topshelfFileSystemEventArgs.OldName
            };
            _bus.Publish(m, "FileSystemChanges");

            Console.WriteLine("*********************");
            Console.WriteLine("Rename File");
            Console.WriteLine("ChangeType = {0}", topshelfFileSystemEventArgs.ChangeType);
            Console.WriteLine("FullPath = {0}", topshelfFileSystemEventArgs.FullPath);
            Console.WriteLine("Name = {0}", topshelfFileSystemEventArgs.Name);
            Console.WriteLine("FileSystemEventType {0}", topshelfFileSystemEventArgs.FileSystemEventType);
            Console.WriteLine("*********************");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   File system renamed directory. </summary>
        ///
        /// <param name="topshelfFileSystemEventArgs">  Topshelf file system event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static void FileSystemRenamedDirectory(TopshelfFileSystemEventArgs topshelfFileSystemEventArgs)
        {
            FileSystemChangeMessage m = new FileSystemChangeMessage
            {
                ChangeDate = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime(),
                ChangeType = (int)topshelfFileSystemEventArgs.ChangeType,
                EventType = (int)topshelfFileSystemEventArgs.FileSystemEventType,
                FullPath = topshelfFileSystemEventArgs.FullPath,
                OldPath = topshelfFileSystemEventArgs.OldFullPath,
                Name = topshelfFileSystemEventArgs.Name,
                OldName = topshelfFileSystemEventArgs.OldName
            };
            _bus.Publish(m, "FileSystemChanges");

            Console.WriteLine("*********************");
            Console.WriteLine("Rename Dir");
            Console.WriteLine("ChangeType = {0}", topshelfFileSystemEventArgs.ChangeType);
            Console.WriteLine("FullPath = {0}", topshelfFileSystemEventArgs.FullPath);
            Console.WriteLine("Name = {0}", topshelfFileSystemEventArgs.Name);
            Console.WriteLine("FileSystemEventType {0}", topshelfFileSystemEventArgs.FileSystemEventType);
            Console.WriteLine("*********************");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a topology. </summary>
        ///
        /// <param name="exchange">     The exchange. </param>
        /// <param name="queue">        The queue. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void CreateTopology(string exchange, string queue, string routingID = "")
        {
            _bus = RabbitHutch.CreateBus("host=localhost");
            IExchange exch = _bus.Advanced.ExchangeDeclare(exchange, ExchangeType.Topic);
            IQueue q = _bus.Advanced.QueueDeclare(queue);
            _bus.Advanced.Bind(exch, q, "");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Publish message. </summary>
        ///
        /// <param name="msg">          The message. </param>
        /// <param name="exchange">     The exchange. </param>
        /// <param name="routingID">    (Optional) Identifier for the routing. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void PublishMessage(object msg, string exchange, string routingID = "")
        {
            _bus.Publish(msg, "FileSystem");
        }
    }
}
