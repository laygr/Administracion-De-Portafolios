module BlackLitterman

open Optimizacion
open AuxFuncs

let withNormalizingFactor (weights:float[]) (varcovar:float[,]) (anticipatedBenchmarkReturn:float) (riskFree:float) : float[] =
    let weightsM = MatrixOp.matrixFrowRow weights
    let impliedReturnsM =
        (
        ((varcovar * (MatrixOp.transpose(weightsM))) *^ (anticipatedBenchmarkReturn - riskFree))
        /^
        (weightsM * varcovar * (MatrixOp.transpose(weightsM))).[0,0]
        ) +^ riskFree
    impliedReturnsM.[*, 0]