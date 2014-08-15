#r "System.IO.Compression.FileSystem"
#r "System.Xml.Linq.dll"
open System.Xml
open System.Xml.Linq
open System.Linq
open System

type Version =
    | Latest
    | Version of string

let unpack file destination =
    System.IO.Compression.ZipFile.ExtractToDirectory(file, destination)

let parseFileAddress xml =
    let doc = XDocument.Parse xml in
        doc.Descendants()
           .Elements().Where(fun  i -> i.Name.LocalName = "content")
        |> Seq.map (fun i -> i.Attribute(XName.Get "src"))
        |> Seq.map (fun i -> i.Value)
        |> Seq.exactlyOne

let getPackage package version =
    match version with
    | Version version -> sprintf "https://www.nuget.org/api/v2/Packages(Id='%s',Version='%s')" package version
    | Latest          -> sprintf "https://www.nuget.org/api/v2/Packages()?$filter=Id eq '%s' and IsLatestVersion" package

let fuget =
    getPackage

fuget "FSharp.Data" Latest
