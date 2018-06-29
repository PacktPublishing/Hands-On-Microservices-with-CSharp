using System;
using System.Diagnostics;
using System.Runtime;

namespace MicroServiceEcoSystem.Memory_MicroService
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Timers;
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.AutoSubscribe;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using JetBrains.Annotations;
    using RabbitMQ.Client;
    using Topshelf;
    using ExchangeType = RabbitMQ.Client.ExchangeType;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A memory micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{MicroServiceEcoSystem.Memory_MicroService.MemoryMicroService, CommonMessages.MemoryUpdateMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class MemoryMicroService : BaseMicroService<MemoryMicroService, MemoryUpdateMessage>
    {

        /// <summary>   The timer. </summary>
        private Timer _timer = null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the
        /// MicroServiceEcoSystem.Memory_MicroService.MemoryMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MemoryMicroService()
        {
            Name = "Memory Microservice_" + Environment.MachineName;
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
            base.Start(host);
            Host = host;
            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;
            _timer.AutoReset = true;
            _timer.Start();
            Subscribe();
            Console.WriteLine(string.Intern("MemoryMicroService Started."));
            return (true);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            _timer.Stop();

            base.Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Continues this object. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
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
        /// <summary>   Raises the elapsed event. </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void OnTick(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(string.Intern("Reclaiming Memory"));
            ReclaimMemory();
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
            IQueue queue = Bus.Advanced.QueueDeclare("Memory");
            Bus.Advanced.Bind(exchange, queue, "");
        }

        /// <summary>   Reclaim memory. </summary>
        public void ReclaimMemory()
        {
            long mem2 = GC.GetTotalMemory(false);

            Console.WriteLine(string.Intern("*** Memory ***"));
            Console.WriteLine(string.Intern("\tMemory before GC: ") + ToBytes(mem2));
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long mem3 = GC.GetTotalMemory(false);
            Console.WriteLine(string.Intern("\tMemory after GC: ") + ToBytes(mem3));
            Console.WriteLine("\tApp memory being used: " + ToBytes(Environment.WorkingSet));
            int gen1=0;
            int gen2=0;
            for (int x = 0; x < GC.MaxGeneration; x++)
            {
                if (x == 0)
                    gen1 = GC.CollectionCount(x);
                else if (x == 1)
                    gen2 = GC.CollectionCount(x);
                Console.WriteLine("\t\tGeneration " + (x) + " Collection Count: " + GC.CollectionCount(x));
            }

            const string category = ".NET CLR Memory";
            const string counter = "% Time in GC";
            string instance = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            float percent= 0.0F;

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
                Console.WriteLine("\t\tTime Spent in GC: " + $"{percent:00.##}" + suffix);
            }

            PublishMemoryUpdateMessage(gen1, gen2, percent, ToBytes(mem2), ToBytes(mem3));
            Console.WriteLine(string.Intern("*** Memory ***"));
        }
    }
}
