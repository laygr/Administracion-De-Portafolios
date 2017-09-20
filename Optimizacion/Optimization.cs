using System;
using System.Linq;

namespace Optimizacion
{
    public enum OptimizationKinds { TargetReturn, TargetPortfolio, MaximizeUtility }
    public static class Optimization {
        
        static Func<StateForReturnTargeting, alglib.ndimensional_fvec> target_return_func = (state) =>
         {

             alglib.ndimensional_fvec f = (newStocksAllocation, fi, obj) =>
             {
                 var valuation = RebalancingValuation.ValuePortfolio(state, newStocksAllocation);
                 var error = state.TargetReturn - valuation.ExpectedReturn;
                 //fi[0] = -valuation.ExpectedReturn / valuation.StdDev;
                 fi[0] = valuation.StdDev;
                 fi[1] = Math.Pow(error,2) * 100000000000;
                 // no left money:
                 fi[2] = valuation.RebalancingCost.SharesBuySell * 1000; //fi[2] = Enumerable.Sum(xs) -1;

                 // No short sales
                 for(int i = 0; i < state.PreviousPortfolio.Length; i++)
                 {
                     fi[3 + i] = (-newStocksAllocation[i]) * 1000000;
                 }
             };
             return f;
         };
        static Func<StateForUtilityMaximization, alglib.ndimensional_fvec> maximize_utility_func = (state) =>
        {
            alglib.ndimensional_fvec d = (xs, fi, obj) =>
            {
                /*
                var valuation = ValuePortfolio(state, xs);
                fi[0] = -valuation.Utility;
                fi[1] = Enumerable.Sum(xs) - 1;
                */
            };
            return d;
        };
        static Func<StateForPortfolioTargeting, alglib.ndimensional_fvec> target_portfolio_func = (state) =>
        {
            alglib.ndimensional_fvec f = (xs, fi, obj) =>
            {
                /*
                var valuation = ValuePortfolio(state, xs);
                fi[0] = valuation.Error;
                fi[1] = Enumerable.Sum(xs) - 1;
                */
            };
            return f;
        };
        
        public static ValuationResult Optimize(State state, double[] initialValues2)
        {
            int n = state.PreviousPortfolio.Length;
            var weights = VectorOp.createWith(n, 1.0 / n);
            double[] initialValues = VectorOp.DotDivision(VectorOp.multiplication(weights, state.PortfolioTotalValue()), state.AvgPrices);
            alglib.ndimensional_fvec functionToOptimize = null;
            int equalities = 0;
            int inequalities = 0;
            var stateType = state.GetType();

            if(stateType == typeof(StateForReturnTargeting))
            {
                functionToOptimize = target_return_func((StateForReturnTargeting)state);
                equalities = 2;
                inequalities = initialValues.Length;
            }else if(stateType == typeof(StateForUtilityMaximization))
            {
                functionToOptimize = target_return_func((StateForReturnTargeting)state);
                equalities = 1;
                inequalities = 0;
            } else if(stateType == typeof(StateForPortfolioTargeting))
            {
                functionToOptimize = target_portfolio_func((StateForPortfolioTargeting)state);
                equalities = 1;
                inequalities = 0;
            }

            double[] s = new double[n];
            for (int i = 0; i < n; i++)
            {
                s[i] = 1;// state.AvgPrices.Average() / state.AvgPrices[i];
            }
            double epsx = 0.00001;
            double diffstep = 0.1;
            double radius = 1;
            double rho = 5;
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
            var valuation = RebalancingValuation.ValuePortfolio(state, newStocksAllocation);
            Console.WriteLine("{0}", alglib.ap.format(newStocksAllocation, 3));
            Console.WriteLine("Expected return: {0}", valuation.ExpectedReturn);
            Console.WriteLine("StdDev: {0}", valuation.StdDev);
            Console.WriteLine("buysell: {0}", valuation.RebalancingCost.SharesBuySell);
            //Console.WriteLine("Error: {0}", state.targ);
            return valuation;
        }
    }
}
