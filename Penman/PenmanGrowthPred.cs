using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penman
{
    internal class PenmanGrowthPred
    {
        // 计算最大灌水定额
        static double CalculateMaxIrrigationQuota(double soilBulkDensity, double planMoistureDepth, double designSoilMoistureRatio,
                                       double maxSoilMoistureContent, double minSoilMoistureContent)
        {
            return 0.001 * soilBulkDensity * planMoistureDepth * designSoilMoistureRatio * (maxSoilMoistureContent - minSoilMoistureContent);
        }

        // 计算设计净灌水定额
        static double CalculateDesignIrrigationQuota(int IrrigationPeriod, double designDailyConsumptionIntensity)
        {
            return IrrigationPeriod * designDailyConsumptionIntensity;
        }

        // 计算毛灌水定额
        static double CalculateGrossIrrigationQuota(double designIrrigationQuota, double irrigationEfficiency)
        {
            return designIrrigationQuota / irrigationEfficiency;
        }

        // 计算最大灌水周期
        static double CalculateMaxIrrigationPeriod(double grossIrrigationQuota, double designDailyConsumptionIntensity)
        {
            return grossIrrigationQuota / designDailyConsumptionIntensity;
        }

        public void RunModel()
        {
            double gamma = 1.45;
            double z = 0.5;
            double p = 0.5;
            double theta_max = 0.225;
            double theta_min = 0.1625;
            double ita = 0.9;
            double I = 3;

            // 计算最大灌水定额
            double m_max = CalculateMaxIrrigationQuota(gamma, z, p, theta_max, theta_min);
            Console.WriteLine("最大灌水定额：" + m_max);
            // 计算灌水周期
            int T = (int)CalculateMaxIrrigationPeriod(m_max, I);
            Console.WriteLine("灌水周期：" + T);
            // 计算设计净灌水定额(mm)
            double m = CalculateDesignIrrigationQuota(T, I);
            Console.WriteLine("设计净灌水定额(mm)：" + m);
            // 计算设计灌水定额(m3/亩)
            double m1 = m * 0.001 * 666.7;
            Console.WriteLine("设计灌水定额(m3/亩)：" + m1);
            // 计算设计毛灌水定额
            double m_mao = CalculateGrossIrrigationQuota(m, ita);

            double m1_mao = m_mao * 0.001 * 666.7;
            Console.WriteLine("设计毛灌水定额(m3/亩)：" + T);
        }
    }
}
