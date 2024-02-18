namespace ProxyTool.Services

open System
open System.IO
open ProxyTool.Utils.FileUtils

type JdkVersionInfo = { distro: string; verison: string }

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
    let VERSION_PATH = [| JDK_PATH; "versions.json" |] |> Path.Combine
    let CUR_VERSION = [| JPVM_HOME; ".jdk_version" |] |> Path.Combine
    let LOG_PATH = [| JPVM_HOME; ".jpvm.log" |] |> Path.Combine

    let VERSION_URL = "https://gitee.com/monkeyNaive/jpvm/raw/master/versions.json"

    let CheckJpvmHome () = CreateDirectory(JPVM_HOME)

    let rec DownloadVersionList () =
        if Directory.Exists(JDK_PATH) then
            DownloadFileAsync(VERSION_URL, VERSION_PATH, null)
        else
            CreateDirectory(JDK_PATH)
            DownloadVersionList()

    let Current () =
        CheckJpvmHome()

        if File.Exists(CUR_VERSION) then
            File.ReadAllText(CUR_VERSION)
            |> fun t ->
                t.Split(" ")
                |> fun t ->
                    { JdkVersionInfo.distro = t[0]
                      JdkVersionInfo.verison = t[1] }
        else
            { JdkVersionInfo.distro = ""
              JdkVersionInfo.verison = "" }

    let Clean () =
        if Directory.Exists(CACHE_PATH) then
            Directory.EnumerateFileSystemEntries(CACHE_PATH)
            |> Seq.toList
            |> List.iter (fun f -> CleanFile f)
        else
            ()

    let Remove (jdk: JdkVersionInfo) =
        if jdk.distro.Trim() = "" || jdk.verison.Trim() = "" then
            false
        else
            let path = [| JDK_PATH; jdk.distro; jdk.verison |] |> Path.Combine

            if Directory.Exists(path) then
                CleanFile(path)
                true
            else
                false
