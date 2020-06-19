using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CILPreview
{
    class Program
    {
        static void Main(string[] args)
        {
            int a = int.Parse(Console.ReadLine());
            while (a > 5)
            {
                Console.Write(a);
                Console.Write("\n");
                a = a - 1;
            }
        }
    }
}
