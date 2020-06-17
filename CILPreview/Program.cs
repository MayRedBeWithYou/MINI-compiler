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
            int i = 5;
            double d = 123.456;
            bool b = true;
            Console.Write(i);
            Console.Write("\n");
            Console.Write(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:0.000000}", d));
            Console.Write("\n");
            Console.Write(b);
            Console.Write("\n");
        }
    }
}
