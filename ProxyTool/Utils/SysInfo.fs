﻿namespace ProxyTool.Utils

open System.IO
open System.Runtime.InteropServices
open System
open Microsoft.Win32

module SysInfo =
    let SysArch = (fun () ->
                        let arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower()
                        if arch = "arm64" then "aarch64"
                        else if arch = "x64" then "amd64"
                        else arch)()
    let SysOS = 
#if Windows
        "windows"
#endif

#if Linux
        "linux"
#endif

#if OSX
        "macos"
#endif

    let SetUserEnvironmentVariable name value =
#if Windows
        Registry.CurrentUser |> _.OpenSubKey("Environment", true) |> _.SetValue(name, value)
        true
#else 
        false
#endif

    let GetEnvironment name =
        match Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) with
        | null -> None
        | value -> Some value

    let AddPathValue value origin =
#if Windows
        let originPath = GetEnvironment("PATH")
        match originPath with
        |Some(originPath) ->
                let o = ref originPath
                if origin <> null then
                       if o.Value.StartsWith origin then
                           o.Value <- (o.Value).Replace($"{origin};","")
                       else
                           o.Value <- (o.Value).Replace($";{origin}","")
                SetUserEnvironmentVariable "PATH" $"{value}{Path.DirectorySeparatorChar}bin;{o.Value}"
        | None -> false
#else
        false
#endif      