namespace AdiminstracionDePortafolios


module Main =
    
    open Optimizacion
    open InputInterface

    let omega = loadMatrix "..\..\Omega.csv" false
    let expectedReturns = loadRow "..\..\ExpectedReturns.csv" false
    let initialValues = loadRow "..\..\InitialValues.csv" false
    
    [<EntryPoint>]
    let main argv =
        let opt = new Utilidad(lambda = 3.0, t = 5.0, omega = omega, expectedReturns = expectedReturns)
        let result = opt.Opt(initialValues)
        result.Variables
        |> Array.iter (printfn "%f")
        printfn "%A" argv
        0 // devolver un código de salida entero
