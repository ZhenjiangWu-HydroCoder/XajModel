using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XajModel
{
    public class XajModel
    {
        private double _WUM, _WLM, _WDM, _WM, _B;   //张力水参数
        private double _K, _C;  //蒸散发参数
        private double _EX, _KG, _KSS, _SM; //自由水参数
        //汇流参数
        private double _DT, _KKG, _KKSS;
        private double[] _UH = new double[] { };

        private double _F, _WU, _WL, _WD, _W, _FR, _S, _QSS0, _QG0;

        public XajModel(double F = 537, double WU0 = 0, double WL0 = 70, double WD0 = 80, double S = 20, double QSS0 = 40, double QG0 = 20)
        {
            ReadData();
            _F = F;
            _WU = WU0;
            _WL = WL0;
            _WD = WD0;
            _W = WU0 + WL0 + WD0;
            _FR = 0.1;
            _S = S;
            _QSS0 = QSS0;
            _QG0 = QG0;
        }

        /// <summary>
        /// 初始化数据，可变为从外部读入
        /// </summary>
        protected virtual void ReadData()
        {
            //张力水参数
            _WUM = 20;
            _WLM = 75;
            _WDM = 80;
            _WM = _WUM + _WLM + _WDM;
            _B = 0.3;
            //蒸散发参数
            _K = 0.65;
            _C = 0.11;
            //自由水参数
            _EX = 1;
            _KG = 0.3;
            _KSS = 0.41;
            _SM = 20;
            //汇流参数
            _DT = 2;
            _KKG = 0.99;
            _KKSS = 0.6;
            _UH = new double[] { 0.3, 0.6, 0.1 };
        }

        #region 产流部分

        /// <summary>
        /// 三层蒸发计算
        /// </summary>
        /// <param name="p">降水量</param>
        /// <param name="e0">蒸发量</param>
        /// <returns></returns>
        private Tuple<double, double[]> Evapor3Lyrs(double p, double e0)
        {
            double k = _K, c = _C, wlm = _WLM;
            double wu = _WU, wl = _WL;
            double em = k * e0; // 蒸散发能力
            double eu, el, ed, e;

            if ((p + wu) >= em)
            {
                // 降水+上层土壤蓄水量 >= 蒸散发
                eu = em;
                el = 0;
                ed = 0;
            }
            else
            {
                // 降水+上层土壤蓄水量 < 蒸散发
                eu = p + wu;
                if (wl / wlm >= c)
                {
                    // 不发生深层蒸发
                    el = (em - eu) * (wl / wlm);
                    ed = 0;
                }
                else
                {
                    // 只有当el < c(em-eu) && wl < c(em-eu)才发生深层蒸发
                    if (wl / (em - eu) >= c)
                    {
                        el = c * (em - eu);
                        ed = 0;
                    }
                    else
                    {
                        el = wl;
                        ed = c * (em - eu) - el;
                    }
                }
            }

            e = eu + el + ed;
            return new Tuple<double, double[]>(e, new double[] { eu, el, ed });
        }

        /// <summary>
        /// 蓄满产流计算
        /// </summary>
        /// <param name="p">降水量</param>
        /// <param name="e">蒸发量</param>
        /// <returns></returns>
        private double RunoffDunne(double p, double e)
        {
            double wm = _WM, b = _B, w = _W;
            double wwmm = wm * (1 + b);
            double a = wwmm * (1 - Math.Pow(1 - w / wm, 1 / (1 + b)));
            double r;

            if (p - e <= 0)
            {
                return 0;
            }

            if (p - e + a >= wwmm)
            {
                r = p - e - (wm - w);
            }
            else
            {
                r = p - e - ((wm - w) - wm * (Math.Pow(1 - (p - e + a) / wwmm, 1 + b)));
            }

            return r;
        }

        /// <summary>
        /// 三水源划分
        /// </summary>
        /// <param name="p">降水量</param>
        /// <param name="e">蒸发量</param>
        /// <returns></returns>
        private Tuple<double, double, double> DivWaterSor(double p, double e)
        {
            double s = _S, sm = _SM, ex = _EX, kss = _KSS, kg = _KG, fr = _FR;
            double pe = RunoffDunne(p, e);
            double rs, rss, rg;

            if (pe <= 0)
            {
                rs = 0;
                rss = s * kss * fr;
                rg = s * kg * fr;
                _S = (1 - kss - kg) * s;
            }
            else
            {
                double ssm = (1 + ex) * sm;
                double au = ssm * (1 - Math.Pow(1 - s / sm, 1 / (1 + ex)));

                if (pe + au < ssm)
                {
                    rs = (pe - sm + s + sm * (Math.Pow(1 - (pe + au) / ssm, 1 + ex))) * fr;
                    rss = (sm - sm * (Math.Pow(1 - (pe + au) / ssm, 1 + ex))) * kss * fr;
                    rg = (sm - sm * (Math.Pow(1 - (pe + au) / ssm, 1 + ex))) * kg * fr;
                    _S = (1 - kss - kg) * (sm - sm * (Math.Pow(1 - (pe + au) / ssm, 1 + ex)));
                }
                else
                {
                    rs = (pe - sm + s) * fr;
                    rss = sm * kss * fr;
                    rg = sm * kg * fr;
                    _S = (1 - kss - kg) * sm;
                }
            }

            return Tuple.Create(rs, rss, rg);
        }

        /// <summary>
        /// 土壤蓄水量状态变化
        /// </summary>
        /// <param name="p">降水量</param>
        /// <param name="r">产流量</param>
        /// <param name="ei">三层蒸发的Tuple</param>
        /// <returns></returns>
        private Tuple<double, double[]> SoilWaterChg(double p, double r, Tuple<double, double[]> ei)
        {
            double e = ei.Item1;
            double wum = _WUM, wlm = _WLM, wdm = _WDM;
            double wu = _WU, wl = _WL, wd = _WD;
            double pe = p - e;

            if (pe > 0)
            {
                if (pe - r <= wum - wu)
                {
                    _WU = wu + pe - r;
                    _WL = wl;
                    _WD = wd;
                }
                else if ((pe - r) > (wum - wu) && (pe - r) <= (wum - wu) + (wlm - wl))
                {
                    _WU = wum;
                    _WL = wl + (pe - r) - (wum - wu);
                    _WD = wd;
                }
                else if (pe - r > (wum - wu) + (wlm - wl) && pe - r <= (wum - wu) + (wlm - wl) + (wdm - wd))
                {
                    _WU = wum;
                    _WL = wlm;
                    _WD = wd + pe - r - (wum - wu) - (wlm - wl);
                }
                else
                {
                    _WU = wum;
                    _WL = wlm;
                    _WD = wdm;
                }
            }
            else if (pe == 0)
            {
                _WU = wu;
                _WL = wl;
                _WD = wd;
            }
            else
            {
                double ep = -pe;

                if (ep <= wu)
                {
                    _WU = wu - ep;
                    _WL = wl;
                    _WD = wd;
                }
                else if (ep >= wu && ep < (wl + wu))
                {
                    _WU = 0;
                    _WL = wl - (ep - wu);
                    _WD = wd;
                }
                else if (ep >= wu + wl && ep < (wl + wu + wd))
                {
                    _WU = 0;
                    _WL = 0;
                    _WD = wd - (ep - wu - wl);
                }
                else
                {
                    _WU = 0;
                    _WL = 0;
                    _WD = 0;
                }
            }

            _W = _WU + _WL + _WD;
            _FR = 1 - Math.Pow((1 - _W / _WM), (_B / (1 + _B)));
            return Tuple.Create(_W, new double[] { _WU, _WL, _WD });
        }

        #endregion

        #region 汇流部分
        /// <summary>
        /// 地表汇流
        /// </summary>
        /// <param name="arrRs">三水源划分后的地表水</param>
        /// <returns></returns>
        private List<double> SurfaceFlow(List<double> arrRs)
        {
            double f = _F, dt = _DT;
            double[] uh = _UH;
            List<double> arrQS = new List<double>();
            List<double> QT = new List<double>();

            foreach (var u in uh)
            {
                QT.Add(u * 10 * f / (3.6 * dt));
            }

            int i = 0;

            while (true)
            {
                double QS = 0;

                for (int j = 0; j < arrRs.Count; j++)
                {
                    if (i - j >= 0 && i - j < QT.Count)
                    {
                        QS += (arrRs[j] / 10) * QT[i - j];
                    }
                }

                arrQS.Add(QS);
                i++;

                if (i - arrRs.Count == QT.Count)
                {
                    break;
                }
            }

            return arrQS;
        }

        /// <summary>
        /// 壤中流、地下水汇流
        /// </summary>
        /// <param name="arrRSS">三水源划分后的壤中流</param>
        /// <param name="arrRG">三水源划分后的地下水</param>
        /// <returns></returns>
        private Tuple<List<double>, List<double>> RSSG2Q(List<double> arrRSS, List<double> arrRG)
        {
            double F = _F;
            double DT = _DT;
            double KKSS = _KKSS;
            double KKG = _KKG;
            List<double> arrQSS = new List<double>();
            List<double> arrQG = new List<double>();

            arrQSS.Add(_QSS0);
            arrQG.Add(_QG0);

            for (int i = 0; i < arrRSS.Count; i++)
            {
                double QSS0 = arrQSS[arrQSS.Count - 1];
                double QG0 = arrQG[arrQG.Count - 1];
                double QSS1 = QSS0 * Math.Pow(KKSS, 1.0 / DT) + arrRSS[i] * (1 - Math.Pow(KKSS, 1.0 / DT)) * (F / (3.6 * DT));
                double QG1 = QG0 * Math.Pow(KKG, 1.0 / DT) + arrRG[i] * (1 - Math.Pow(KKG, 1.0 / DT)) * (F / (3.6 * DT));
                arrQSS.Add(QSS1);
                arrQG.Add(QG1);
            }

            arrQSS.Add(arrQSS[arrQSS.Count - 1] * Math.Pow(KKSS, 1.0 / DT));
            arrQG.Add(arrQG[arrQG.Count - 1] * Math.Pow(KKG, 1.0 / DT));

            return Tuple.Create(arrQSS, arrQG);
        }
        #endregion

        #region 模型计算
        private Tuple<double, Tuple<double, double, double>, Tuple<double, double[]>, Tuple<double, double[]>> RunRProduce(double P, double E0)
        {
            var Ei = Evapor3Lyrs(P, E0);
            double R = RunoffDunne(P, Ei.Item1);
            var Ri = DivWaterSor(P, Ei.Item1);
            var Wi = SoilWaterChg(P, R, Ei);
            return Tuple.Create(R, Ri, Ei, Wi);
        }

        private Tuple<List<double>, List<double>, List<double>> RunConverge(List<double> arrRS, List<double> arrRSS, List<double> arrRG)
        {
            var arrQS = SurfaceFlow(arrRS);
            var qResult = RSSG2Q(arrRSS, arrRG);
            return Tuple.Create(arrQS, qResult.Item1, qResult.Item2);
        }

        public Tuple<List<double>, List<double>, List<double>> RunModel(List<double> arrP, List<double> arrE0)
        {
            var arrE = new List<double>();
            var arrR = new List<double>();
            var arrRi = new List<Tuple<double, double, double>>();
            var arrW = new List<double>();

            for (int i = 0; i < arrP.Count; i++)
            {
                double P = arrP[i];
                double E0 = arrE0[i];
                var result = RunRProduce(P, E0);
                arrR.Add(result.Item1);
                arrRi.Add(result.Item2);
                arrE.Add(result.Item3.Item1);
                arrW.Add(result.Item4.Item1);
            }

            var arrRS = arrRi.Select(t => t.Item1).ToList();
            var arrRSS = arrRi.Select(t => t.Item2).ToList();
            var arrRG = arrRi.Select(t => t.Item3).ToList();

            var convergeResult = RunConverge(arrRS, arrRSS, arrRG);
            return Tuple.Create(convergeResult.Item1, convergeResult.Item2, convergeResult.Item3);
        }
        #endregion

    }
}
