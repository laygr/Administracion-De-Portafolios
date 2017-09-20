module Returns
open System
open Deedle

let toArithmeticReturn logRet = Math.Exp(logRet) - 1.0
let toLogarithmic aritRet = Math.Log(aritRet + 1.0)
let inline fromPrices returns = (Frame.diff 1 returns) / returns
let fromValues v0 v1 = v1/v0 - 1.0