﻿module PortfolioManagement
open System
open Deedle
open Optimizacion
open BookKeeping
    
let pickBestPortfolio (kind:OptimizationKinds) (newValuationOfCurrentPortfolio:ValuationResult) (newOptimization:ValuationResult) riskFreeRate =
    if
        match kind with
        | OptimizationKinds.TargetReturn ->
             newOptimization.SharpeRatio(riskFreeRate) > newValuationOfCurrentPortfolio.SharpeRatio(riskFreeRate)
             || true
            //&& Math.Abs(newOptimization.Error) < Math.Abs(newValuationOfCurrentPortfolio.Error))
        | OptimizationKinds.TargetPortfolio ->
            true
            // Math.Abs(newValuationOfCurrentPortfolio.Error) > Math.Abs(newOptimization.Error)
        | OptimizationKinds.MaximizeUtility ->
            true
            //newValuationOfCurrentPortfolio.Utility < newOptimization.Utility
    then newOptimization
    else newValuationOfCurrentPortfolio

let smoothing value lambda percentage =
    value * (lambda + (1.0-lambda)*percentage)

let weightsForStocksAllocation stocksAllocation prices =
    VectorOp.normalize(VectorOp.DotProduct(prices, stocksAllocation))
    
let roundStocks (state:MarketData) currentStocksAllocation newStocksAllocation=
    let stocksAllocation =
        newStocksAllocation
        |> Array.map (fun (s:float) -> Math.Floor s)
    RebalancingValuation.ValuePortfolio(state, currentStocksAllocation, stocksAllocation)

let constructMarketData
        commission
        (returns:Frame<_,_>)
        bidPrices askPrices avgPrices (expectedPrices:Frame<_,_>) (marketCapValues:Frame<_,_>) dividends
        (today:DateTime) =
    let oneYearAgo = today.AddYears(-1)
    
    
    let returnsForPeriod = returns.Rows.[oneYearAgo..today]
    let marketData = new MarketData()
    marketData.Commission <- commission
    marketData.Omega <- Frame.varcovar 50. returnsForPeriod
    marketData.AvgPrices <- Frame.rowAsArray today avgPrices
    marketData.BidPrices <- Frame.rowAsArray today bidPrices
    marketData.AskPrices <- Frame.rowAsArray today askPrices
    marketData.Dividends <- Frame.rowAsArray today dividends
    marketData.ExpectedReturns <-
        (*
        let returnsForPeriod = returns.Rows.[oneYearAgo..today]
        let expectedAnnualReturns =
            let meanReturn = (Deedle.Stats.mean(returnsForPeriod)).Values |> Array.ofSeq
            VectorOp.multiplication(meanReturn, 50.0)
        let expectedReturns' =
            
            let currentPrices = Frame.rowAsArray today avgPrices
            let expectedPricesA = (Frame.rowAsArray today expectedPrices)
            let expectedReturns = VectorOp.Addition(VectorOp.DotDivision(expectedPricesA, currentPrices), - 1.0)
            let dateOfPronostic = DateTime(2017, 09,01)
                //DateTime.Parse(expectedPrices.GetColumn("Fecha de pronostico").[today].ToString())
            VectorOp.division(expectedReturns, (dateOfPronostic - today).TotalDays / 360.0)
        let benchmark = VectorOp.normalize(Frame.asArray marketCapValues.Rows.[today..today])       
        let anticipatedBenchmarkReturn = VectorOp.sumproduct(expectedAnnualReturns, benchmark)
        let impliedReturns = BlackLitterman.withNormalizingFactor benchmark marketData.Omega anticipatedBenchmarkReturn riskFreeRate expectedAnnualReturns
        let deltas = VectorOp.DotSubtraction(expectedReturns', impliedReturns)
        BlackLitterman.adjustedReturns impliedReturns deltas marketData.Omega
        expectedReturns'
        *)
        Frame.rowAsArray today expectedPrices
    marketData

let rebalanceForTargetReturn
    datesForRebalancing
    initDate targetDate targetReturn initialCash (proportions:Frame<_,_>)
    (today: DateTime) (marketData:MarketData) (currentPortfolio:Portfolio) //provided by simulator
    =
    let state = new ReturnTargetingParameters()
    
    state.TargetReturn <-
        let yearsToCashout =
            let realYearsToCashout = FDatesM.yearsBetweenDates today targetDate
            smoothing realYearsToCashout 0.5 (FDatesM.percentageOfTimeOccurred initDate today targetDate)
        let returnToGoal =
            let accumReturn = Returns.fromValues initialCash (currentPortfolio.TotalValue marketData.AvgPrices)
            targetReturn - accumReturn
        let annualReturnToGoal = RatesM.Continuous.annualRateForReturn returnToGoal yearsToCashout
        let idealTargetReturn = if annualReturnToGoal < marketData.RiskFree then marketData.RiskFree else annualReturnToGoal
        let maxExpectedReturn = Array.max marketData.ExpectedReturns
        if maxExpectedReturn < idealTargetReturn then maxExpectedReturn else idealTargetReturn
                
    state.CurrentStocksAllocation <- currentPortfolio.Stocks
    state.MarketData <- marketData
    state.HoldProportions <- (proportions?Hold).[today] = 1.0
    state.IPCProportion <- (proportions?IPC).[today]
    state.StocksProportion <- (proportions?Stocks).[today]

    let newValuationOfCurrentPortfolio  =
        RebalancingValuation.ValuePortfolio(marketData, currentPortfolio.Stocks, currentPortfolio.Stocks)
    let newCurrentValuation =
        if Seq.contains today datesForRebalancing
        then
            let newOptimization =
                let optimization = Optimization.Optimize(state, currentPortfolio.Stocks)
                roundStocks marketData currentPortfolio.Stocks (optimization.StocksAllocation)
            pickBestPortfolio OptimizationKinds.TargetReturn newValuationOfCurrentPortfolio newOptimization marketData.RiskFree
        else
            newValuationOfCurrentPortfolio
    
    portfolioFromValuation today currentPortfolio newCurrentValuation marketData.RiskFree
   
let simulate
        (initialPortfolio:Portfolio)
        datesForAnalysis
        marketDataConstructor
        managementSimulator
        =
    let mutable portfolios = List.empty
    let mutable performances = List.empty
    
    let mutable currentPortfolio = initialPortfolio

    Seq.pairwise datesForAnalysis
    |> Seq.iter(fun (previousDay, today) ->
        let todayMarketData = marketDataConstructor today
        let yesterdayMarketData = marketDataConstructor previousDay
        currentPortfolio <- managementSimulator today yesterdayMarketData currentPortfolio 
        portfolios <- List.append portfolios [currentPortfolio]
        let performance = currentPortfolio.performanceFor today todayMarketData currentPortfolio.DividendsAccount

        performances <- List.append performances [performance]
    )
    portfolios, performances 