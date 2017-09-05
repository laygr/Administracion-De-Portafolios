using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class Result
    {
        public double[] Variables { get; }
        public double Value { get; }

        public Result(double[] variables, double value)
        {
            this.Variables = variables;
            this.Value = value;
        }
    }
}
