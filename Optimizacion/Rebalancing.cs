using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    /*
    class RebalancingState
    {
        public State State { get; set; }
        public double[] NeededStocksProportions { get; set; }
        public double[] DesiredWeights { get; set; }
    }
    
    public class RebalancingResult
    {
        public double[] NewStocksAllocation { get; set; }
        public double[] NewWeights { get; set; }
        public double LeftoverCash { get; set; }
        public RebalancingCosts RebalancingCosts { get; set; }
    }
    public class Rebalancing
    {
        public static ValuationResult ValuePortfolio(StateForReturnTargeting state, double[] newStocksAllocation)
        {
            double[,] xsMatrix = MatrixOp.matrixFromRow(weights);

            double expectedReturn = VectorOp.sumproduct(weights, state.ExpectedReturns) * state.Years;
            double stdDev =
                Math.Sqrt(
                    state.Years *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, state.Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );

            RebalancingResult rebalancingResult = Rebalancing.Rebalance(state, weights);
            double netExpectedReturn = NetExpectedReturn(state, expectedReturn, rebalancingResult);
            double error = netExpectedReturn - state.TargetReturn;

            return new ValuationResult
            {
                ExpectedReturn = netExpectedReturn,
                StdDev = stdDev,
                RebalancingResult = rebalancingResult,
                Error = error
            };
        }

        public static RebalancingResult Rebalance(State state, double[] desiredWeights)
        {
            RebalancingState rState = new RebalancingState
            {
                State = state,
                DesiredWeights = desiredWeights,
            };
            if(state.ShouldRebalance == false)
            {
                return ReportResult(rState, state.PreviousPortfolio);
            }
            return Rebalance(rState);
        }
        static double PriceToUse(double currentStocksAllocation, double newStocksAllocation, double bidPrice, double askPrice)
        {
            return currentStocksAllocation < newStocksAllocation ? askPrice : bidPrice;
        }
        
        static double[] PricesToUseWithEstimateOfChange(double[] previousPortfolio, double[] newPortfolio, double estimateChange, double[]bidPrices, double[] askPrices)
        {
            int n = bidPrices.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = PriceToUse(previousPortfolio[i], newPortfolio[i] * 1 + estimateChange, bidPrices[i], askPrices[i]);
            }
            return result;
        }
        
        static double[] WeightsForNewStocksAllocation(RebalancingState state, double[] newStocksAllocation)
        {
            return VectorOp.normalize(VectorOp.DotProduct(state.State.AvgPrices, newStocksAllocation));
        }
        static double LeftoverCash(RebalancingState state, double[] newStocksAllocation)
        {
            return state.State.AvailableCash + CostForNewStocksAllocation(state, newStocksAllocation).SharesBuySell;
        }
        static double LimitAbs(double a, double b)
        {
            return Math.Abs(a) < Math.Abs(b)? a: SignOf(a, b);
        }
        static double SignOf(double valueWithSign, double targetValue)
        {
            if (valueWithSign < 0)
            {
                if(targetValue < 0) { return targetValue; } else { return -targetValue; }
            }
            else
            {
                if(targetValue < 0) { return -targetValue; }
            }
            return targetValue * valueWithSign / Math.Abs(valueWithSign);
        }
        static double ScaleLimit(double iter)
        {
            var limit = 1.0 - 1.0 / 400.0 * iter;
            return limit == 1 ? 0.999 : limit;
        }
        static double[] ScaleAllocation(RebalancingState state, double[] stocksAllocation, double totalCashToBeLeftover, int iter)
        {
            int n = stocksAllocation.Length;
            var currentlyLeftoverCash = LeftoverCash(state, stocksAllocation);
            var cashToBeLeftover = totalCashToBeLeftover - currentlyLeftoverCash;
            var scaleLimitor = ScaleLimit(iter);

            var portfolioValue = VectorOp.sumproduct(state.State.AvgPrices, stocksAllocation);
            var pricesToUse = PricesToUseWithEstimateOfChange(
                state.State.PreviousPortfolio,
                stocksAllocation,
                1.0-cashToBeLeftover / (2.0*portfolioValue),
                state.State.BidPrices, state.State.AskPrices);
            
            var portfolioValueInChangePrices = VectorOp.sumproduct(pricesToUse, stocksAllocation);

            var targetPortfolioValue = portfolioValueInChangePrices - cashToBeLeftover / 2.0;
            var percentualChange = targetPortfolioValue / portfolioValueInChangePrices - 1.0;


            //var percentualChangeNeeded = LimitAbs(-cashToBeLeftover / portfolioValueInChangePrices, scaleLimitor);

            var newStocksAllocation = VectorOp.multiplication(stocksAllocation, 1 + percentualChange);
            var newPV = VectorOp.sumproduct(state.State.AvgPrices, newStocksAllocation);
            var newCashLeftOver = LeftoverCash(state, newStocksAllocation);
            var cashToBeLeftOverAfterScalling = totalCashToBeLeftover - newCashLeftOver;
            if(cashToBeLeftOverAfterScalling / cashToBeLeftover > 1.0)
            {
                Console.WriteLine("why :(");
            }
            if (Double.IsNaN(newStocksAllocation[0]))
            {
                Console.WriteLine("why :(");
            }
            return newStocksAllocation;
        }

        static RebalancingResult Rebalance(RebalancingState state)
        {
            var portfolioValue = state.State.PortfolioTotalValue();
            var desiredWeightsSum = state.DesiredWeights.Sum();
            var cashToBeLeftover = (1 - desiredWeightsSum) * portfolioValue;
            var valueAllocation = VectorOp.multiplication(state.DesiredWeights, portfolioValue);
            var newStocksAllocation = VectorOp.DotDivision(valueAllocation, state.State.AvgPrices);

            bool done = false;
            double remainingCashToBeLeftover = 0;
            for(int i = 1; i <= 400 && !done; i ++)
            {
                newStocksAllocation = ScaleAllocation(state, newStocksAllocation, cashToBeLeftover, i);
                remainingCashToBeLeftover = LeftoverCash(state, newStocksAllocation);
                done = Math.Abs(cashToBeLeftover - remainingCashToBeLeftover) < 100;
            }
            if (!done)
            {
                Console.WriteLine("Wrong by: {0}", cashToBeLeftover - remainingCashToBeLeftover);
            }
            var result = ReportResult(state, newStocksAllocation);
            return result;
        }

        static RebalancingResult ReportResult(RebalancingState state, double[] newStocksAllocation)
        {
            var rebalancingCost = CostForNewStocksAllocation(state, newStocksAllocation);
            var weightsForNewStocksAllocation = WeightsForNewStocksAllocation(state, newStocksAllocation);
            weightsForNewStocksAllocation = VectorOp.normalize(weightsForNewStocksAllocation, state.DesiredWeights.Sum());
            var rebalancingResult = new RebalancingResult
            {
                NewStocksAllocation = newStocksAllocation,
                RebalancingCosts = rebalancingCost,
                LeftoverCash = LeftoverCash(state, newStocksAllocation),
                NewWeights = weightsForNewStocksAllocation
            };
            return rebalancingResult;
        }

        static int counter = 0;
        static Func<RebalancingState, alglib.ndimensional_fvec> diff_func = (state) =>
        {
            alglib.ndimensional_fvec f = (xs, fi, obj) =>
            {
                counter++;
                int n = xs.Length;
                double[] newStocksAllocation = xs;
                var leftoverCash = LeftoverCash(state, newStocksAllocation);
                var weightsForNewStocksAllocation = WeightsForNewStocksAllocation(state, newStocksAllocation);
                var standarizedDesiredWeights = VectorOp.normalize(state.DesiredWeights);
                fi[0] = VectorOp.SquareError(weightsForNewStocksAllocation, standarizedDesiredWeights);
                fi[1] = Math.Pow(leftoverCash, 2);
                /*
                for(int i = 0; i < n - 1; i++)
                {
                    fi[i + 1] = weightsForNewStocksAllocation[i] - state.DesiredWeights[i];
                }
               
            };
            return f;
        };
    
        static RebalancingResult Optimize(RebalancingState state)
        {
            counter = 0;
            int n = state.State.PreviousPortfolio.Length;
            double[] initialValue = new double[n];// state.State.PreviousPortfolio;
            int equalities = 1;//n;
            int inequalities = 0;
            var stateType = state.GetType();

            double[] s = new double[n];
            for (int i = 0; i < n; i++)
            {
                s[i] = 1;
            }
            double epsx = 0.0001;
            double diffstep = 1;
            double radius = 0.1;
            double rho = .01;
            int maxits = 0;
            alglib.minnsstate optimizationState;
            alglib.minnsreport rep;
            double[] x1;

            alglib.minnscreatef(n, initialValue, diffstep, out optimizationState);
            alglib.minnssetalgoags(optimizationState, radius, rho);
            alglib.minnssetcond(optimizationState, epsx, maxits);
            alglib.minnssetscale(optimizationState, s);
            alglib.minnssetnlc(optimizationState, equalities, inequalities);
            alglib.minnsoptimize(optimizationState, diff_func(state), null, null);
            alglib.minnsresults(optimizationState, out x1, out rep);
            var newStocksAllocation = x1;
            var rebalancingCost = CostForNewStocksAllocation(state, newStocksAllocation);
            var rebalancingResult = new RebalancingResult
            {
                NewStocksAllocation = newStocksAllocation,
                RebalancingCosts = rebalancingCost,
                LeftoverCash = LeftoverCash(state, newStocksAllocation),
                NewWeights = WeightsForNewStocksAllocation(state, newStocksAllocation)
            };
            if(VectorOp.SquareError(state.DesiredWeights, rebalancingResult.NewWeights) > 0.1)
            {
                Console.WriteLine("What the fuck");
            }
            return rebalancingResult;
        }
*/
        /*

        static double[] StocksAllocationsFor(RebalancingState state, double factor)
        {
            int n = state.NeededStocksProportions.Length;
            double[] newStocksAllocations = new double[n];
            for (int i = 0; i < n; i++)
            {
                newStocksAllocations[i] = state.NeededStocksProportions[i] * factor;
            }
            return newStocksAllocations;
        }

        static Func<RebalancingState, alglib.ndimensional_fvec> diff_func = (state) =>
        {
            alglib.ndimensional_fvec f = (xs, fi, obj) =>
            {
                double[] newStocksAllocations = StocksAllocationsFor(state, xs[0]);
                var leftoverCash = LeftoverCash(state, newStocksAllocations);
                fi[0] = Math.Pow(leftoverCash ,2);
            };
            return f;
        };
        static RebalancingResult Optimize(RebalancingState state)
        {
            int n = 1;
            double[] initialValue = new double[] { 0.0 };
            int equalities = 0;
            int inequalities = 0;
            var stateType = state.GetType();
            
            double[] s = new double[n];
            for (int i = 0; i < n; i++)
            {
                s[i] = 1;
            }
            double epsx = 0.00000001;
            double diffstep = 0.000000001;
            double radius = 0.1;
            double rho = 0.8;
            int maxits = 0;
            alglib.minnsstate optimizationState;
            alglib.minnsreport rep;
            double[] x1;

            alglib.minnscreatef(n, initialValue, diffstep, out optimizationState);
            alglib.minnssetalgoags(optimizationState, radius, rho);
            alglib.minnssetcond(optimizationState, epsx, maxits);
            alglib.minnssetscale(optimizationState, s);
            alglib.minnssetnlc(optimizationState, equalities, inequalities);
            alglib.minnsoptimize(optimizationState, diff_func(state), null, null);
            alglib.minnsresults(optimizationState, out x1, out rep);
            var newStocksAllocation = StocksAllocationsFor(state, x1[0]);
            var rebalancingCost = CostForNewStocksAllocation(state, newStocksAllocation);
            return new RebalancingResult
            {
                NewStocksAllocation = newStocksAllocation,
                RebalancingCosts = rebalancingCost,
                LeftoverCash = LeftoverCash(state, newStocksAllocation)
            };
        }
        */
        /*
    }
        */

}
