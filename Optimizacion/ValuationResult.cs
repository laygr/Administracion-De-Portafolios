using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class ValuationResult
    {
        public double[] Weights { get; set; }
        public double ExpectedReturn { get; set; }
        public double StdDev { get; set; }
        public double TransactionCost { get; set; }
        public double Error { get; set; }
        public double Utility { get; set; }

        public void SetEmptyPortfolio(int n)
        {
            Weights = new double[n];
        }
        public ValuationResult(int n)
        {
            Weights = new double[n];
        }
        public ValuationResult() { }
    }
}
