module BookKeeping
open System
open Deedle
open Optimizacion

(*
let cashoutPercent (marketData:MarketData) percent =
        let stocksValue = this.stocksValue marketData.AvgPrices
        let cashToRemove = (stocksValue + this.DividendsAccount) * percent
        let cashToRemoveFromStocks = max (cashToRemove - this.DividendsAccount) 0.0
        let percentToRemoveFromStocks = cashToRemoveFromStocks / stocksValue
        let stocks =
            Array.map (fun s -> s * (1.0 - percentToRemoveFromStocks) ) this.Stocks
            |> Array.map (fun (s:float) -> Math.Floor s)
        let valuation = RebalancingValuation.ValuePortfolio(marketData, this.Stocks, stocks)
        let cashout =
            marketData.SellingValue(VectorOp.DotSubtraction(this.Stocks, stocks))
            + this.DividendsAccount
            - valuation.RebalancingCost.CommissionCosts
        {
            this with
                Stocks = stocks
                CommissionCost = valuation.RebalancingCost.CommissionCosts + this.CommissionCost
                SpreadCost = valuation.RebalancingCost.SpreadCost + this.SpreadCost
                DividendsAccount = 0.0
        }, cashout
*)
type PortfolioPerformance = {
    Date : DateTime
    StocksValue : float
    DividendsAccount : float
} with
    member this.AddToFrame (frame:Frame<DateTime,string>) =
        let seriesWithValue value =
                Series([this.Date], [value]) :> ISeries<DateTime>
        let columns =
            [ "Stocks & cash"; "Dividends account"
            ]
        let values =
            Seq.map seriesWithValue
                ([
                    string this.StocksValue
                    string this.DividendsAccount
                ])
        Frame.merge frame (Frame(columns, values))
type Portfolio = {
    Date : DateTime
    Stocks : float[]
    DividendsAccount : float
    ExpectedReturn : float
    StdDev : float
    ChangeInExpectedReturn : float
    ChangeInStdDev : float
    ChangeInSharpeRatio : float
    CommissionCost : float
    SpreadCost : float
    CashEarned : float
    CashExpended : float
    Cashout : float
} with
    member this.CashoutMoney (marketData:MarketData) cashToRemove =
        let stocksValue = marketData.SellingValue this.Stocks
        let cashToRemoveFromStocks = max (cashToRemove - this.DividendsAccount) 0.0
        let percentToRemoveFromStocks = cashToRemoveFromStocks / stocksValue
        let stocks =
            Array.map (fun s -> s * (1.0 - percentToRemoveFromStocks) ) this.Stocks
            |> Array.map (fun (s:float) -> Math.Floor s)
        let valuation = RebalancingValuation.ValuePortfolio(marketData, this.Stocks, stocks)
        { this with
            Stocks = stocks
            DividendsAccount = 0.0
            CommissionCost = valuation.RebalancingCost.CommissionCosts + this.CommissionCost
            Cashout = this.DividendsAccount + marketData.SellingValue (VectorOp.DotSubtraction(this.Stocks, stocks))
        }
    member this.SetCashout cashout = { this with Cashout = cashout }
    member this.SharpeRatio riskFreeRate =
        if this.ExpectedReturn = 0.0 || this.StdDev = 0.0
        then 0.0 else (this.ExpectedReturn - riskFreeRate) / this.StdDev
    member this.AddDividends dividends = { this with DividendsAccount = this.DividendsAccount + dividends }
    member this.ClearDividends () = { this with DividendsAccount = 0.0 }
    member this.AddToFrame (frame:Frame<DateTime,string>) assetNames =
            let seriesWithValue value =
                Series([this.Date], [value]) :> ISeries<DateTime>
            let columns =
                [ "DividendsAccount"; "Expected Return"; "Standard Deviation"; "Change In Expected Return";
                    "Change In Std Dev"; "Change In Sharpe Ratio"; "Commission Cost"; "Spread Cost";
                    "Cash Earned"; "Cash Expended"; "Cashout"
                ]
                |> Seq.append assetNames
            let values =
                Seq.map seriesWithValue
                    ([
                        string this.DividendsAccount
                        string this.ExpectedReturn
                        string this.StdDev
                        string this.ChangeInExpectedReturn
                        string this.ChangeInStdDev
                        string this.ChangeInSharpeRatio
                        string this.CommissionCost
                        string this.SpreadCost
                        string this.CashEarned
                        string this.CashExpended
                        string this.Cashout
                    ]
                    |> Seq.append (Seq.ofArray this.Stocks |> Seq.map string))
            Frame.merge frame (Frame(columns, values))
    member  this.stocksValue prices =
        VectorOp.sumproduct(prices, this.Stocks)

    member this.dividendsFor dividends =
        VectorOp.sumproduct(dividends, this.Stocks)
    member this.TotalValue avgPrices = 
        this.stocksValue avgPrices
        + this.DividendsAccount
    member this.performanceFor date (marketData:MarketData) currentDividends =
        {
            Date = date
            StocksValue = this.stocksValue marketData.AvgPrices
            DividendsAccount = currentDividends + this.dividendsFor marketData.Dividends
        }
    member this.Weights date avgPrices =
        let prices = Frame.rowAsArray date avgPrices
        VectorOp.normalize(VectorOp.DotProduct(prices, this.Stocks))

