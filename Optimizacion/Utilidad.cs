using System;
using System.Linq;

namespace Optimizacion
{
    public class UtilityResult
    {
        public double[] Weights { get; }
        public double ExpectedReturn { get; }
        public double StdDev { get; }
        public double TransactionCost { get; }
        public double Utility { get; }

        public UtilityResult(double[] weights, double expectedReturn, double stdDev, double transactionCost, double utility)
        {
            Weights = weights;
            ExpectedReturn = expectedReturn;
            StdDev = stdDev;
            TransactionCost = transactionCost;
            Utility = utility;
        }
        public UtilityResult(int n)
        {
            Weights = new double[n];
            ExpectedReturn = 0;
            StdDev = 0;
            TransactionCost = 0;
            Utility = 0;
        }
    }

    class State2
    {
        public double Lambda { get; }
        public double T { get; }
        public double[,] Omega { get; }
        public double[] ExpectedReturns { get; }
        public double[] PreviousPortfolio { get; }
        public double TransactionCost { get; }
        public double RiskFree { get; }
    }

    public class State
    {
        double Lambda { get; }
        double T { get; }
        double[,] Omega { get; }
        double[] ExpectedReturns { get; }
        double[] PreviousPortfolio { get; }
        double TransactionCost { get; }
        double RiskFree { get; }

        public State(double lambda, double t, double[,]omega, double[] expectedReturns, double[] previousPortfolio, double transactionCost, double riskFree) {
            Lambda = lambda;
            T = t;
            Omega = omega;
            ExpectedReturns = expectedReturns;
            PreviousPortfolio = previousPortfolio;
            TransactionCost = transactionCost;
            RiskFree = riskFree;
        }
        public UtilityResult Utility(double[] weights)
        {
            double[,] xsMatrix = MatrixOp.matrixFrowRow(weights);

            double expectedReturn = VectorOp.sumproduct(weights, ExpectedReturns) * T;
            double stdDev =
                Math.Sqrt(
                    T *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );

            double transactionCost = TransactionCosts.Cost(PreviousPortfolio, weights, TransactionCost);
            double futureTransactionCost = transactionCost * Math.Exp(RiskFree);
            double netExpectedReturn = expectedReturn - futureTransactionCost;
            double utility = netExpectedReturn - Math.Pow(stdDev, 2) * Lambda;

            return new UtilityResult(weights, netExpectedReturn, stdDev, transactionCost, utility);
        }
        void utility_func(double[] xs, double[] fi, object obj)
        {
            var utilityResult = Utility(xs);
            fi[0] = - utilityResult.Utility;
            fi[1] = Enumerable.Sum(xs) - 1;
        }
        public UtilityResult OptimizeForUtility(double[] initialValues)
        {
            int n = initialValues.Length;

            double[] s = new double[n];
            for(int i = 0; i < n; i++)
            {
                s[i] = 1;
            }
            double epsx = 0.00000001;
            double diffstep = 0.00000000001;
            double radius = 0.1;
            double rho = 0.1;
            int maxits = 0;
            alglib.minnsstate state;
            alglib.minnsreport rep;
            double[] x1;

            alglib.minnscreatef(n, initialValues, diffstep, out state);
            alglib.minnssetalgoags(state, radius, rho);
            alglib.minnssetcond(state, epsx, maxits);
            alglib.minnssetscale(state, s);
            alglib.minnssetnlc(state, 1, 0);

            alglib.minnsoptimize(state, utility_func, null, null);
            alglib.minnsresults(state, out x1, out rep);
            Console.WriteLine("{0}", alglib.ap.format(x1, 3));
            Console.WriteLine("Value: {0}", state.fi[0]);
            return Utility(x1);
        }
    }
}
