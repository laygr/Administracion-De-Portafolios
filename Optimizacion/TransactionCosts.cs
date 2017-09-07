using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class TransactionCosts
    {
        public static double Cost(double[]previousPortfolio, double[] newPortfolio, double cost)
        {
            return VectorOp.difference(newPortfolio, previousPortfolio).Sum() * cost;
        }
    }
}