let portfolioFromValuation date (currentPortfolio:Portfolio) (valuation:ValuationResult) riskFree =
    let stocks = Array.copy valuation.StocksAllocation
    stocks.[stocks.Length - 1] <- stocks.[stocks.Length - 1] + valuation.RebalancingCost.SharesBuySell

    {
        Date = date
        Stocks = stocks
        DividendsAccount = currentPortfolio.DividendsAccount
        ExpectedReturn = valuation.ExpectedReturn
        StdDev = valuation.StdDev
        ChangeInExpectedReturn = valuation.ExpectedReturn - currentPortfolio.ExpectedReturn
        ChangeInStdDev = valuation.StdDev - currentPortfolio.StdDev
        ChangeInSharpeRatio = valuation.SharpeRatio(riskFree) - currentPortfolio.SharpeRatio(riskFree)
        CommissionCost = valuation.RebalancingCost.CommissionCosts
        SpreadCost = valuation.RebalancingCost.SpreadCost
        CashEarned = valuation.RebalancingCost.CashEarned
        CashExpended = valuation.RebalancingCost.CashExpended
        Cashout = valuation.RebalancingCost.SharesBuySell
    }
let benchmarkPortfolioStocks cash (avgPrices:double[]) =
    let n = float avgPrices.Length
    VectorOp.division(cash / n, avgPrices)

let emptyPortfolio date n =
    {
        Date = date
        Stocks = Array.zeroCreate n
        DividendsAccount = 0.0
        ExpectedReturn = 0.0
        StdDev = 0.0
        ChangeInExpectedReturn = 0.0
        ChangeInStdDev = 0.0
        ChangeInSharpeRatio = 0.0
        CommissionCost = 0.0
        SpreadCost = 0.0
        CashEarned = 0.0
        CashExpended = 0.0
        Cashout = 0.0
    }

let portfolioWithOnlyCash date cash n =
    let zeroes = Array.zeroCreate n
    zeroes.[n-1] <- cash
    {emptyPortfolio date n with Stocks = zeroes }

let portfoliosToFrame assetNames =
    let emptyFrame = Frame<DateTime,String>(Seq.empty, Seq.empty)
    Seq.fold (fun (frame)(portfolio:Portfolio) -> portfolio.AddToFrame frame assetNames) emptyFrame

let performanceToFrame (portfolioPerformances : PortfolioPerformance seq)=
    let emptyFrame = Frame<DateTime,String>(Seq.empty, Seq.empty)
    portfolioPerformances
    |> Seq.fold (fun (frame)(performance:PortfolioPerformance) -> performance.AddToFrame frame) emptyFrame