using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Penman
{

    public class PenmanDailyFromCsv
    {
        private const double Pi = Math.PI;
        private const int H = 200;
        private const double fai = 25 * (Math.PI / 180);

        public void RunModel()
        {
            // 获取当前exe的运行目录
            string executExePath = AppDomain.CurrentDomain.BaseDirectory;
            var et0Table = new DataTable();
            var irrTable = new DataTable();
            et0Table = FileOperate.OpenFile(Path.Combine(executExePath, "et0input.csv"));
            if (et0Table == null)
            {
                return;
            }
            irrTable = FileOperate.OpenFile(Path.Combine(executExePath, "irrinput.csv"));
            if (irrTable == null)
            {
                return;
            }
            List<List<string>> et0Data = new List<List<string>>();
            et0Data.Add(new List<string> { "日期", "ET0" });
            Queue<Tuple<double, double>> previousData = new Queue<Tuple<double, double>>();
            for (int i = 0; i < et0Table.Rows.Count; i++)
            {
                string date = Convert.ToString(et0Table.Rows[i][0]);
                double Tmean = Convert.ToDouble(et0Table.Rows[i][1]);
                double ed = Convert.ToDouble(et0Table.Rows[i][2]);
                double U2 = Convert.ToDouble(et0Table.Rows[i][3]);
                double n = Convert.ToDouble(et0Table.Rows[i][4]);
                int J = Convert.ToInt32(et0Table.Rows[i][5]);

                double ea = 6.11 * Math.Exp(17.27 * Tmean / (Tmean + 237.3));
                double del_val = 4098 * ea / Math.Pow((Tmean + 237.3), 2);
                double lamda = 2501 - 2.361 * Tmean;
                double Press = 1013 * Math.Pow(((293 - 0.0065 * H) / 293), 5.26);
                double gama = 1.63 * Press / lamda;

                double dr = 1 + 0.033 * Math.Cos(2 * Pi / 365 * J);
                double delta = 0.409 * Math.Sin(2 * Pi / 365 * J - 1.39);

                // Calculate oumigas
                double x = -Math.Sin(fai) / Math.Cos(fai) * Math.Sin(delta) / Math.Cos(delta);
                double oumigas;
                if (x == 0)
                {
                    oumigas = Pi / 2;
                }
                else if (x > 0)
                {
                    oumigas = Math.Atan(Math.Sqrt(1 - Math.Pow(x, 2)) / x);
                }
                else
                {
                    oumigas = Math.Atan(Math.Sqrt(1 - Math.Pow(x, 2)) / x) + Pi;
                }
                // Calculate Nmax
                double Nmax = 24 / Pi * oumigas;

                // Calculate Ra
                double Ra = 37.6 / 2.45 * dr * (oumigas * Math.Sin(fai) * Math.Sin(delta) + Math.Cos(fai) * Math.Cos(delta) * Math.Sin(oumigas));

                // Calculate Rns
                double Rns = 0.77 * (0.25 + 0.5 * n / Nmax) * Ra;

                // Calculate Rnl
                double Rnl = 0.000000001 * (0.1 + 0.9 * n / Nmax) * (0.34 - 0.14 * Math.Sqrt(ed / 10)) * (Math.Pow((Tmean + 273.15), 4) + Math.Pow((Tmean + 273.15), 4));

                // Calculate Rn
                double Rn = Rns - Rnl;

                // Calculate G
                // 考虑过去三天的温度，可以更准确地估计土壤的热通量，从而更好地模拟地表能量平衡
                double G = 0.0;
                if (previousData.Count >= 3)
                {
                    double T_i1 = (previousData.Peek().Item1 + previousData.Peek().Item2) / 2;
                    previousData.Dequeue();

                    double T_i2 = (previousData.Peek().Item1 + previousData.Peek().Item2) / 2;
                    previousData.Dequeue();

                    double T_i3 = (previousData.Peek().Item1 + previousData.Peek().Item2) / 2;
                    previousData.Dequeue();

                    G = 0.1 / 2.45 * (Tmean - (T_i1 + T_i2 + T_i3) / 3);
                }


                // Calculate ETrad and ETaero
                double ETrad = 0.408 * del_val * (Rn - G) / (del_val + gama * (1 + 0.34 * U2));
                double ETaero = 90 / (Tmean + 273) * gama * U2 * (ea - ed) / (del_val + gama * (1 + 0.34 * U2));

                // Calculate ETP
                double ETP = ETrad + ETaero;

                // Add ET0 result to the list
                et0Data.Add(new List<string> { date, ETP.ToString() });

                // Store today's data for use in the next iteration
                previousData.Enqueue(new Tuple<double, double>(Tmean, ed));
            }


            List<double> initialWaterDepths = new List<double>();
            List<double> finalWaterDepths = new List<double>();
            List<double> irrigationWater = new List<double>();
            List<double> dischargeWater = new List<double>();
            for (int i = 0; i < irrTable.Rows.Count; i++)
            {
                double irrigation = 0.0;
                double discharge = 0.0;
                double currentWaterDepth = Convert.ToDouble(irrTable.Rows[i][8]);

                // 如果当前水层深度为0且
                if (currentWaterDepth == 0 && finalWaterDepths.Count > 0)
                {
                    currentWaterDepth = finalWaterDepths[finalWaterDepths.Count - 1];
                }
                initialWaterDepths.Add(currentWaterDepth);

                double Kc = Convert.ToDouble(irrTable.Rows[i][1]);
                double Et0 = Convert.ToDouble(et0Data[i + 1][1]);
                double S = Convert.ToDouble(irrTable.Rows[i][2]);
                double lowLimit = Convert.ToDouble(irrTable.Rows[i][3]);
                double upperLimit = Convert.ToDouble(irrTable.Rows[i][4]);
                double maxStorage = Convert.ToDouble(irrTable.Rows[i][5]);
                double P = Convert.ToDouble(irrTable.Rows[i][6]);

                // Predict the next day's water depth
                double nextWaterDepth = currentWaterDepth + P - S - Kc * Et0;

                // Adjust irrigation water based on lower_limit and upper_limit
                if (nextWaterDepth < lowLimit)
                {
                    irrigation = upperLimit - nextWaterDepth;
                    nextWaterDepth = upperLimit;
                }

                if (nextWaterDepth > maxStorage)
                {
                    discharge = nextWaterDepth - maxStorage;
                    nextWaterDepth = maxStorage;
                }

                // Store initial and final water depths, irrigation, and discharge
                finalWaterDepths.Add(nextWaterDepth);
                irrigationWater.Add(irrigation);
                dischargeWater.Add(discharge);
            }

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(executExePath, "output_irrigation.csv")))
            {
                if (outputFile == null)
                {
                    Console.WriteLine("Error opening output file.");
                    return;
                }

                outputFile.WriteLine("日期,时段初水层深度,Kc,ET0,渗漏损失S,适宜水层下限,适宜水层上限,当天降雨,灌溉水量,最大蓄水,当天排水,时段末水层深度");

                // Start writing data from the beginning of the water balance data
                for (int i = 0; i < irrTable.Rows.Count; i++)
                {
                    outputFile.WriteLine($"{irrTable.Rows[i][0]},{initialWaterDepths[i]},{irrTable.Rows[i][1]},{et0Data[i + 1][1]},{irrTable.Rows[i][2]},{irrTable.Rows[i][3]},{irrTable.Rows[i][4]},{irrTable.Rows[i][6]},{irrigationWater[i]},{irrTable.Rows[i][5]},{dischargeWater[i]},{finalWaterDepths[i]}");
                }
            }
            Console.WriteLine("Conversion completed successfully.");
        }


    }
}
