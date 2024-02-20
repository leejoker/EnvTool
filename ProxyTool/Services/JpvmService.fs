namespace ProxyTool.Services

open System
open System.IO
open System.IO.Compression
open Newtonsoft.Json.Linq
open ProxyTool.Utils.FileUtils
open ProxyTool.Utils.SysInfo

type JdkVersionInfo = { distro: string; version: string }

module JpvmModule =
    let JPVM_HOME =
        Environment.GetEnvironmentVariable("JPVM_HOME")
        |> fun e ->
            match e with
            | _ when e = null ->
#if Windows
                [| Environment.GetEnvironmentVariable("USERPROFILE"); ".jpvm" |] |> Path.Combine
#else
                [| Environment.GetEnvironmentVariable("HOME"); ".jpvm" |] |> Path.Combine
#endif
            | _ -> e

    let JDK_PATH = [| JPVM_HOME; "jdks" |] |> Path.Combine
    let CACHE_PATH = [| JPVM_HOME; "cache" |] |> Path.Combine
    let JDK_CACHE_PATH = [| CACHE_PATH; "jdks" |] |> Path.Combine
    let VERSION_PATH = [| JDK_PATH; "versions.json" |] |> Path.Combine
    let CUR_VERSION = [| JPVM_HOME; ".jdk_version" |] |> Path.Combine
    let LOG_PATH = [| JPVM_HOME; ".jpvm.log" |] |> Path.Combine

    let VERSION_URL = "https://gitee.com/monkeyNaive/jpvm/raw/master/versions.json"

    let CheckJpvmHome () = CreateDirectory(JPVM_HOME) |> ignore

    let rec DownloadVersionList (progress: IProgress<double>) =
        if Directory.Exists(JDK_PATH) then
            if not (File.Exists(VERSION_PATH)) then
                DownloadFileTask(VERSION_URL, VERSION_PATH, progress) |> Async.RunSynchronously
        else
            CreateDirectory(JDK_PATH) |> ignore
            DownloadVersionList(progress)

    let DownloadCache (jdk: JdkVersionInfo, json: JObject, progress: IProgress<double>) =
        let cachePath = [| JDK_CACHE_PATH; jdk.distro |] |> Path.Combine |> CreateDirectory
        let url = json[jdk.distro][jdk.version][SysOS][SysArch]
        let pathList = url.ToString().Split("/") |> Array.toList
        let packagePath = [| cachePath; pathList[pathList.Length - 1] |] |> Path.Combine
        let (parentDir, _) = SplitFile(packagePath)

        if not (File.Exists(packagePath)) then
            DownloadFileTask(url.ToString(), packagePath, progress)
            |> Async.RunSynchronously

        let packageName = jdk.distro + "-" + jdk.version
        let packageDirPath = [| parentDir; packageName |] |> Path.Combine
        ZipFile.ExtractToDirectory(packagePath, packageDirPath)
        ([| packageDirPath; "bin" |] |> Path.Combine, packageName)

    let Current () =
        CheckJpvmHome()

        if File.Exists(CUR_VERSION) then
            File.ReadAllText(CUR_VERSION)
            |> fun t ->
                t.Split(" ")
                |> fun t ->
                    { JdkVersionInfo.distro = t[0]
                      JdkVersionInfo.version = t[1] }
        else
            { JdkVersionInfo.distro = ""
              JdkVersionInfo.version = "" }

    let Clean () =
        if Directory.Exists(CACHE_PATH) then
            Directory.EnumerateFileSystemEntries(CACHE_PATH)
            |> Seq.toList
            |> List.iter CleanFile
        else
            ()

    let Remove (jdk: JdkVersionInfo) =
        if jdk.distro.Trim() = "" || jdk.version.Trim() = "" then
            false
        else
            let path = [| JDK_PATH; jdk.distro; jdk.version |] |> Path.Combine

            if Directory.Exists(path) then
                CleanFile(path)
                true
            else
                false

    let Install (jdk: JdkVersionInfo, progress: IProgress<double>) =
        DownloadVersionList(null)

        let (pDir, packageName) =
            DownloadCache(jdk, File.ReadAllText(VERSION_PATH) |> JObject.Parse, progress)

        let dirPath =
            [| JDK_PATH; jdk.distro; jdk.version; SysOS; SysArch |]
            |> Path.Combine
            |> CreateDirectory

        Directory.Move(pDir, [| dirPath; packageName |] |> Path.Combine)
