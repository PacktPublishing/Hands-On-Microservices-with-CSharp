using System;
using System.Diagnostics;
using System.Runtime;

namespace MicroServiceEcoSystem.Memory_MicroService
{
    using System.Timers;
    using JetBrains.Annotations;
    using Topshelf;

    public class MemoryMicroService : BaseMicroService
    {
        private HostControl hc;
        private Timer _timer = null;

        public bool Start([CanBeNull] HostControl host)
        {
            base.Start(host);
            hc = host;
            const double interval = 60000;
            _timer = new Timer(interval);
            _timer.Elapsed += OnTick;

            Console.WriteLine(string.Intern("MemoryMicroService Started."));
            return (true);
        }

        
        public bool Stop()
        {
            base.Stop();
            return true;
        }

        protected virtual void OnTick(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(string.Intern("Reclaiming Memory"));
            ReclaimMemory();
        }

        /// <summary>   Reclaim memory. </summary>
        public static void ReclaimMemory()
        {
            long mem2 = GC.GetTotalMemory(false);

            Debug.Print(string.Intern("*** Memory ***"));
            Debug.Print(string.Intern("\tMemory before GC: ") + ToBytes(mem2));
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long mem3 = GC.GetTotalMemory(false);
            Debug.Print(string.Intern("\tMemory after GC: ") + ToBytes(mem3));
            Debug.Print("\tApp memory being used: " + ToBytes(Environment.WorkingSet));
            for (int x = 0; x < GC.MaxGeneration; x++)
            {
                Debug.Print("\t\tGeneration " + (x) + " Collection Count: " + GC.CollectionCount(x));
            }

            const string category = ".NET CLR Memory";
            const string counter = "% Time in GC";
            string instance = Process.GetCurrentProcess().ProcessName;

            if (PerformanceCounterCategory.Exists(category) && PerformanceCounterCategory.CounterExists(counter, category) &&
                PerformanceCounterCategory.InstanceExists(instance, category))
            {
                var gcPerf = new PerformanceCounter(category, counter, instance);
                float percent = gcPerf.NextValue();

                string suffix = "%";
                if (percent > 50.0)
                {
                    suffix += " <- High Watermark Warning";
                }
                Debug.Print("\t\tTime Spent in GC: " + $"{percent:00.##}" + suffix);
            }

            Debug.Print(string.Intern("*** Memory ***"));
        }

        [NotNull]
        private static string ToBytes(double value)
        {
            string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
            for (int i = 0; i < suffixes.Length; i++)
            {
                if (value <= (Math.Pow(1024, i + 1)))
                {
                    return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
            }

            return ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " + suffixes[suffixes.Length - 1];
        }

        [NotNull]
        private static string ThreeNonZeroDigits(double value)
        {
            return value >= 100 ? value.ToString("0,0") : value.ToString(value >= 10 ? "0.0" : "0.00");

            // Two digits after the decimal.
        }
    }
}
