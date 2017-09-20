using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class RebalancingCosts
    {
        public double SharesBuySell { get; set; }
        public double CommissionCosts { get; set; }
        public double TotalRebalancingCost { get; set; }
        public double SpreadCost { get { return TotalRebalancingCost - CommissionCosts; } }
        public double TotalRebalancingCostInPercentage(State state)
        {
            return TotalRebalancingCost / state.PortfolioTotalValue();
        }
    }

    public class RebalancingValuation
    {
        public static double NetExpectedReturn(State state, double expectedReturn, RebalancingCosts rebalancingCosts)
        {
            double transactionCost = rebalancingCosts.TotalRebalancingCostInPercentage(state);
            if (transactionCost >= 0.01)
            {
                //Console.WriteLine("transaction cost: {0}", transactionCost);
            }
            return expectedReturn - transactionCost - (expectedReturn * transactionCost);
        }
        public static double ExpectedReturn(State state, double[] newStocksAllocation, RebalancingCosts rebalancingCosts)
        {
            var oldPortfolioValue = state.PortfolioTotalValue();
            var stocksValue = VectorOp.DotProduct(newStocksAllocation, state.AvgPrices);
            var newValue = VectorOp.DotProduct(stocksValue, VectorOp.Addition(state.ExpectedReturns, 1.0)).Sum();
            var expectedReturn = ((newValue + rebalancingCosts.SharesBuySell) / oldPortfolioValue - 1)* state.Years;
            //return NetExpectedReturn(state, expectedReturn, rebalancingCosts);
            return expectedReturn;
        }
        public static ValuationResult ValuePortfolio(State state, double[] newStocksAllocation)
        {
            var valueAllocation = VectorOp.DotProduct(newStocksAllocation, state.AvgPrices);
            var weights = VectorOp.normalize(valueAllocation);
            double[,] xsMatrix = MatrixOp.matrixFromRow(weights);

            RebalancingCosts rebalancingCost = CostForNewStocksAllocation(state, newStocksAllocation);
            double netExpectedReturn = ExpectedReturn(state, newStocksAllocation, rebalancingCost);
            double stdDev =
                Math.Sqrt(
                    state.Years *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, state.Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );
            

            return new ValuationResult
            {
                StocksAllocation = newStocksAllocation,
                ExpectedReturn = netExpectedReturn,
                StdDev = stdDev,
                RebalancingCost = rebalancingCost,
            };
        }
        static double PriceToUse(double currentStocksAllocation, double newStocksAllocation, double bidPrice, double askPrice)
        {
            return currentStocksAllocation < newStocksAllocation ? askPrice : bidPrice;
        }
        static double[] PricesToUse(double[] previousPortfolio, double[] newPortfolio, double[] bidPrices, double[] askPrices)
        {
            int n = bidPrices.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = PriceToUse(previousPortfolio[i], newPortfolio[i], bidPrices[i], askPrices[i]);
            }
            return result;
        }
        static RebalancingCosts CostForNewStocksAllocation(State state, double[] newStocksAllocation)
        {
            double sharesBuySellAcum = 0;
            double commissionCostAcum = 0;
            int n = state.PreviousPortfolio.Length;
            var pricesToUse = PricesToUse(state.PreviousPortfolio, newStocksAllocation, state.BidPrices, state.AskPrices);
            for (int i = 0; i < n; i++)
            {
                double shareBuySell = (state.PreviousPortfolio[i] - newStocksAllocation[i]) * pricesToUse[i];
                double commision = i + 1 == n ? 0 : state.TransactionCost;
                double commissionCost = Math.Abs(shareBuySell) * commision;

                sharesBuySellAcum += shareBuySell - commissionCost;
                commissionCostAcum += commissionCost;

            }
            var newStocksAllocationValue = VectorOp.sumproduct(newStocksAllocation, state.AvgPrices);
            return new RebalancingCosts
            {
                TotalRebalancingCost = state.PortfolioTotalValue() - (newStocksAllocationValue + sharesBuySellAcum),
                CommissionCosts = commissionCostAcum,
                SharesBuySell = sharesBuySellAcum
            };
        }
    }
}
