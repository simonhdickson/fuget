#r @"fuget\FSharp.Data\lib\net40\FSharp.Data.DesignTime.dll" 
#r @"fuget\FSharp.Data\lib\net40\FSharp.Data.dll" 
#load "fuget.fsx"
fuget "FSharp.Data" Latest

open FSharp.Data
type fuget = JsonProvider<"""{"fuget":"hello world"}""">
