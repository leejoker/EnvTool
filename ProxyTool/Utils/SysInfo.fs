namespace ProxyTool.Utils

open System.Runtime.InteropServices

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