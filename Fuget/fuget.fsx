[<AutoOpen>]
module Fuget
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

module FugetInternal =
    let isUnix = Environment.OSVersion.Platform = PlatformID.Unix
    let isMacOS = Environment.OSVersion.Platform = PlatformID.MacOSX
    let isLinux = int System.Environment.OSVersion.Platform |> fun p -> (p = 4) || (p = 6) || (p = 128)
    let isMono = isLinux || isUnix || isMacOS

    let inline (++) left right = Path.Combine(left, right)

    let fugetFolder = (__SOURCE_DIRECTORY__ ++ "fuget")

    let downloadString (url:string) =
        use client = new WebClient()
        HttpUtility.UrlPathEncode url |> client.DownloadString
        
    let downloadFile destination (url:Uri) =
        use client = new WebClient()
        client.DownloadFile(url, destination)
        
    let getNugetPackage id = function
        | Version version -> sprintf "https://www.nuget.org/api/v2/Packages(Id='%s',Version='%s')" id version
        | Latest          -> sprintf "https://www.nuget.org/api/v2/Packages()?$filter=Id eq '%s' and IsLatestVersion" id

    let extract zip destination =
        if isMono then
            let zipProcess = System.Diagnostics.Process.Start("unzip", sprintf" -d %s -q %s" destination zip)
            zipProcess.WaitForExit()
        else System.IO.Compression.ZipFile.ExtractToDirectory(zip, destination)

    let parsePackageUri xml =
        let doc = XDocument.Parse xml in
            doc.Descendants().Elements()
            |> Seq.filter(fun  i -> i.Name.LocalName = "content")
            |> Seq.map (fun i -> i.Attribute(XName.Get "src").Value)
            |> Seq.exactlyOne

    let parseDependencies xml =
        let doc = XDocument.Parse xml in
            doc.Descendants().Elements()
            |> Seq.filter(fun i -> i.Name.LocalName = "Dependencies")
            |> Seq.map (fun i -> i.Value.Split([|':'|]) |> Array.toList)
            |> Seq.choose (function id :: version :: _ -> Some (id, Version version) | _ -> None)

open FugetInternal

let fulibs packages =
    let toRelativePath (file:string) = file.Remove(0, __SOURCE_DIRECTORY__.Length + 1)
    
    let (|FolderExists|_|) lib root =
        let directory = root ++ lib
        let files = lazy (Directory.EnumerateFiles(directory, "*.dll"))
        if Directory.Exists directory && Seq.length files.Value <> 0 then Some files.Value else None

    let getLibs = function
        | FolderExists ((++)"lib" "net451") files
        | FolderExists ((++)"lib" "net45")  files
        | FolderExists ((++)"lib" "net40")  files
        | FolderExists ((++)"lib" "net35")  files
        | FolderExists "lib"                files -> files
        | _ -> Seq.empty

    let printLibs libs =
        printfn "Evaluate or add these to your script file:"
        libs |> Seq.map toRelativePath |> Seq.iter (printfn """#r @"%s" """)

    packages
    |> Seq.collect (fun package -> getLibs (fugetFolder ++ package))
    |> printLibs

let fuget (packageName, version) =
    let unpack name folder (url:string) =
        let destination = folder ++ name
        let nupkg = folder ++ name + ".nupkg"
        if Directory.Exists destination then Directory.Delete (destination, true)
        Directory.CreateDirectory destination |> ignore
        downloadFile nupkg (Uri url)
        extract nupkg destination

    let rec allPackages packageName version =
        seq {
            let dependencies =
                getNugetPackage packageName version
                |> downloadString
                |> parseDependencies
            yield packageName, version
            yield! dependencies |> Seq.collect (fun (i,_) -> allPackages i Latest)
        }
    let packages = allPackages packageName version

    packages
    |> Seq.map (fun (id, version) -> getNugetPackage id version)
    |> Seq.map downloadString
    |> Seq.map parsePackageUri
    |> Seq.iter (unpack packageName fugetFolder)

    fulibs (Seq.map fst packages)

let fulist() =
    Directory.EnumerateDirectories(fugetFolder)
    |> Seq.map (fun i -> DirectoryInfo i)
    |> Seq.map (fun i -> i.Name)
    |> Seq.toList
    |> function | []       -> printfn "No package installed"
                | packages -> printfn "Folllowing packages installed:"
                              packages |> List.iter (printfn "%s")

let fudelete packageName =
    let packageFolder = fugetFolder ++ packageName
    if Directory.Exists (packageFolder) then
        try
            Directory.Delete(packageFolder, true)
            File.Delete(packageFolder + ".nupkg")
            printfn "Package '%s' deleted" packageName
        with
            | :? IOException -> printfn "Couldn't delete package '%s', please try resetting the interactive session" packageName
    else
        printfn "Package '%s' is not installed" packageName

let fufind id =
    let find id =
        sprintf "https://www.nuget.org/api/v2/Packages()?$filter=substringof('%s',Id) and IsLatestVersion&$top=10" id
        |> downloadString

    let getNames xml =
        let doc = XDocument.Parse xml in
            doc.Descendants().Elements()
            |> Seq.filter(fun  i -> i.Name.LocalName = "title")
            |> Seq.map (fun i -> i.Value)
            |> Seq.filter (fun i -> i <> "Packages")
            |> List.ofSeq

    let printNames id = function
        | [] -> printfn "No results found for '%s'" id
        | names -> printfn "Found the following packages:"
                   names |> List.iter (printfn "%s")

    find id
    |> getNames
    |> printNames id

let fupdate () =
    downloadFile "fuget.fsx" <| Uri "https://raw.githubusercontent.com/simonhdickson/fuget/master/Fuget/fuget.fsx"
    printfn """fuget updated, please run: #load "fuget.fsx" """

let fuhelp () =
    printfn "The following commands available:"
    printfn "fuget packageName    - install package"
    printfn "fudelete packageName - delete package"
    printfn "fulibs packageName   - prints all includes needed to use package"
    printfn "fufind id            - searches for packages names containing a string"
    printfn "fulist ()            - list installed packages"
    printfn "fupdate ()           - downloads the latest version of fuget"
