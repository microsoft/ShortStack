using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spikes
{
    class Program
    {
        static void Main(string[] args)
        {
            var tester = new FooTech();
            tester.Run();

            Console.WriteLine("Done.  Press <Enter> to exit.");
            Console.Read();
        }
    }
}
