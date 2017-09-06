namespace AdiminstracionDePortafolios


module Main =
    
    open Optimizacion
    open InputInterface

    let omega = loadMatrix "..\..\Omega.csv" false
    let expectedReturns = loadRow "..\..\ExpectedReturns.csv" false
    let initialValues = loadRow "..\..\InitialValues.csv" false

    let benchmarkOmega = loadMatrix "..\..\BenchmarkOmega.csv" false
    let benchmarkWeights = loadRow "..\..\BenchmarkWeights.csv" false
    
    [<EntryPoint>]
    let main argv =
        let opt = new Utilidad(lambda = 3.0, t = 5.0, omega = omega, expectedReturns = expectedReturns)
        let result = opt.Opt(initialValues)
        result.Variables
        |> Array.iter (printfn "%f")
        let arr = array2D [[2.;1.];[1.;2.]]
        let inverse = MatrixOp.invert arr

        let x = BlackLitterman.withNormalizingFactor benchmarkWeights benchmarkOmega 0.0633 0.08
        let adjustedReturns =
            ExpectationsInclusion.adjustedReturns
                [|0.074455629; 0.070676728; 0.07313265; 0.081624918; 0.111186519; 0.082504604; 0.082040492; 0.077847073; 0.075568456; 0.074764192|]
                [|0.005; 0.;0.;0.;-0.01; 0.005;0.;0.;0.;0.01|]
                benchmarkOmega
            
        printfn "%A" argv
        0 // devolver un código de salida entero
