namespace AdiminstracionDePortafolios


module Main =
    
    open Deedle
    open System
    open Optimizacion
    open BookKeeping

    (*
    let benchmarkOmega = loadMatrix "..\..\BenchmarkOmega.csv" false
    let benchmarkWeights = loadRow "..\..\BenchmarkWeights.csv" false
    *)

    
    let filterAndSortCols selectedStocks =
        let filterBySelectedStocks = Frame.filterCols(fun (c:string) _ -> Array.contains (c.ToUpper()) selectedStocks)
        filterBySelectedStocks >> Frame.sortColsByKey
    
    let returns = Frame.loadDateFrame "..\..\Input Data\Returns.csv" "Fecha"        
    let marketCap = Frame.loadDateFrame "..\..\Input Data\Market Cap.csv" "Fecha"
    let expectedPrices = Frame.loadDateFrame "..\..\Input Data\Expected Returns.csv" "Fecha"
    let bidPrices = Frame.loadDateFrame "..\..\Input Data\Bid Prices.csv" "Fecha"
    let askPrices = Frame.loadDateFrame "..\..\Input Data\Ask Prices.csv" "Fecha"
    let dividends = Frame.loadDateFrame "..\..\Input Data\Dividends.csv" "Fecha"
    let cashouts: Frame<_,_> = Frame.loadDateFrame "..\..\Input Data\Cashouts.csv" "Fecha"

    let avgPrices =
        bidPrices.Clone()
        |> Frame.map (fun r c bidPrice ->
            let askPrice = (askPrices.GetColumn c).[r]
            (bidPrice + askPrice)/2.0
        )
    
    let filteredData selectedStocks =
        let fs = filterAndSortCols selectedStocks
        fs returns, fs marketCap, fs expectedPrices, fs bidPrices, fs askPrices, fs avgPrices, fs dividends
    
    let initialCash = 14466136.00 
    let commission = 25.0/10000.0

    let initDate = DateTime(2016, 08, 26)
    let endDate = DateTime(2017, 09, 01)
    let targetDate = DateTime(2019, 12, 31)
    let datesForAnalysis =
        returns.RowKeys
        |> Seq.filter(fun date -> date >= initDate && date <= endDate)
    
    let simulatePortfolio () =
        let proportions = Frame.loadDateFrame "..\..\Input Data\Portfolio\Proportions.csv" "Fecha"
        let selectedStocks =
            [|
                "BMV: AMX L"; "BMV: CEMEX CPO";
                "BMV: FEMSA UBD"; "BMV: GAP B";
                "BMV: GRUMA B"; "BMV:AUTLAN B";
                "BMV:GFNORTE O";
                "BMV:GISSA A"; "BMV:HERDEZ *";
                "BMV:VITRO A"; "BMV: NAFTRAC";
                 "Z: CETES"
            |]
        (*
            loadRowOfStrings "..\..\Input Data\Selected Stocks.csv" false
            |> Array.map(fun s -> s.ToUpper())
            *)
        let returns, marketCap, expectedPrices, bidPrices, askPrices, avgPrices, dividends = filteredData selectedStocks

        let marketConstructor =
            PortfolioManagement.constructMarketData
                commission returns bidPrices askPrices avgPrices expectedPrices marketCap dividends
        
        let n = selectedStocks.Length
        
        let initialPorfolio = BookKeeping.portfolioWithOnlyCash initDate initialCash n

        let portfolioManagement today marketData (currentPortfolio:Portfolio) =
            let targetReturn = 1.432365 //RatesM.Return.returnFor initialCash 32630537.75 
            let datesForRebalancing = [
                DateTime(2016, 09, 01) ; DateTime(2016, 11, 10) ; DateTime(2016,12,31) ;
                DateTime(2017,03,30); DateTime(2017, 05, 02); DateTime(2017,08,30)
            ]
            let portfolio =
                PortfolioManagement.rebalanceForTargetReturn 
                    datesForRebalancing
                    initDate targetDate targetReturn initialCash proportions
                    today marketData currentPortfolio
            portfolio.CashoutMoney marketData ((cashouts?Cashout).[today])

        let portfolios, performances =
            PortfolioManagement.simulate initialPorfolio datesForAnalysis marketConstructor portfolioManagement
            //PortfolioManagement.simulateBenchmark initDate endDate targetDate returns marketCap riskFreeRates transactionCost
        let portfoliosFrame, performancesFrame =
            BookKeeping.portfoliosToFrame selectedStocks portfolios,
            BookKeeping.performanceToFrame performances

        portfoliosFrame.SaveCsv("../../Output Data/Portfolio/Portfolios.csv",true)
        performancesFrame.SaveCsv("../../Output Data/Portfolio/Performances.csv",true)

    let simulateBenchmark () =
        let selectedStocks = [| "BMV: NAFTRAC"; "Z: CETES"|]
        let returns, marketCap, expectedPrices, bidPrices, askPrices, avgPrices, dividends = filteredData selectedStocks

        let marketConstructor =
            PortfolioManagement.constructMarketData
                commission returns bidPrices askPrices avgPrices expectedPrices marketCap dividends
        
        let n = selectedStocks.Length
        
        let portfolioManagement (today:DateTime) marketData (currentPortfolio:Portfolio) =
            let newValuation  = RebalancingValuation.ValuePortfolio(marketData, currentPortfolio.Stocks, currentPortfolio.Stocks)
            let portfolio = portfolioFromValuation today currentPortfolio newValuation marketData.RiskFree
            portfolio.CashoutMoney marketData ((cashouts?Cashout).[today])

        (*
        let initialPorfolio =
            let marketData = marketConstructor initDate
            let blankPortfolio = BookKeeping.emptyPortfolio initDate n
            let equallyWeightedPortfolio = BookKeeping.benchmarkPortfolioStocks initialCash marketData.AvgPrices
            let newValuation  = RebalancingValuation.ValuePortfolio(marketData, equallyWeightedPortfolio, equallyWeightedPortfolio)
            portfolioFromValuation initDate blankPortfolio newValuation marketData.RiskFree
            |> portfolioManagement initDate (marketConstructor initDate)
        *)
        let initialPorfolio =
            let marketData = marketConstructor initDate
            let blankPortfolio = BookKeeping.emptyPortfolio initDate n
            let portfolio = VectorOp.DotDivision(VectorOp.multiplication([|0.8 ; 0.2|], initialCash), marketData.AvgPrices)
            let newValuation  = RebalancingValuation.ValuePortfolio(marketData, portfolio, portfolio)
            portfolioFromValuation initDate blankPortfolio newValuation marketData.RiskFree
            |> portfolioManagement initDate (marketConstructor initDate)

        let portfolios, performances =
            PortfolioManagement.simulate initialPorfolio datesForAnalysis marketConstructor portfolioManagement
        let portfoliosFrame, performancesFrame =
            BookKeeping.portfoliosToFrame selectedStocks portfolios,
            BookKeeping.performanceToFrame performances

        portfoliosFrame.SaveCsv("../../Output Data/Benchmark/Portfolios.csv",true)
        performancesFrame.SaveCsv("../../Output Data/Benchmark/Performances.csv",true)

    [<EntryPoint>]
    let main argv =
        simulatePortfolio()
        simulateBenchmark()
        printfn "%A" argv
        0