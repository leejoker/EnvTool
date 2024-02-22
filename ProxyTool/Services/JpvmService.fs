namespace ProxyTool.Services

open System
open System.IO
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

    //------------------------- private functions ---------------------------------------------
    let private CheckJpvmHome () = CreateDirectory(JPVM_HOME) |> ignore

    let private DownloadCache (jdk: JdkVersionInfo, json: JObject, progress: IProgress<double>) =
        let cachePath = [| JDK_CACHE_PATH; jdk.distro |] |> Path.Combine |> CreateDirectory
        let url = json[jdk.distro][jdk.version][SysOS][SysArch]

        if url.Type = JTokenType.Null then
            raise (Exception("version not found"))

        let pathList = url.ToString().Split("/") |> Array.toList
        let packagePath = [| cachePath; pathList[pathList.Length - 1] |] |> Path.Combine
        let (parentDir, _) = SplitFile(packagePath)

        if not (File.Exists(packagePath)) then
            DownloadFileTask(url.ToString(), packagePath, progress)
            |> Async.RunSynchronously

        let packageName = jdk.distro + "-" + jdk.version
        let packageDirPath = [| parentDir; packageName |] |> Path.Combine
        DeCompressFile(packagePath, packageDirPath)

        let binPath =
            WalkDir(packageDirPath).Keys
            |> Seq.toList
            |> List.tryFind (fun k -> k.EndsWith("bin"))
            |> fun s ->
                match s with
                | Some(s) -> s
                | None -> raise (Exception("JDK bin path not found"))

        let (binPathParent, _) = SplitFile(binPath)
        (binPathParent, packageName)

    let private SetJavaEnvironment (javaHome: string) =
        let originJavaHome = GetEnviromnent("JAVA_HOME")
        let result = SetEnvironmentVariable("JAVA_HOME", javaHome)

        match originJavaHome with
        | Some(originJavaHome) ->
            //TODO let path = GetEnviromnent("PATH")
            false
        | None ->
            if result then
                AddPathValue(javaHome, null)
            else
                raise (Exception("Set JAVA_HOME Failed"))


    //------------------------- public functions ---------------------------------------------
    let rec DownloadVersionList (progress: IProgress<double>) =
        if Directory.Exists(JDK_PATH) then
            DownloadFileTask(VERSION_URL, VERSION_PATH, progress) |> Async.RunSynchronously
        else
            CreateDirectory(JDK_PATH) |> ignore
            DownloadVersionList(progress)

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

        CleanDir([| JDK_CACHE_PATH; jdk.distro |] |> Path.Combine)

        let (pDir, packageName) =
            DownloadCache(jdk, File.ReadAllText(VERSION_PATH) |> JObject.Parse, progress)

        let dirPath =
            [| JDK_PATH; jdk.distro; jdk.version; SysOS; SysArch; packageName |]
            |> Path.Combine
            |> CreateDirectory

        if pDir <> null then
            if Directory.Exists(dirPath) then
                CleanFile(dirPath)

            Directory.Move(pDir, dirPath)

    let Use (jdk: JdkVersionInfo) =
        let packagePath =
            [| JDK_PATH
               jdk.distro
               jdk.version
               SysOS
               SysArch
               jdk.distro + "-" + jdk.version |]
            |> Path.Combine

        if Directory.Exists(packagePath) then true else false
