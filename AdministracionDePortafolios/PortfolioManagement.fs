module PortfolioManagement
open System
open Deedle
open Optimizacion
open BookKeeping
    
let pickBestPortfolio (kind:OptimizationKinds) (newValuationOfCurrentPortfolio:ValuationResult) (newOptimization:ValuationResult) riskFreeRate =
    if
        match kind with
        | OptimizationKinds.TargetReturn ->
            (newOptimization.ExpectedReturn < newValuationOfCurrentPortfolio.ExpectedReturn
            && newOptimization.SharpeRatio(riskFreeRate) > newValuationOfCurrentPortfolio.SharpeRatio(riskFreeRate))
            ||
            (newOptimization.ExpectedReturn >= newValuationOfCurrentPortfolio.ExpectedReturn
            && newOptimization.SharpeRatio(riskFreeRate) > newValuationOfCurrentPortfolio.SharpeRatio(riskFreeRate))
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
    
let roundStocks (state:State) (portfolioValuation:ValuationResult) =
    let stocksAllocation =
        portfolioValuation.StocksAllocation
        |> Array.map (fun (s:float) -> Math.Floor s)
        //|> Array.map (fun (s:float) -> if s < 0.0 then 0.0 else s)
    state.ShouldRebalance <- false
    RebalancingValuation.ValuePortfolio(state, stocksAllocation)

