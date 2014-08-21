#load "fuget.fsx"
fuget ("FSharp.Data", LatestStable)
#r @"fuget\FSharp.Data\lib\net40\FSharp.Data.dll" 

open FSharp.Data

type fuget = JsonProvider<"""{"fuget":"hello world"}""">

printfn "%s" <| fuget.Parse("""{"fuget":"hello world"}""").Fuget
