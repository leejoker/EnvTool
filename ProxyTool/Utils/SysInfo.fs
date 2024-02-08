namespace ProxyTool.Utils

open System
open System.Runtime.InteropServices

module SysInfo =
    let SysArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").ToLower()
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