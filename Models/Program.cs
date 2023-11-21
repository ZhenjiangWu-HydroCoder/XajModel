using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XajModel
{
    class Program
    {
        static void Main(string[] args)
        {
            List<double> arrP = new List<double> { 10, 24.1, 20.4, 18.3, 10.1, 5.5, 0.6, 3.1, 1.9, 4.6, 5, 4.8, 36.2, 29, 6, 3.6, 0.4, 0, 0.5, 3.8, 0, 1.8, 0.2, 0.3 };
            List<double> arrE0 = new List<double> { 0.1, 0.0, 0.1, 0.5, 0.7, 0.9, 0.8, 0.7, 0.5, 0.3, 0.2, 0.1, 0.0, 0.0, 0.1, 0.6, 0.8, 1.0, 0.9, 0.8, 0.7, 0.5, 0.3, 0.1 };

            XajModel xajModel = new XajModel();
            var result = xajModel.RunModel(arrP, arrE0);
            List<double> Q = result.Item1.Zip(result.Item2, (qs, qss) => qs + qss).Zip(result.Item3, (sum, qg) => sum + qg).ToList();

            foreach (var value in Q)
            {
                Console.WriteLine(value);
            }
        }
    }
}
