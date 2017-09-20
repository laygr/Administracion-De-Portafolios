[<AutoOpen>]
module DeedleExtensions
open Deedle
open System
open AdiminstracionDePortafolios
open Optimizacion
open System.IO

type Frame<'TRowKey, 'TColumnKey when 'TRowKey : equality and 'TColumnKey : equality> with
    static member loadDateFrame filePath dateColumn =
        Frame.ReadCsv(path=filePath, hasHeaders=true)
        |> Frame.indexRowsDate dateColumn
        |> Frame.sortRowsByKey
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
        
    static member asArray (frame:Frame<_,_>) =
        (Frame.asMatrix frame).[0,*]

    static member varcovar multiplier frame =
        let m = Frame.asMatrix frame
        MatrixOp.mmultbyscalar(MatrixOp.varcovar(m), multiplier)

    static member rowAsArray rowKey (frame:Frame<_,_>) =
        Frame.asArray (frame.Rows.[rowKey..rowKey])

type Series<'TRowKey, 'TColumnKey when 'TRowKey : equality> with
    static member loadDateSeries filePath dateColumn valuesColumn =
        Frame.loadDateFrame filePath dateColumn
        |> Frame.getCol valuesColumn