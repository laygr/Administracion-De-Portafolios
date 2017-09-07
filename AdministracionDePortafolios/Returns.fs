module Returns
open System

let toArithmeticReturn logRet = Math.Exp(logRet) - 1.0
let toLogarithmic aritRet = Math.Log(aritRet + 1.0)