let targetReturnSimulator
    (bidPrices:Frame<_,_>) (askPrices:Frame<_,_>) (avgPrices:Frame<_,_>) (returns:Frame<_,_>) (marketValues:Frame<_,_>) (expectedPrices:Frame<_,_>)
    (riskFreeRates:Series<_,_>) 
    commission
    initDate targetDate targetReturn
    initialCash
    (today: DateTime) 
    (currentPortfolio:Portfolio)
    =
    let oneYearAgo = today.AddYears(-1)
    let benchmark = VectorOp.normalize(Frame.asArray marketValues.Rows.[today..today])
    let returnsForPeriod = returns.Rows.[oneYearAgo..today]

    let varcovar =
        Frame.varcovar 250. returnsForPeriod
        //|> BlackLitterman.shrinkM 0.7

    let expectedAnnualReturns =
        let meanReturn = (Deedle.Stats.mean(returnsForPeriod)).Values |> Array.ofSeq
        VectorOp.multiplication(meanReturn, 250.0)
                    
    let anticipatedBenchmarkReturn = VectorOp.sumproduct(expectedAnnualReturns, benchmark)
    let riskFreeRate = 0.04 //riskFreeRates.[today]
    let impliedReturns = BlackLitterman.withNormalizingFactor benchmark varcovar anticipatedBenchmarkReturn riskFreeRate expectedAnnualReturns
    let expectedReturns' =
        let currentPrices = Frame.rowAsArray today avgPrices
        let expectedPricesA = (Frame.rowAsArray today expectedPrices)
        let expectedReturns = VectorOp.Addition(VectorOp.DotDivision(expectedPricesA, currentPrices), - 1.0)
        let dateOfPronostic = DateTime(2017, 09,01)
            //DateTime.Parse(expectedPrices.GetColumn("Fecha de pronostico").[today].ToString())
        VectorOp.division(expectedReturns, (dateOfPronostic - today).TotalDays / 360.0)
    let deltas = VectorOp.DotSubtraction(expectedReturns', impliedReturns)
        
    let expectedReturns = BlackLitterman.adjustedReturns impliedReturns deltas varcovar
    let yearsToCashout =
        let realYearsToCashout = FDatesM.yearsBetweenDates today targetDate
        smoothing realYearsToCashout 0.5 (FDatesM.percentageOfTimeOccurred initDate today targetDate)
    let returnToGoal =
        let accumReturn = Returns.fromValues initialCash (currentPortfolio.TotalValue today avgPrices)
        targetReturn - accumReturn

    let state = new StateForReturnTargeting()
    state.TransactionCost <- commission
    state.Years <- 1.0
    
    state.ExpectedReturns <- expectedReturns
    state.TargetReturn <-
        let idealTargetReturn = RatesM.Continuous.annualRateForReturn returnToGoal yearsToCashout
        let maxExpectedReturn = Array.max state.ExpectedReturns
        if maxExpectedReturn < idealTargetReturn then maxExpectedReturn else idealTargetReturn
                
    state.Omega <- varcovar
    state.PreviousPortfolio <- currentPortfolio.Stocks
    state.RiskFree <- riskFreeRate
    state.AvgPrices <- Frame.rowAsArray today avgPrices
    state.BidPrices <- Frame.rowAsArray today bidPrices
    state.AskPrices <- Frame.rowAsArray today askPrices
    state.AvailableCash <- currentPortfolio.Money
    state.ShouldRebalance <- false
    let newValuationOfCurrentPortfolio  =
        RebalancingValuation.ValuePortfolio(state, currentPortfolio.Stocks)
        |> roundStocks state
    state.ShouldRebalance <- true
    let newOptimization =
        Optimization.Optimize(state, currentPortfolio.Stocks)
        |> roundStocks state
    let newCurrentValuation = pickBestPortfolio OptimizationKinds.TargetReturn newValuationOfCurrentPortfolio newOptimization riskFreeRate
    portfolioFromValuation today newCurrentValuation newValuationOfCurrentPortfolio riskFreeRate
    
let simulate
        (firstDate:DateTime) initialCash datesForAnalysis
        avgPrices dividends
        simulator
        =
    let n = Frame.countCols dividends
    let mutable portfolios = List.empty
    let mutable performances = List.empty
    
    let mutable currentPortfolio = BookKeeping.portfolioWithOnlyCash firstDate initialCash n

    datesForAnalysis
    |> Seq.iter(fun today ->
        printfn "Today: %s" (today.ToString())
        let performance = currentPortfolio.performanceFor today dividends avgPrices
        performances <- List.append performances [performance]
        currentPortfolio <- { currentPortfolio with Money = performance.DividendsReceived + currentPortfolio.Money }
        
        currentPortfolio <- simulator today currentPortfolio
        portfolios <- List.append portfolios [currentPortfolio]
    )
    BookKeeping.portfoliosToFrame (dividends.ColumnKeys) portfolios, BookKeeping.performanceToFrame performances

let simulateTargetReturn
    datesForAnalysis avgPrices dividends
    (bidPrices:Frame<_,_>) (askPrices:Frame<_,_>) (returns:Frame<_,_>) (marketValues:Frame<_,_>) (expectedPrices:Frame<_,_>)
    (riskFreeRates:Series<_,_>) 
    commission
    initDate targetDate targetReturn
    initialCash =
        let simulator =
            targetReturnSimulator 
                bidPrices askPrices avgPrices returns marketValues expectedPrices
                riskFreeRates
                commission
                initDate targetDate targetReturn initialCash

        simulate initDate initialCash datesForAnalysis avgPrices dividends simulator
                     

(*
let simulateBenchmark (firstDate:DateTime) lastDate targetDate (returns:Frame<_,_>) (marketValues:Frame<_,_>) (riskFreeRates:Series<_,_>) transactionCost =
    let pickBestPortfolio = pickBestPortfolio OptimizationKinds.TargetReturn
    let state = new StateForPortfolioTargeting()
    state.TransactionCost <- transactionCost
    
    let datesForAnalysis = returns.RowKeys |> Seq.filter(fun date -> date >= firstDate && date < lastDate)

    seq {
        let mutable currentPortfolio = ValuationResult(returns.ColumnCount).Weights
        let mutable acumLogReturn = 0.0
        let mutable transactionCost = 0.0
        yield!
            datesForAnalysis
            |> Seq.map(fun today ->
                printfn "Today: %s" (today.ToString("yy.MM.dd"))
                let oneYearAgo = today.AddYears(-1)
                let benchmark = Array.create 19 (1.0/19.0) //VectorOp.normalize(Frame.asArray marketValues.Rows.[today..today])
                let returnsForPeriod = returns.Rows.[oneYearAgo..today]

                let varcovar = Frame.varcovar 250. returnsForPeriod
                
                
                
                let riskFreeRate = 0.04 //riskFreeRates.[today]
                let yearsToCashout = (targetDate - today).TotalDays / 365.

                let todayReturn =
                    let todayReturns = Frame.asArray (returns.Rows.[today..today])
                    VectorOp.sumproduct(todayReturns, currentPortfolio)
                acumLogReturn <- acumLogReturn + (Returns.toLogarithmic (1.0 - transactionCost) * todayReturn)
                let accumReturn = Returns.toArithmeticReturn acumLogReturn

                state.TargetPortfolio <- benchmark
                state.Years <- yearsToCashout
                state.Omega <- varcovar
                state.PreviousPortfolio <- currentPortfolio
                state.RiskFree <- riskFreeRate
                
                state.ExpectedReturns <- 
                    let meanReturn = (Deedle.Stats.mean(returnsForPeriod)).Values |> Array.ofSeq
                    VectorOp.multiplication(meanReturn, 250.0)
                let newValuationOfCurrentPortfolio  = Optimization.ValuePortfolio(state, currentPortfolio);
                let newOptimization                 = Optimization.Optimize(state, benchmark)
                let newCurrentValuation, rebalanced = pickBestPortfolio newValuationOfCurrentPortfolio newOptimization riskFreeRate
                currentPortfolio <- newCurrentValuation.Weights
                transactionCost <- newCurrentValuation.TransactionCost
                
                { Date = today; ValuationResult = newCurrentValuation; Rebalanced = rebalanced; Return = todayReturn;  AccumReturn = accumReturn}
            )
    }
    |> eventsToFrame returns.ColumnKeys
*)