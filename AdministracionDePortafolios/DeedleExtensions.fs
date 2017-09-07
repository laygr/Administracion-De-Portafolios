[<AutoOpen>]
module DeedleExtensions
open Deedle
open System
open AdiminstracionDePortafolios
open Optimizacion

type Frame<'TRowKey, 'TColumnKey when 'TRowKey : equality and 'TColumnKey : equality> with
    static member indexRowsByDateTime dateColumnName frame =
            let toDateTime dateColumnName (os:ObjectSeries<'TColumnKey>) =
                (os.Get dateColumnName :?> string)
                |> DateTime.Parse
            Frame.indexRowsUsing (toDateTime dateColumnName) frame

    static member asMatrix (frame:Frame<_,_>) = 
        let m = 
            frame.ColumnKeys |> Array.ofSeq
            |> Array.map (fun colname -> Frame.getCol<string,_,float> colname frame)
            |> Array.map (fun serie -> Series.values serie)
            |> Array.map Array.ofSeq
            |> InputInterface.matrixFromArrayOfArrays
        MatrixOp.transpose(m)
        