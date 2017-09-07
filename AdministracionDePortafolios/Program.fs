namespace AdiminstracionDePortafolios


module Main =
    
    open Optimizacion
    open InputInterface
    open Deedle
    open System

    

    let omega = loadMatrix "..\..\Omega.csv" false
    let expectedReturns = loadRow "..\..\ExpectedReturns.csv" false
    let initialValues = loadRow "..\..\InitialValues.csv" false
    let prices = Frame.loadDateFrame "..\..\Prices.csv" "Fecha"
    let marketValues = Frame.loadDateFrame "..\..\MarketValue.csv" "Fecha"
    
    let returns = Frame.loadDateFrame "..\..\Rendimientos.csv" "Fecha"
    let deltas = Frame.loadDateFrame "..\..\Deltas.csv" "Fecha"

    let benchmarkOmega = loadMatrix "..\..\BenchmarkOmega.csv" false
    let benchmarkWeights = loadRow "..\..\BenchmarkWeights.csv" false

    let transactionCost = 0.001
    let riskFree = 0.05
    
        
    
    [<EntryPoint>]
    let main argv =

        let blankPortfolio = Array.zeroCreate (initialValues.Length)
        let opt1 = new Utility(lambda = 3.0, t = 5.0, omega = omega, expectedReturns = expectedReturns, previousPortfolio = blankPortfolio, transactionCost = transactionCost, riskFree = riskFree)
        let result1 = opt1.Opt(initialValues)
        result1.Weights
        |> Array.iter (printfn "%f")
        let opt2 = new Utility(lambda = 3.0, t = 5.0, omega = omega, expectedReturns = expectedReturns, previousPortfolio = blankPortfolio, transactionCost = 0.0, riskFree = riskFree)
        let result2 = opt2.Opt(initialValues)
        result2.Weights
        |> Array.iter (printfn "%f")

        let initDay = DateTime(2007,2,1)
        let filtered = returns.Rows.[initDay .. initDay.AddMonths(1).AddDays(-1.0)]

        let m = Frame.asMatrix returns
        let varcovar = MatrixOp.varcovar(m);

        let x = BlackLitterman.withNormalizingFactor benchmarkWeights benchmarkOmega 0.0633 0.08
        let adjustedReturns =
            ExpectationsInclusion.adjustedReturns
                [|0.074455629; 0.070676728; 0.07313265; 0.081624918; 0.111186519; 0.082504604; 0.082040492; 0.077847073; 0.075568456; 0.074764192|]
                [|0.005; 0.;0.;0.;-0.01; 0.005;0.;0.;0.;0.01|]
                benchmarkOmega
            
        printfn "%A" argv
        0