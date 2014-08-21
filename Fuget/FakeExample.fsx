﻿// run using: fsi FakeExample.fsx
// or fsharpi FakeExample.fsx on Mono
#load "fuget.fsx"
fuget ("FAKE", LatestStable)
#r @"fuget\FAKE\tools\FakeLib.dll" 

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
