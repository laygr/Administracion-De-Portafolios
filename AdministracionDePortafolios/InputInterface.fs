namespace AdiminstracionDePortafolios
module InputInterface =
    open FSharp.Data

    let matrixFromArrayOfArrays (arrOfArrs:float[][]) =
        let rows = arrOfArrs.GetLength 0
        let columns = arrOfArrs.[0].GetLength 0

        let m = Array2D.zeroCreate rows columns
        [0..rows-1]
        |> Seq.iter(fun r -> [0..columns-1] |> Seq.iter(fun c -> m.[r,c] <- arrOfArrs.[r].[c]))
        m
    let loadMatrix fileName hasHeaders =
        let s = CsvFile.Load(uri=fileName,hasHeaders=hasHeaders)
        s.Rows |> Array.ofSeq
        |> Array.map (fun (row:CsvRow) -> Array.map float row.Columns) 
        |> matrixFromArrayOfArrays

    let loadRow fileName hasHeaders =
        let s = CsvFile.Load(uri=fileName,hasHeaders=hasHeaders)
        let row = s.Rows |> Array.ofSeq |> Array.head
        Array.map float row.Columns

    let loadRowOfStrings fileName hasHeaders =
        let s = CsvFile.Load(uri=fileName,hasHeaders=hasHeaders)
        (s.Rows |> Array.ofSeq |> Array.head).Columns