using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class ValuationResult
    {
        public double[] StocksAllocation { get; set; }
        public RebalancingCosts RebalancingCost { get; set; }
        public double ExpectedReturn { get; set; }
        public double StdDev { get; set; }
        
        public double SharpeRatio(double riskFreeRate)
        {
            if (ExpectedReturn == 0 || ExpectedReturn - riskFreeRate == 0) return 0;
            return (ExpectedReturn-riskFreeRate) / StdDev;
        }
    }
}
