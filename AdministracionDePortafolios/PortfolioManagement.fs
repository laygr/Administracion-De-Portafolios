module PortfolioManagement
open System
open Deedle
open Optimizacion
open FSharp.Data.HttpResponseHeaders

type Event = {
        Date : DateTime
        ValuationResult : ValuationResult
        Rebalanced : bool
        Return : float
        AccumReturn : float
    }
    with member this.AddToFrame (frame:Frame<DateTime,string>) assetNames =
            let seriesWithValue value =
                Series([this.Date], [value]) :> ISeries<DateTime>
            let columns =
                [
                    "Transaction Cost"
                    "Expected Return"
                    "Standard Deviation"
                    "Rebalanced"
                    "Return"
                    "Accumulated Return"
                ]
                |> Seq.append assetNames
            let values =
                Seq.map seriesWithValue
                    ([
                        string this.ValuationResult.TransactionCost
                        string this.ValuationResult.ExpectedReturn
                        string this.ValuationResult.StdDev
                        string this.Rebalanced
                        string this.Return
                        string this.AccumReturn
                    ]
                    |> Seq.append (Seq.ofArray this.ValuationResult.Weights |> Seq.map string))
            Frame.merge frame (Frame(columns, values))

let eventsToFrame assetNames =
    let emptyFrame = Frame<DateTime,String>(Seq.empty, Seq.empty)
    Seq.fold (fun (frame)(event:Event) -> event.AddToFrame frame assetNames) emptyFrame
    
let pickBestPortfolio (kind:OptimizationKinds) (newValuationOfCurrentPortfolio:ValuationResult) (newOptimization:ValuationResult) =
    if
        match kind with
        | OptimizationKinds.TargetReturn ->
            Math.Abs(newValuationOfCurrentPortfolio.Error) > Math.Abs(newOptimization.Error)
        | OptimizationKinds.TargetPortfolio ->
            Math.Abs(newValuationOfCurrentPortfolio.Error) > Math.Abs(newOptimization.Error)
        | OptimizationKinds.MaximizeUtility ->
            newValuationOfCurrentPortfolio.Utility < newOptimization.Utility
    then newOptimization, true
    else newValuationOfCurrentPortfolio, false

let smoothing value lambda percentage =
    value * (lambda + (1.0-lambda)*percentage)

let simulate (firstDate:DateTime) lastDate targetDate (returns:Frame<_,_>) (marketValues:Frame<_,_>) (deltas:Frame<_,_>) (riskFreeRates:Series<_,_>) targetReturn transactionCost =
    let pickBestPortfolio = pickBestPortfolio OptimizationKinds.TargetReturn
    let state = new StateForReturnTargeting()
    state.TransactionCost <- transactionCost
    state.Years <- 1.0
    let rand = System.Random()
    let datesForAnalysis =
        returns.RowKeys
        |> Seq.filter(fun date -> date >= firstDate && date < lastDate)

    seq {
        let mutable currentPortfolio = ValuationResult(returns.ColumnCount).Weights
        let mutable acumLogReturn = 0.0
        let mutable transactionCost = 0.0
        yield!
            datesForAnalysis
            |> Seq.map(fun today ->
                printfn "Today: %s" (today.ToString("yy.MM.dd"))
                let oneYearAgo = today.AddYears(-1)
                let benchmark = VectorOp.normalize(Frame.asArray marketValues.Rows.[today..today])
                let returnsForPeriod = returns.Rows.[oneYearAgo..today]
                let days = (today - oneYearAgo).TotalDays

                let varcovar = Frame.varcovar returnsForPeriod

                let expectedAnnualReturns =
                    let meanReturn = (Deedle.Stats.mean(returnsForPeriod)).Values |> Array.ofSeq
                    VectorOp.multiplication(meanReturn, days)
                
                let todayReturn =
                    let todayReturns = Frame.asArray (returns.Rows.[today..today])
                    VectorOp.sumproduct(todayReturns, currentPortfolio)
                acumLogReturn <- acumLogReturn + Returns.toLogarithmic (todayReturn - transactionCost)
                let accumReturn = Returns.toArithmeticReturn acumLogReturn
                printfn "Acum return %f" accumReturn

                let anticipatedBenchmarkReturn = VectorOp.sumproduct(expectedAnnualReturns, benchmark)
                let riskFree = 0.04 //riskFreeRates.[today]
                let impliedReturns = BlackLitterman.withNormalizingFactor benchmark varcovar anticipatedBenchmarkReturn riskFree
                let deltas = Frame.asArray deltas.Rows.[today..today]
                let expectedReturns = ExpectationsInclusion.adjustedReturns impliedReturns deltas varcovar
                let yearsToCashout =
                    let realYearsToCashout = FDatesM.yearsBetweenDates today targetDate
                    smoothing realYearsToCashout 0.5 (FDatesM.percentageOfTimeOccurred firstDate today targetDate)
                let accumReturn = Returns.toArithmeticReturn acumLogReturn
                let returnToGoal = targetReturn - accumReturn

                state.ExpectedReturns <- expectedAnnualReturns
                state.TargetReturn <- RatesM.Continuous.annualRateForReturn returnToGoal yearsToCashout
                
                state.Omega <- varcovar
                state.ExpectedReturns <- expectedReturns
                state.PreviousPortfolio <- currentPortfolio
                state.RiskFree <- riskFree
                let newValuationOfCurrentPortfolio  = Optimization.ValuePortfolio(state, currentPortfolio);
                let newOptimization                 = Optimization.Optimize(state, currentPortfolio)
                let newCurrentValuation, rebalanced = pickBestPortfolio newValuationOfCurrentPortfolio newOptimization
                currentPortfolio <- newCurrentValuation.Weights
                transactionCost <- newCurrentValuation.TransactionCost
                    
                { Date = today; ValuationResult = newCurrentValuation; Rebalanced = rebalanced; Return = todayReturn;  AccumReturn = accumReturn}
            )
    } |> eventsToFrame returns.ColumnKeys

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

                let varcovar = Frame.varcovar returnsForPeriod
                
                
                
                let riskFree = 0.04 //riskFreeRates.[today]
                let yearsToCashout = (targetDate - today).TotalDays / 365.

                let todayReturn =
                    let todayReturns = Frame.asArray (returns.Rows.[today..today])
                    VectorOp.sumproduct(todayReturns, currentPortfolio)
                acumLogReturn <- acumLogReturn + Returns.toLogarithmic (todayReturn - transactionCost)
                let accumReturn = Returns.toArithmeticReturn acumLogReturn

                state.TargetPortfolio <- benchmark
                state.Years <- yearsToCashout
                state.Omega <- varcovar
                state.PreviousPortfolio <- currentPortfolio
                state.RiskFree <- riskFree
                
                state.ExpectedReturns <- 
                    let meanReturn = (Deedle.Stats.mean(returnsForPeriod)).Values |> Array.ofSeq
                    VectorOp.multiplication(meanReturn, 250.0)
                let newValuationOfCurrentPortfolio  = Optimization.ValuePortfolio(state, currentPortfolio);
                let newOptimization                 = Optimization.Optimize(state, benchmark)
                let newCurrentValuation, rebalanced = pickBestPortfolio newValuationOfCurrentPortfolio newOptimization
                currentPortfolio <- newCurrentValuation.Weights
                transactionCost <- newCurrentValuation.TransactionCost
                
                { Date = today; ValuationResult = newCurrentValuation; Rebalanced = rebalanced; Return = todayReturn;  AccumReturn = accumReturn}
            )
    }
    |> eventsToFrame returns.ColumnKeys