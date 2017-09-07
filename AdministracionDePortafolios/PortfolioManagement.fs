module PortfolioManagement
open System
open Deedle
open Optimizacion

type Event = {
        UtilityResult : UtilityResult
        Rebalanced : bool
    }

let simulate (prices:Frame<_,_>) (marketValues:Frame<_,_>) (deltas:Frame<_,_>) targetDate lambda transactionCost riskFree =
    let returns = Returns.fromPrices prices
    let dates = returns.RowKeys |> Seq.filter(fun date -> date < DateTime(2007,1,1))
    let events = seq {
        let initialState = { UtilityResult = UtilityResult(returns.ColumnCount); Rebalanced = false }
        let mutable currentPortfolio = initialState.UtilityResult.Weights
        yield initialState
        yield!
            dates
            |> Seq.map(fun oneYearAgo ->
                let today = oneYearAgo.AddYears(1)
                let benchmark = VectorOp.normalize(Frame.asArray marketValues.Rows.[today..today])
                let returnsForPeriod = Frame.asMatrix returns.Rows.[oneYearAgo..today]
                let varcovar = MatrixOp.varcovar(returnsForPeriod)
                let anticipatedBenchmarkReturn = 0.0
                let otherThing = 0.0
                let impliedReturns = BlackLitterman.withNormalizingFactor benchmark varcovar anticipatedBenchmarkReturn otherThing
                let deltas = Frame.asArray deltas.Rows.[today..today]
                let expectedReturns = ExpectationsInclusion.adjustedReturns impliedReturns deltas varcovar
                let yearsToCashout = (targetDate - today).TotalDays / 365.

                let currentState = State(lambda, yearsToCashout, varcovar, expectedReturns, currentPortfolio, transactionCost, riskFree)
                let newValuationOfCurrentPortfolio = currentState.Utility(currentPortfolio);
                let newOptimization = currentState.OptimizeForUtility(currentPortfolio)
                let newCurrentValuation, rebalanced =
                    if newValuationOfCurrentPortfolio.Utility < newOptimization.Utility
                    then newOptimization, true
                    else newValuationOfCurrentPortfolio, false
                { UtilityResult = newCurrentValuation; Rebalanced = rebalanced }
            )
    }
    events