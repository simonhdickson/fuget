// run using: fsi FakeExample.fsx
#load "fuget.fsx"
fuget ("FAKE", LatestStable)
#if WIN
#r @"fuget\FAKE\tools\FakeLib.dll" 
#else
#r @"fuget/FAKE/tools/NuGet.Core.dll"
#r @"fuget/FAKE/tools/FakeLib.dll" 
#endif

open Fake

Target "Test" (fun _ ->
    trace "Testing stuff..."
)

Target "Deploy" (fun _ ->
    trace "Heavy deploy action"
)

"Test"            // define the dependencies
   ==> "Deploy"

Run "Deploy"
