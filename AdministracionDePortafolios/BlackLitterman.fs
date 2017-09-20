module BlackLitterman

open Optimizacion
open AuxFuncs

let ifNaNReplaceWith replacement value =
    if System.Double.IsNaN value
    then replacement
    else value

let replaceNaNs (replacements:float[]) =
    Array.mapi(fun i v -> ifNaNReplaceWith replacements.[i] v) 
    
let withNormalizingFactor (weights:float[]) (varcovar:float[,]) (anticipatedBenchmarkReturn:float) (riskFree:float) expectedReturns : float[] =
    let weightsM = MatrixOp.matrixFromRow weights
    let impliedReturnsM =
        (
        ((varcovar *! (MatrixOp.transpose(weightsM))) *^ (anticipatedBenchmarkReturn - riskFree))
        /^
        (weightsM *! varcovar *! (MatrixOp.transpose(weightsM))).[0,0]
        ) +^ riskFree

    let n = weights.Length
    impliedReturnsM.[n-1, 0] <- 0.0

    impliedReturnsM.[*, 0]
    |> replaceNaNs expectedReturns

let adjustedReturns (previousExpectedReturns:float[]) (deltas:float[]) (varcovar:float[,]) =
    previousExpectedReturns
    |> Array.mapi(fun i expectedReturn ->
        expectedReturn +
            (deltas
             |> Array.mapi(fun j d ->
                let div = varcovar.[i,j]/varcovar.[i,i] |> ifNaNReplaceWith 0.0
                d * div)
             |> Array.sum)
        )

let shrinkM (lambda:double) (m:float[,]) =
    m *^ (lambda) +! MatrixOp.diagonal(m)*^(1.0-lambda)
    