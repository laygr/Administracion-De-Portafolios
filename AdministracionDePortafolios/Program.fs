namespace AdiminstracionDePortafolios


module Main =
    
    open Optimizacion
    open InputInterface
    open Deedle
    open System
    open BookKeeping

    (*
    let benchmarkOmega = loadMatrix "..\..\BenchmarkOmega.csv" false
    let benchmarkWeights = loadRow "..\..\BenchmarkWeights.csv" false
    *)

    
    let filterAndSortCols selectedStocks =
        let filterBySelectedStocks = Frame.filterCols(fun (c:string) _ -> Array.contains (c.ToUpper()) selectedStocks)
        filterBySelectedStocks >> Frame.sortColsByKey
    
    let returns = Frame.loadDateFrame "..\..\Input Data\All Market\Returns.csv" "Fecha"        
    let marketCap = Frame.loadDateFrame "..\..\Input Data\All Market\Market Cap.csv" "Fecha"
    let expectedPrices = Frame.loadDateFrame "..\..\Input Data\All Market\Expected Prices.csv" "Fecha"
    let bidPrices = Frame.loadDateFrame "..\..\Input Data\All Market\Bid Prices.csv" "Fecha"
    let askPrices = Frame.loadDateFrame "..\..\Input Data\All Market\Ask Prices.csv" "Fecha"
    let dividends = Frame.loadDateFrame "..\..\Input Data\All Market\Dividends.csv" "Fecha"

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
    let cashoutDates = [
        DateTime(2016,09,30); DateTime(2016,10,28); DateTime(2016,11,25); DateTime(2016,12,30);
        DateTime(2017,01,27); DateTime(2017,02,24); DateTime(2017,03,31); DateTime(2017,04,28); DateTime(2017,05,26);
        DateTime(2017,06,30); DateTime(2017,07,28); DateTime(2017,08,25); 
    ]

    [<EntryPoint>]
    let main argv =
        
        
        let selectedStocks =
            [|
                "BMV:GISSA A";"BMV: GRUMA B"; (*"BMV: BACHOCO B"; *)"BMV:VITRO A"; "BMV: GAP B"; "BMV:GFNORTE O";
                "BMV:HERDEZ *"; "BMV: AMX L"; "BMV: FEMSA UBD"; "BMV: CEMEX CPO"; "BMV:AUTLAN B"; "MXN"
            |]
        (*
            loadRowOfStrings "..\..\Input Data\Selected Stocks.csv" false
            |> Array.map(fun s -> s.ToUpper())
            *)
        let returns, marketCap, expectedPrices, bidPrices, askPrices, avgPrices, dividends = filteredData selectedStocks
        let riskFreeRates = Series.loadDateSeries "..\..\Input Data\Risk Free Rates.csv" "Fecha" "SF43936"
        let n = selectedStocks.Length
        
        let targetReturn = 1.432365 //RatesM.Return.returnFor initialCash 32630537.75 
        
        let datesForRebalancing = [DateTime(2016, 09, 02); DateTime(2016, 11, 11)]
        
        let initialPorfolio = BookKeeping.portfolioWithOnlyCash initDate initialCash n

        let rebalancing =
            PortfolioManagement.rebalanceForTargetReturn 
                datesForRebalancing
                initDate targetDate targetReturn initialCash

        let monthlyCashout = 1.02**(1.0/12.0) - 1.0
        let cashoutSimulator = PortfolioManagement.cashout monthlyCashout cashoutDates 

        let marketConstructor =
            PortfolioManagement.constructMarketData
                commission returns bidPrices askPrices avgPrices expectedPrices marketCap dividends riskFreeRates

        let portfolios, performances =
            PortfolioManagement.simulateTargetReturn initialPorfolio datesForAnalysis marketConstructor rebalancing cashoutSimulator
            //PortfolioManagement.simulateBenchmark initDate endDate targetDate returns marketCap riskFreeRates transactionCost
        let portfoliosFrame, performancesFrame =
            BookKeeping.portfoliosToFrame selectedStocks portfolios,
            BookKeeping.performanceToFrame performances

        portfoliosFrame.SaveCsv("../../Output Data/Portfolios.csv",true)
        performancesFrame.SaveCsv("../../Output Data/Performances.csv",true)
        printfn "%A" argv
        0