module AuxFuncs

open Optimizacion
open System

let ( *! ) m1 m2 = MatrixOp.mmult(m1, m2)
let (/!) m1 m2 = MatrixOp.pointwiseDivision(m1, m2)
let (+!) m1 m2 = MatrixOp.addition(m1, m2)

let ( *^ ) m1 (f:float) = MatrixOp.mmultbyscalar(m1, f)
let (/^) m1 f = MatrixOp.dividebyscalar(m1, f)
let (+^) m1 (f:float) = MatrixOp.addScalar(m1, f)
