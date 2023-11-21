using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penman
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("----------------------单一数据的日需水计算结果---------------------------");
            PenmanDaily penmanDaily = new PenmanDaily();
            penmanDaily.RunModel();
            Console.WriteLine("------------------------多数据日需水计算结果-----------------------------");
            PenmanDailyFromCsv penmanDailyFromCsv = new PenmanDailyFromCsv();
            penmanDailyFromCsv.RunModel();
            Console.WriteLine("----------------------单一数据生长期需水计算结果-------------------------");
            PenmanGrowthPred penmanGrowPred = new PenmanGrowthPred();
            penmanGrowPred.RunModel();
        }
    }
}
