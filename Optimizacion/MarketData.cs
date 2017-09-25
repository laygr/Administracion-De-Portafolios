namespace Optimizacion
{
    public class MarketData
    {
        public double[] BidPrices { get; set; }
        public double[] AskPrices { get; set; }
        public double[] AvgPrices { get; set; }
        public double RiskFree
        {
            get
            {
                return ExpectedReturns[ExpectedReturns.Length - 1];
            }
        }
        public double Commission { get; set; }
        public double[] ExpectedReturns { get; set; }
        public double[,] Omega { get; set; }
        public double[] Dividends { get; set; }
        public double StocksValue(double[] stocks)
        {
            return VectorOp.sumproduct(stocks, AvgPrices);
        }
        public double SellingValue(double[] stocks)
        {
            return VectorOp.sumproduct(stocks, BidPrices);
        }
    }
}
