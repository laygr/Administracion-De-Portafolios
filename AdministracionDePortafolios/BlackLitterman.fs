module BlackLitterman

open Optimizacion

let (*) m1 m2 = MatrixOp.mmult(m1, m2)
let (/) m1 m2 = MatrixOp.pointwiseDivision(m1, m2)
let (+) m1 m2 = MatrixOp.addition(m1, m2)

let ( *^ ) m1 (f:float) = MatrixOp.mmultbyscalar(m1, f)
let (/^) m1 f = MatrixOp.dividebyscalar(m1, f)
let (+^) m1 (f:float) = MatrixOp.addScalar(m1, f)

let withNormalizingFactor (weights:float[]) (varcovar:float[,]) (anticipatedBenchmarkReturn:float) (otherThing:float) : float[] =
        let weightsM = MatrixOp.matrixFrowRow weights
        let impliedReturnsM =
            (
            ((varcovar * (MatrixOp.transpose(weightsM))) *^ (otherThing - anticipatedBenchmarkReturn))
            /^
            (weightsM * varcovar * (MatrixOp.transpose(weightsM))).[0,0]
            ) +^ anticipatedBenchmarkReturn
        impliedReturnsM.[0, *]