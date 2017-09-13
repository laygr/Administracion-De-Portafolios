module RatesM
    
    module Return =
        let returnFor x0 xf = xf/x0 - 1.0

    module Compounded =

        let toContinuousRate r m =
            m * log(1.0 + r/m)

    module Continuous =
        let rateFor initialCash endCash years =
            log(endCash / initialCash) / years

        let annualRateForReturn totalReturn years =
            log(totalReturn + 1.0)/years