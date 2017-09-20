module BookKeeping
open System
open Deedle
open Optimizacion
open FSharp.Data.HttpResponseHeaders
open System.Diagnostics

type PortfolioPerformance = {
    Date : DateTime
    StocksValue : double
    Cash : double
    DividendsReceived : double
} with
    member this.AddToFrame (frame:Frame<DateTime,string>) =
        let seriesWithValue value =
                Series([this.Date], [value]) :> ISeries<DateTime>
        let columns =
            [ "Stocks' Value"; "Cash"; "DividendsReceived"
            ]
        let values =
            Seq.map seriesWithValue
                ([
                    string this.StocksValue
                    string this.Cash
                    string this.DividendsReceived
                ])
        Frame.merge frame (Frame(columns, values))
type Portfolio = {
    Date : DateTime
    Stocks : float[]
    Money : float
    ExpectedReturn : float
    StdDev : float
    ChangeInExpectedReturn : float
    ChangeInStdDev : float
    ChangeInSharpeRatio : float
    CommissionCost : float
    SpreadCost : float
} with
    member this.AddToFrame (frame:Frame<DateTime,string>) assetNames =
            let seriesWithValue value =
                Series([this.Date], [value]) :> ISeries<DateTime>
            let columns =
                [ "Money"; "Expected Return"; "Standard Deviation"; "Change In Expected Return";
                    "Change In Std Dev"; "Change In Sharpe Ratio"; "Commission Cost"; "Spread Cost";
                ]
                |> Seq.append assetNames
            let values =
                Seq.map seriesWithValue
                    ([
                        string this.Money
                        string this.ExpectedReturn
                        string this.StdDev
                        string this.ChangeInExpectedReturn
                        string this.ChangeInStdDev
                        string this.ChangeInSharpeRatio
                        string this.CommissionCost
                        string this.SpreadCost
                    ]
                    |> Seq.append (Seq.ofArray this.Stocks |> Seq.map string))
            Frame.merge frame (Frame(columns, values))
    member  this.stocksValue date avgPrices =
        let avgPrices = Frame.rowAsArray date avgPrices
        VectorOp.sumproduct(avgPrices, this.Stocks)

    member this.dividendsFor date dividends =
        let dividends = Frame.rowAsArray date dividends
        VectorOp.sumproduct(dividends, this.Stocks)
    member this.TotalValue date avgPrices = 
        this.stocksValue date avgPrices
        + this.Money
    member this.performanceFor date dividends avgPrices =
        {
            Date = date
            StocksValue = this.stocksValue date avgPrices 
            Cash = this.Money
            DividendsReceived = this.dividendsFor date dividends
        }
    member this.Weights date avgPrices =
        let prices = Frame.rowAsArray date avgPrices
        VectorOp.normalize(VectorOp.DotProduct(prices, this.Stocks))

let portfolioFromValuation date (valuation:ValuationResult) (newValuationOfPreviousPortfolio:ValuationResult) riskFree =
    let stocks = Array.copy valuation.StocksAllocation
    stocks.[stocks.Length - 1] <- stocks.[stocks.Length - 1] + valuation.RebalancingCost.SharesBuySell

    {
        Date = date
        Stocks = stocks
        Money = 0.0
        ExpectedReturn = valuation.ExpectedReturn
        StdDev = valuation.StdDev
        ChangeInExpectedReturn = valuation.ExpectedReturn - newValuationOfPreviousPortfolio.ExpectedReturn
        ChangeInStdDev = valuation.StdDev - newValuationOfPreviousPortfolio.StdDev
        ChangeInSharpeRatio = valuation.SharpeRatio(riskFree) - newValuationOfPreviousPortfolio.SharpeRatio(riskFree)
        CommissionCost = valuation.RebalancingCost.CommissionCosts
        SpreadCost = valuation.RebalancingCost.SpreadCost
    }

let portfolioWithOnlyCash date cash n =
    let zeroes = Array.zeroCreate n
    zeroes.[n-1] <- cash
    {
        Date = date
        Stocks = zeroes
        Money = 0.0
        ExpectedReturn = 0.0
        StdDev = 0.0
        ChangeInExpectedReturn = 0.0
        ChangeInStdDev = 0.0
        ChangeInSharpeRatio = 0.0
        CommissionCost = 0.0
        SpreadCost = 0.0
    }

let portfoliosToFrame assetNames =
    let emptyFrame = Frame<DateTime,String>(Seq.empty, Seq.empty)
    Seq.fold (fun (frame)(portfolio:Portfolio) -> portfolio.AddToFrame frame assetNames) emptyFrame

let performanceToFrame (portfolioPerformances : PortfolioPerformance seq)=
    let emptyFrame = Frame<DateTime,String>(Seq.empty, Seq.empty)
    portfolioPerformances
    |> Seq.fold (fun (frame)(performance:PortfolioPerformance) -> performance.AddToFrame frame) emptyFrame