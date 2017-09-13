using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public enum OptimizationKinds { TargetReturn, TargetPortfolio, MaximizeUtility }
    public static class Optimization {
        
        public static ValuationResult ValuePortfolio (StateForUtilityMaximization state, double[] weights)
         {
             double[,] xsMatrix = MatrixOp.matrixFrowRow(weights);

             double expectedReturn = VectorOp.sumproduct(weights, state.ExpectedReturns) * state.Years;
             double stdDev =
                 Math.Sqrt(
                     state.Years *
                     MatrixOp.mmult(MatrixOp.mmult(xsMatrix, state.Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                 );

             double transactionCost = TransactionCosts.Cost(state.PreviousPortfolio, weights, state.TransactionCost);
             double futureTransactionCost = transactionCost * Math.Exp(state.Years * state.RiskFree);
             double netExpectedReturn = expectedReturn - futureTransactionCost;
             double utility = netExpectedReturn - Math.Pow(stdDev, 2) * state.Lambda;

             return new ValuationResult
             {
                 Weights = weights,
                 ExpectedReturn = netExpectedReturn,
                 StdDev = stdDev,
                 TransactionCost = transactionCost,
                 Utility = utility
             };
         }

        public static ValuationResult ValuePortfolio(StateForReturnTargeting state, double[] weights)
        {
            double[,] xsMatrix = MatrixOp.matrixFrowRow(weights);

            double expectedReturn = VectorOp.sumproduct(weights, state.ExpectedReturns) * state.Years;
            double stdDev =
                Math.Sqrt(
                    state.Years *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, state.Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );

            double transactionCost = TransactionCosts.Cost(state.PreviousPortfolio, weights, state.TransactionCost);
            double futureTransactionCost = transactionCost * Math.Exp(state.Years * state.RiskFree);
            double netExpectedReturn = expectedReturn - futureTransactionCost;
            double error = netExpectedReturn - state.TargetReturn;

            return new ValuationResult
            {
                Weights = weights,
                ExpectedReturn = netExpectedReturn,
                StdDev = stdDev,
                TransactionCost = transactionCost,
                Error = error
            };
        }
        public static ValuationResult ValuePortfolio(StateForPortfolioTargeting state, double[] weights)
        {
            double[,] xsMatrix = MatrixOp.matrixFrowRow(weights);

            double expectedReturn = VectorOp.sumproduct(weights, state.ExpectedReturns) * state.Years;
            double stdDev =
                Math.Sqrt(
                    state.Years *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, state.Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );

            double transactionCost = TransactionCosts.Cost(state.PreviousPortfolio, weights, state.TransactionCost);
            double futureTransactionCost = transactionCost * Math.Exp(state.Years * state.RiskFree);
            double netExpectedReturn = expectedReturn - futureTransactionCost;
            double error = VectorOp.difference(weights, state.TargetPortfolio).Sum();

            return new ValuationResult
            {
                Weights = weights,
                ExpectedReturn = netExpectedReturn,
                StdDev = stdDev,
                TransactionCost = transactionCost,
                Error = error
            };
        }
        public static ValuationResult ValuePortfolio(State state, double[] weights)
        {
            if (state.GetType() == typeof(StateForPortfolioTargeting))
            {
                return ValuePortfolio((StateForPortfolioTargeting)state, weights);
            }
            else if (state.GetType() == typeof(StateForReturnTargeting))
            {
                return ValuePortfolio((StateForReturnTargeting)state, weights);
            }
            else if (state.GetType() == typeof(StateForUtilityMaximization))
            {
                return ValuePortfolio((StateForUtilityMaximization)state, weights);
            }
            else
            {
                return null;
            }
        }
        
        static Func<StateForReturnTargeting, alglib.ndimensional_fvec> target_return_func = (state) =>
         {

             alglib.ndimensional_fvec f = (xs, fi, obj) =>
             {
                 var valuation = ValuePortfolio(state, xs);
                 fi[0] = valuation.StdDev;
                 fi[1] = Enumerable.Sum(xs) - 1;
                 fi[2] = valuation.Error;
             };
             return f;
 
         };
        static Func<StateForUtilityMaximization, alglib.ndimensional_fvec> maximize_utility_func = (state) =>
        {
            alglib.ndimensional_fvec d = (xs, fi, obj) =>
            {
                var valuation = ValuePortfolio(state, xs);
                fi[0] = -valuation.Utility;
                fi[1] = Enumerable.Sum(xs) - 1;
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
        
        public static ValuationResult Optimize(State state, double[] initialValues)
        {
            alglib.ndimensional_fvec functionToOptimize = null;
            int n = initialValues.Length;
            int equalities = 0;
            int inequalities = 0;
            var stateType = state.GetType();

            if(stateType == typeof(StateForReturnTargeting))
            {
                functionToOptimize = target_return_func((StateForReturnTargeting)state);
                equalities = 2;
                inequalities = 0;
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
                s[i] = 1;
            }
            double epsx = 0.00000001;
            double diffstep = 0.00000000001;
            double radius = 0.1;
            double rho = 0.1;
            int maxits = 0;
            alglib.minnsstate optimizationState;
            alglib.minnsreport rep;
            double[] x1;

            alglib.minnscreatef(n, initialValues, diffstep, out optimizationState);
            alglib.minnssetalgoags(optimizationState, radius, rho);
            alglib.minnssetcond(optimizationState, epsx, maxits);
            alglib.minnssetscale(optimizationState, s);
            alglib.minnssetnlc(optimizationState, equalities, inequalities);
            alglib.minnsoptimize(optimizationState, functionToOptimize, null, null);
            alglib.minnsresults(optimizationState, out x1, out rep);
            Console.WriteLine("{0}", alglib.ap.format(x1, 3));
            Console.WriteLine("Value: {0}", optimizationState.fi[0]);
            var valuation = ValuePortfolio(state, x1);
            Console.WriteLine("Expected return: {0}", valuation.ExpectedReturn);
            Console.WriteLine("StdDev: {0}", valuation.StdDev);
            Console.WriteLine("Transaction cost: {0}", valuation.TransactionCost);
            return valuation;
        }
    }
}
