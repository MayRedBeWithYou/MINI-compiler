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
            int i1;
            int i2;
            double d1;
            bool b1;
            i1 = 2;
            i2 = int.Parse(Console.ReadLine());
            d1 = double.Parse(Console.ReadLine());
            b1 = bool.Parse(Console.ReadLine());
            i1 = i2 + i1;
            i2 = i1 * i1;
            i1 = i2 - i1;
            i2 = i2 / i1;
            i2 = i1;
            if (b1)
            {
                int i3 = 1;
                i3 = i3 + 1;
                i2 = i3 - 10;
                b1 = false;
            }
            Console.Write(i1);
            Console.Write(i2);
            Console.Write(d1);
            Console.Write(b1);
        }
    }
}
