using System;
using System.Linq;

namespace Optimizacion
{
    public enum OptimizationKinds { TargetReturn, TargetPortfolio, MaximizeUtility }
    public abstract class OptimizationParameters {
        public MarketData MarketData { get; set; }
        public double Cashout { get; set; }
        public double[] CurrentStocksAllocation { get; set; }
        public double StocksProportion { get; set; }
        public double IPCProportion { get; set; }
        public bool HoldProportions { get; set; }
        public double StocksValue()
        {
            return MarketData.StocksValue(CurrentStocksAllocation);
        }
    }
    public class ReturnTargetingParameters : OptimizationParameters
    {
        public double TargetReturn { get; set; }
    }
    public static class Optimization {
        
        static Func<ReturnTargetingParameters, alglib.ndimensional_fvec> target_return_func = (state) =>
         {

             alglib.ndimensional_fvec f = (newStocksAllocation, fi, obj) =>
             {
                 var valuation = RebalancingValuation.ValuePortfolio(state.MarketData, state.CurrentStocksAllocation, newStocksAllocation);
                 var error = state.TargetReturn - valuation.ExpectedReturn;
                 //fi[0] = -valuation.ExpectedReturn / valuation.StdDev;
                 fi[0] = valuation.StdDev;
                 fi[1] = Math.Pow(error,2);
                 // Cashout:
                 fi[2] = valuation.RebalancingCost.SharesBuySell; //fi[2] = Enumerable.Sum(xs) -1;
                 var totalValue = state.MarketData.StocksValue(newStocksAllocation);
                 
                 if (state.HoldProportions)
                 {
                     int n = newStocksAllocation.Length;
                     double[] stocksAlloc = ((double[])newStocksAllocation.Clone());

                     stocksAlloc[n - 2] = 0;
                     stocksAlloc[n - 1] = 0;
                     double[] ipc = new double[n];
                     ipc[n - 2] = newStocksAllocation[n - 2];
                     fi[3] = (state.MarketData.StocksValue(stocksAlloc) / totalValue - state.StocksProportion);//stocks;
                     fi[4] = (state.MarketData.StocksValue(ipc) / totalValue - state.IPCProportion);//ipc;
                     fi[5] = Math.Abs(fi[3]) + Math.Abs(fi[4]);
                 }
                 else
                 {
                     fi[3] = 0;
                     fi[4] = 0;
                     fi[5] = 0;
                 }

                 // No short sales
                 for(int i = 0; i < state.CurrentStocksAllocation.Length; i++)
                 {
                     fi[6 + i] = (-newStocksAllocation[i]) / 100;
                 }
             };
             return f;
         };
        /*
        static Func<StateForUtilityMaximization, alglib.ndimensional_fvec> maximize_utility_func = (state) =>
        {
            alglib.ndimensional_fvec d = (xs, fi, obj) =>
            {
                var valuation = ValuePortfolio(state, xs);
                fi[0] = -valuation.Utility;
                fi[1] = Enumerable.Sum(xs) - 1;
                *
            };
            return d;
        };
        
        static Func<StateForPortfolioTargeting, alglib.ndimensional_fvec> target_portfolio_func = (state) =>
        {
            alglib.ndimensional_fvec f = (xs, fi, obj) =>
            {
                var valuation = ValuePortfolio(state, xs);
                fi[0] = valuation.Error;
                fi[1] = Enumerable.Sum(xs) - 1;
            };
            return f;
        };
        */
        public static ValuationResult Optimize(OptimizationParameters state, double[] initialValues2)
        {
            int n = state.CurrentStocksAllocation.Length;
            var weights = VectorOp.createWith(n, 1.0 / n);
            double[] initialValues = VectorOp.DotDivision(VectorOp.multiplication(weights, state.StocksValue()), state.MarketData.AvgPrices);
            alglib.ndimensional_fvec functionToOptimize = null;
            int equalities = 0;
            int inequalities = 0;
            var stateType = state.GetType();

            if(stateType == typeof(ReturnTargetingParameters))
            {
                functionToOptimize = target_return_func((ReturnTargetingParameters)state);
                equalities = 5;
                inequalities = initialValues.Length;
            }else if(false)//stateType == typeof(StateForUtilityMaximization))
            {
                //functionToOptimize = target_return_func((StateForReturnTargeting)state);
                //equalities = 1;
                //inequalities = 0;
            } else if(false)//stateType == typeof(StateForPortfolioTargeting))
            {
                //functionToOptimize = target_portfolio_func((StateForPortfolioTargeting)state);
                //equalities = 1;
                //inequalities = 0;
            }

            double[] s = new double[n];
            for (int i = 0; i < n; i++)
            {
                s[i] = 1;// state.AvgPrices.Average() / state.AvgPrices[i];
            }
            double epsx = 0.00001;
            double diffstep = 0.1;
            double radius = 1;
            double rho = 100000;
            int maxits = 0;
            alglib.minnsstate optimizationState;
            alglib.minnsreport rep;
            double[] newStocksAllocation;

            alglib.minnscreatef(n, initialValues, diffstep, out optimizationState);
            alglib.minnssetalgoags(optimizationState, radius, rho);
            alglib.minnssetcond(optimizationState, epsx, maxits);
            alglib.minnssetscale(optimizationState, s);
            alglib.minnssetnlc(optimizationState, equalities, inequalities);
            alglib.minnsoptimize(optimizationState, functionToOptimize, null, null);
            alglib.minnsresults(optimizationState, out newStocksAllocation, out rep);
            var valuation = RebalancingValuation.ValuePortfolio(state.MarketData, state.CurrentStocksAllocation, newStocksAllocation);
            Console.WriteLine("{0}", alglib.ap.format(newStocksAllocation, 3));
            Console.WriteLine("Expected return: {0}", valuation.ExpectedReturn);
            Console.WriteLine("StdDev: {0}", valuation.StdDev);
            Console.WriteLine("buysell: {0}", valuation.RebalancingCost.SharesBuySell);
            //Console.WriteLine("Error: {0}", state.targ);
            return valuation;
        }
    }
}
