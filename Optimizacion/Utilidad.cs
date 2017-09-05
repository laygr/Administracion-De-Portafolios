using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class Utilidad
    {
        double Lambda { get; }
        double T { get; }
        double [,] Omega { get; }
        double[] ExpectedReturns { get; }
        
        public Utilidad(double lambda, double t, double[,]omega, double[] expectedReturns) {
            Lambda = lambda;
            T = t;
            Omega = omega;
            ExpectedReturns = expectedReturns;
        }
        void func(double[] xs, double[] fi, object obj)
        {
            double[,] xsMatrix = MatrixOp.matrixFrowRow(xs);

            double ret = VectorOp.sumproduct(xs, ExpectedReturns) * T;
            double stdDev =
                Math.Sqrt(
                    T *
                    MatrixOp.mmult(MatrixOp.mmult(xsMatrix, Omega), MatrixOp.transpose(xsMatrix))[0, 0]
                );


            fi[0] = -(ret - Math.Pow(stdDev, 2) * this.Lambda);
            fi[1] = Enumerable.Sum(xs) - 1;
        }
        public Result Opt(double[] initialValues)
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

            alglib.minnsoptimize(state, func, null, null);
            alglib.minnsresults(state, out x1, out rep);
            Console.WriteLine("{0}", alglib.ap.format(x1, 3));
            Console.WriteLine("Value: {0}", state.fi[0]);
            return new Result(x1, state.fi[0]);
        }
    }
}
