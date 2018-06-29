using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem.Basic_MicroService
{
    public class Worker
    {
        private Guid _workerId;

        public Worker()
        {
            _workerId = Guid.NewGuid();
        }

        public void Work()
        {
            Console.WriteLine("I am working. My id is {0}.", _workerId);

            Console.WriteLine("  Step 1");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("  Step 2");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("  Step 3");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("  Step 4");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("  Step 5");
            System.Threading.Thread.Sleep(1000);
        }
    }
}
