using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penman
{
    public class PenmanDaily
    {
        private double H, latitudeDeg, Tmax, Tmin, Tdew, RHmax, RHmin, uz, Krs, precipitation,
            Kc, W0_theta, z, theta_max, theta_min, plot_area;
        private int J;

        public PenmanDaily()
        {
            InitData();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitData()
        {
            // 海拔高程、纬度
            H = 2095;
            latitudeDeg = 40.24489;
            // 气象输入
            // 最高气温、最低气温、露点温度、最高相对湿度、最低相对湿度、风速、日序数、日照时数、降雨量
            Tmax = 20;
            Tmin = 6;
            Tdew = 0.0;
            RHmax = 55;
            RHmin = 11;
            uz = 0.3021;
            J = 285;
            Krs = 0.17;
            precipitation = 0;
            // 作物生育期综合系数
            Kc = 0.8;
            // 土壤参数
            // 初始土壤含水率、计划土层深度、最大适宜土壤含水率、最小土壤含水率
            W0_theta = 28.6;
            z = 50;
            theta_max = 19.8;
            theta_min = 13.2;
            plot_area = 30;
        }

        /// <summary>
        /// 计算两米处的风速
        /// </summary>
        /// <param name="windSpeedZ">Z米处的风速</param>
        /// <param name="levelZ">Z米的高程</param>
        /// <returns></returns>
        private double CalculateWindSpeed(double windSpeedZ, double levelZ)
        {
            double windSpeed2 = windSpeedZ * (4.87 / Math.Log(67.8 * levelZ - 5.42));
            return windSpeed2;
        }

        /// <summary>
        /// 计算平均温度
        /// </summary>
        /// <param name="tMax">最高温度</param>
        /// <param name="tMin">最低温度</param>
        /// <returns></returns>
        private double CalculateAverageTemperature(double tMax, double tMin)
        {
            double T = (Tmax + Tmin) / 2;
            return T;
        }

        /// <summary>
        /// 计算大气压
        /// </summary>
        /// <param name="z">海拔高程(m)</param>
        /// <returns></returns>
        private double CalculateAtmosphericPressure(double z)
        {
            const double standardPressure = 101.3;
            const double temperatureLapseRate = 0.0065;
            double P = standardPressure * Math.Pow((293 - temperatureLapseRate * z) / 293, 5.26);
            return P;
        }

        /// <summary>
        /// 计算饱和水汽压
        /// </summary>
        /// <param name="T">平均温度/露点温度/最高温度/最低温度</param>
        /// <returns></returns>
        private double CalculateSaturationVaporPressure(double tMean)
        {
            return 0.6108 * Math.Exp((17.27 * tMean) / (tMean + 237.3));
        }

        /// <summary>
        /// 计算饱和水汽压曲线的斜率
        /// </summary>
        /// <param name="T">平均温度/露点温度/最高温度/最低温度</param>
        /// <returns></returns>
        private double CalculateSaturationVaporPressureSlope(double T)
        {
            double es = CalculateSaturationVaporPressure(T);
            double slope = 4098 * es / Math.Pow((T + 237.3), 2);
            return slope;
        }

        /// <summary>
        /// 计算湿度计常数
        /// </summary>
        /// <param name="P">大气压</param>
        /// <returns></returns>
        private double CalculateTemperatureSensorConstant(double P)
        {
            return 0.665 * 1e-3 * P;
        }

        /// <summary>
        /// 计算平均饱和水汽压
        /// </summary>
        /// <param name="tMax"></param>
        /// <param name="tMin"></param>
        /// <returns></returns>
        private double CalculateAverageSaturationVaporPressure(double tMax, double tMin)
        {
            double es_tmax = CalculateSaturationVaporPressure(tMax);
            double es_tmin = CalculateSaturationVaporPressure(tMin);
            double es = (es_tmax + es_tmin) / 2;
            return es;
        }

        /// <summary>
        /// 计算实际饱和水汽压
        /// </summary>
        /// <param name="tMax">最高气温</param>
        /// <param name="tMin">最低气温</param>
        /// <param name="relativeHumidityMax">最高相对湿度</param>
        /// <param name="relativeHumidityMin">最低相对湿度</param>
        /// <returns></returns>
        private double CalculateActualVaporPressure(double tMax, double tMin, double relativeHumidityMax, double relativeHumidityMin)
        {
            double es_tmax = CalculateSaturationVaporPressure(tMax);
            double es_tmin = CalculateSaturationVaporPressure(tMin);
            return (es_tmax * relativeHumidityMin / 100 + es_tmin * relativeHumidityMax / 100) / 2;
        }

        /// <summary>
        /// 计算实际饱和水汽压
        /// </summary>
        /// <param name="tDew">露点温度</param>
        /// <returns></returns>
        private double CalculateActualVaporPressure(double tDew)
        {
            return CalculateSaturationVaporPressure(tDew);
        }

        /// <summary>
        /// 计算净辐射
        /// </summary>
        /// <param name="latitudeDeg">纬度</param>
        /// <param name="J">日序数</param>
        /// <param name="Krs">日照调节系数，通常为0.16-0.19，这里取0.17</param>
        /// <param name="z">计划土层深度</param>
        /// <param name="T_max">最高温度</param>
        /// <param name="T_min">最低温度</param>
        /// <param name="ea"></param>
        /// <returns>净辐射</returns>
        private double CalculateNetRadiation(double latitudeDeg, int J, double Krs, double z, double T_max, double T_min, double ea)
        {
            // 计算纬度弧度值
            double latitudeRad = latitudeDeg * Math.PI / 180.0;
            // 计算日地相对距离倒数
            double distanceFactor = 1.0 + 0.033 * Math.Cos(2.0 * Math.PI / 365.0 * J);
            // 计算太阳磁偏角
            double solarDeclination = 0.409 * Math.Sin(2.0 * Math.PI * J / 365.0 - 1.39);
            // 计算日落时角
            double sunsetHourAngle = Math.Acos(-Math.Tan(latitudeRad) * Math.Tan(solarDeclination));
            // 太阳常数，通常为0.0820
            const double Gsc = 0.0820;
            // 计算天顶辐射
            double Ra = (24.0 * 60.0 / Math.PI) * Gsc * distanceFactor * (sunsetHourAngle * Math.Sin(latitudeRad) * Math.Sin(solarDeclination) + Math.Cos(latitudeRad) * Math.Cos(solarDeclination) * Math.Sin(sunsetHourAngle));
            // 计算白昼时间
            double N = 24.0 * sunsetHourAngle / Math.PI;
            // 计算太阳辐射
            double Rs = Krs * Math.Sqrt(T_max - T_min) * Ra;
            // 计算晴空太阳辐射
            double Rso = (0.75 + 2.0 * 1e-5 * z) * Ra;
            // 计算净短波辐射
            double Rns = (1.0 - 0.23) * Rs;
            // 计算净长波辐射
            double sigma = 4.903e-9;
            double Rnl = sigma * ((Math.Pow(T_max + 273.16, 4) + Math.Pow(T_min + 273.16, 4)) / 2.0) * (0.34 - 0.14 * Math.Sqrt(ea)) * (1.35 * (Rs / Rso) - 0.35);
            // 计算净辐射
            double Rn = Rns - Rnl;
            return Rn;
        }

        /// <summary>
        /// 计算日尺度参考作物腾发量
        /// </summary>
        /// <param name="Rn">净辐射</param>
        /// <param name="G">土壤热通量</param>
        /// <param name="delta">饱和水汽压曲线斜率</param>
        /// <param name="gamma">湿度计常数</param>
        /// <param name="T">平均温度</param>
        /// <param name="u2">两米处风速</param>
        /// <param name="es">平均饱和水汽压</param>
        /// <param name="ea">实际饱和水汽压</param>
        /// <returns></returns>
        private double CalculateET0(double Rn, double G, double delta, double gamma,
                    double T, double u2, double es, double ea)
        {
            double numerator = (0.408 * delta * (Rn - G) + gamma * 900 / (T + 273) * u2 * (es - ea));
            double denominator = (delta + gamma * (1 + 0.34 * u2));
            double ET0 = numerator / denominator;
            return ET0;
        }

        /// <summary>
        /// 计算日尺度灌溉需水
        /// </summary>
        /// <param name="theta0">初始土壤含水率</param>
        /// <param name="theta_min">最小土壤适宜土壤含水率</param>
        /// <param name="theta_max">最大土壤适宜土壤含水率</param>
        /// <param name="H">计划土层深度</param>
        /// <param name="P">降水</param>
        /// <param name="ET">实际蒸发（乘以作物生育期综合系数后的）</param>
        /// <returns></returns>
        private double CalculateIrrigationRequirement(double theta0, double theta_min, double theta_max, double H, double P, double ET)
        {
            H *= 10;
            double sigma = 1;
            if (P < 5)
                sigma = 0;
            else if (P > 50)
                sigma = 0.75;
            double P0 = sigma * P;
            double W0 = H * theta0 / 100;
            double Wt = W0 + P0 - ET;
            double M = 0.0;
            double Wt_theta = 100 * Wt / H;
            if (Wt_theta < 0 || Wt_theta < theta_min)
            {
                M = ET - P0 - (W0 - H * theta_max / 100);
            }
            return M;
        }

        public void RunModel()
        {
            // 计算两米处风速
            double u2 = CalculateWindSpeed(uz, 10);
            // 计算平均温度
            double Tmean = CalculateAverageTemperature(Tmax, Tmin);
            // 计算大气压
            double P = CalculateAtmosphericPressure(H);
            // 计算饱和水汽压曲线的斜率
            double delta = CalculateSaturationVaporPressureSlope(Tmean);
            // 计算湿度计常数
            double gamma = CalculateTemperatureSensorConstant(P);
            // 计算平均饱和水汽压
            double es = CalculateAverageSaturationVaporPressure(Tmax, Tmin);
            // 计算实际水汽压两种方式
            // 1. 输入最高气温、最低气温、最高相对湿度、最低相对湿度
            double ea = CalculateActualVaporPressure(Tmax, Tmin, RHmax, RHmin);
            // 2. 输入露点温度
            // double ea = CalculateActualVaporPressure(Tdew);
            // 计算净辐射
            double Rn = CalculateNetRadiation(latitudeDeg, J, Krs, H, Tmax, Tmin, ea);
            // 计算日尺度参考作物腾发量
            double ET0 = CalculateET0(Rn, 0, delta, gamma, Tmean, u2, es, ea);
            // 计算ET
            double ET = Kc * ET0;
            Console.WriteLine("作物腾发量：" + ET);
            // 灌溉水量
            double M = CalculateIrrigationRequirement(W0_theta, theta_min, theta_max, z, precipitation, ET);
            // 换算单位为m3/亩
            double M1 = M * 0.001 * 666.7;
            // 全区域的灌溉需水量
            double W = M1 * plot_area;
            Console.WriteLine("灌溉需水量：" + W);
        }
    }
}
