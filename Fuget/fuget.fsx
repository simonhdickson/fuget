#r "System.IO.Compression.FileSystem"
#r "System.Xml.Linq.dll"
#r "System.Web.dll"
open System
open System.IO
open System.Net
open System.Xml
open System.Xml.Linq
open System.Web

type Version = Latest | Version of string

let downloadFile destination (url:Uri) =
    use client = new WebClient()
    client.DownloadFile(url, destination)

let downloadString (url:string) =
    use client = new WebClient()
    HttpUtility.UrlPathEncode url
    |> client.DownloadString

let unpack name folder (url:string) =
    let destination = Path.Combine(folder, name)
    let nupkg = Path.Combine(destination, name + ".nupkg")
    if Directory.Exists destination then Directory.Delete (destination, true)
    Directory.CreateDirectory destination |> ignore
    downloadFile nupkg (Uri url)
    System.IO.Compression.ZipFile.ExtractToDirectory(nupkg, destination)
    destination

let (|FolderExists|_|) lib root =
    let directory = Path.Combine(root, lib)
    let files = lazy (Directory.EnumerateFiles(directory, "*.dll"))
    if Directory.Exists directory && Seq.length files.Value <> 0 then Some files.Value else None

let toRelativePath (file:string) =
    file.Remove(0, __SOURCE_DIRECTORY__.Length + 1)

let getLibs = function
    | FolderExists @"lib\net451" files
    | FolderExists @"lib\net45"  files
    | FolderExists @"lib\net40"  files
    | FolderExists @"lib\net35"  files
    | FolderExists @"lib"        files -> files
    | _ -> failwith "supported nuget package"

let printLibs libs =
    printfn "Add these to the top of your script file:"
    libs
    |> Seq.map toRelativePath
    |> Seq.iter (printfn """#r @"%s" """)

let getPackageUri xml =
    let doc = XDocument.Parse xml in
        doc.Descendants().Elements()
        |> Seq.filter(fun  i -> i.Name.LocalName = "content")
        |> Seq.map (fun i -> i.Attribute(XName.Get "src"))
        |> Seq.map (fun i -> i.Value)
        |> Seq.exactlyOne

let getPackage id = function
    | Version version -> sprintf "https://www.nuget.org/api/v2/Packages(Id='%s',Version='%s')" id version
    | Latest          -> sprintf "https://www.nuget.org/api/v2/Packages()?$filter=Id eq '%s' and IsLatestVersion" id

let fuget packageName version =
    getPackage packageName version
    |> downloadString
    |> getPackageUri
    |> unpack packageName (__SOURCE_DIRECTORY__ + "\\fuget")
    |> getLibs
    |> printLibs
