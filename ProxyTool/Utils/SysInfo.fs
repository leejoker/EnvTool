namespace ProxyTool.Utils

open System.Runtime.InteropServices
open System

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

#if Windows
    [<DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool SetEnvironmentVariable(string lpName, string lpValue)
    [<DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern IntPtr GetEnvironmentVariable(string lpName, Text.StringBuilder lpBuffer, int nSize)
#endif

    let GetEnviromnent name =
#if Windows
        let mutable buffer = Text.StringBuilder(10240)
        let length = GetEnvironmentVariable(name, buffer, buffer.Capacity)
        if length <> IntPtr.Zero then
                Some (buffer.ToString())
        else
                None
#else
        match Environment.GetEnvironmentVariable(name) with
        | null -> None
        | value -> Some value
#endif

    let SetSystemEnviromentVariable name value =
#if Windows
        SetEnvironmentVariable(name, value)
#endif

#if Linux
        false
#endif

#if OSX
        false
#endif

    let AddPathValue(value: string, origin: string) =
#if Windows
        let originPath = GetEnviromnent("PATH")
        match originPath with
        |Some(originPath) ->
                if origin = null then
                    SetSystemEnviromentVariable "PATH" $"{value};{originPath}"
                else
                    //TODO
                    false
        | None -> false
#endif

#if Linux
        false
#endif

#if OSX
        false
#endif      