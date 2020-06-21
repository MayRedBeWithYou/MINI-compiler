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
            double d = double.Parse(Console.ReadLine());
            Console.Write(string.Format(
            System.Globalization.CultureInfo.InvariantCulture, "{0:0.000000}", d));

        }
    }
}
