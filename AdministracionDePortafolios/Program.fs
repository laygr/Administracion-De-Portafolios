namespace AdiminstracionDePortafolios


module Main =
    
    open Optimizacion
    open InputInterface
    open Deedle
    open System

    (*
    let benchmarkOmega = loadMatrix "..\..\BenchmarkOmega.csv" false
    let benchmarkWeights = loadRow "..\..\BenchmarkWeights.csv" false
    *)
    
    
        
    
    [<EntryPoint>]
    let main argv =
        

        let initDate = DateTime(2015, 09, 01)
        let endDate = DateTime(2016, 09, 01)
        let targetDate = DateTime(2019, 12, 31)
        let returns = Frame.loadDateFrame "..\..\Input Data\Benchmark Returns.csv" "Fecha"
        let marketCap = Frame.loadDateFrame "..\..\Input Data\Benchmark Market Cap.csv" "Fecha"
        let deltas = Frame.loadDateFrame "..\..\Input Data\Benchmark Deltas.csv" "Fecha"
        let riskFreeRates = Series.loadDateSeries "..\..\Input Data\Risk Free Rates.csv" "Fecha" "SF43936"
        let targetReturn = RatesM.Return.returnFor 14466136.00 32630537.75 

        let transactionCost = 10.0/10000.0

        let frame =
            PortfolioManagement.simulate initDate endDate targetDate returns marketCap deltas riskFreeRates targetReturn transactionCost
            //PortfolioManagement.simulateBenchmark initDate endDate targetDate returns marketCap riskFreeRates transactionCost

        frame.SaveCsv("../../Output Data/result.csv",true)
        printfn "%A" argv
        0