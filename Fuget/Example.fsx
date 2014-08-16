#load "fuget.fsx"
open Fuget
fuget "FSharp.Data" Latest
#r @"fuget\FSharp.Data\lib\net40\FSharp.Data.dll" 

open FSharp.Data

type fuget = JsonProvider<"""{"fuget":"hello world"}""">

printfn "%s" <| fuget.GetSample().Fuget
