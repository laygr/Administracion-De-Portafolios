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
    
    
        
    
    [<EntryPoint>]
    let main argv =
        

        let initDate = DateTime(2016, 09, 01)
        let endDate = DateTime(2017, 09, 01)
        let targetDate = DateTime(2019, 12, 31)
        (*
        let selectedStocks = [
                "BMV: ALSEA *"
                "BMV: AMX L"
                "BMV: ASUR B"
                "BMV: BIMBO A"
                "BMV: CEMEX CPO"
                "BMV: FEMSA UBD"
                "BMV: GMEXICO B"
                "BMV: GRUMA B"
                "BMV:OMA B"
                "BMV:WALMEX *"
            ]
            *)
        let selectedStocks =
            loadRowOfStrings "..\..\Input Data\Selected Stocks.csv" false
            |> Array.map(fun s -> s.ToUpper())
        let filterBySelectedStocks = Frame.filterCols(fun (c:string) _ -> Array.contains (c.ToUpper()) selectedStocks)
        let filterAndSortCols = filterBySelectedStocks >> Frame.sortColsByKey

        let returns =
            Frame.loadDateFrame "..\..\Input Data\All Market\Returns.csv" "Fecha"
            |> filterAndSortCols
        let marketCap =
            Frame.loadDateFrame "..\..\Input Data\All Market\Market Cap.csv" "Fecha"
            |> filterAndSortCols
        let expectedPrices =
            Frame.loadDateFrame "..\..\Input Data\All Market\Expected Prices.csv" "Fecha"
            |> filterAndSortCols
        let bidPrices =
            Frame.loadDateFrame "..\..\Input Data\All Market\Bid Prices.csv" "Fecha"
            |> filterAndSortCols
        let askPrices = 
            Frame.loadDateFrame "..\..\Input Data\All Market\Ask Prices.csv" "Fecha"
            |> filterAndSortCols

        let dividends = 
            Frame.loadDateFrame "..\..\Input Data\All Market\Dividends.csv" "Fecha"
            |> filterAndSortCols

        let avgPrices =
            bidPrices.Clone()
            |> Frame.map (fun r c bidPrice ->
                let askPrice = (askPrices.GetColumn c).[r]
                (bidPrice + askPrice)/2.0
            )

        let riskFreeRates = Series.loadDateSeries "..\..\Input Data\Risk Free Rates.csv" "Fecha" "SF43936"
        let initialCash = 14466136.00 
        let targetReturn = RatesM.Return.returnFor initialCash 32630537.75 

        let commission = 25.0/10000.0

        let datesForAnalysis =
            returns.RowKeys
            |> Seq.filter(fun date -> date >= initDate && date < endDate)

        let portfoliosFrame, performancesFrame =
            PortfolioManagement.simulateTargetReturn
                datesForAnalysis
                avgPrices dividends
                bidPrices askPrices
                returns marketCap
                expectedPrices
                riskFreeRates
                commission
                initDate targetDate targetReturn
                initialCash
            //PortfolioManagement.simulateBenchmark initDate endDate targetDate returns marketCap riskFreeRates transactionCost

        portfoliosFrame.SaveCsv("../../Output Data/Portfolios.csv",true)
        performancesFrame.SaveCsv("../../Output Data/Performances.csv",true)
        printfn "%A" argv
        0