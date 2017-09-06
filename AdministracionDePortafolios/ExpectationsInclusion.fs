module ExpectationsInclusion

let adjustedReturns (previousExpectedReturns:float[]) (delta:float[]) (varcovar:float[,]) =
    previousExpectedReturns
    |> Array.mapi(fun i expectedReturn ->
        expectedReturn +
            (delta
             |> Array.mapi(fun j d -> d * varcovar.[i,j]/varcovar.[i,i])
             |> Array.sum)
        )
    