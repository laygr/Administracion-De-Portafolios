﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimizacion
{
    public class State
    {
        public double[,] Omega { get; set; }
        public double[] ExpectedReturns { get; set; }
        public double[] PreviousPortfolio { get; set; }
        public double TransactionCost { get; set; }
        public double Years { get; set; }
        public double RiskFree { get; set; }
        public double[] BidPrices { get; set; }
        public double[] AskPrices { get; set; }
        public double[] AvgPrices { get; set; }
        public double AvailableCash { get; set; }
        public bool ShouldRebalance { get; set; }
        public double PortfolioTotalValue()
        {
            return VectorOp.sumproduct(PreviousPortfolio, AvgPrices);
        }
    }
    public class StateForUtilityMaximization : State
    {
        public double Lambda { get; set; }
    }
    public class StateForReturnTargeting : State
    {
        public double TargetReturn { get; set; }
    }
    public class StateForPortfolioTargeting : State {
        public double[] TargetPortfolio { get; set; }
    }
}
