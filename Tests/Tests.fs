module Tests

open Xunit
open EnvTool.Services
open EnvTool.Services.JpvmModule
open EnvTool.Utils.FileUtils
open EnvTool.Utils.ProxyUtils
open EnvTool.Utils.SysInfo

[<Fact>]
let ``Jpvm Install Test`` () =
    Install(
        { JdkVersionInfo.distro = "openjdk"
          JdkVersionInfo.version = "21" },
        null
    )

    Assert.True(true)

[<Fact>]
let ``Jpvm Use Test`` () =
    let result =
        Use(
            { JdkVersionInfo.distro = "openjdk"
              JdkVersionInfo.version = "21" }
        )

    Assert.True(result)

[<Fact>]
let ``Jpvm Current Test`` () =
    let result = Current()
    Assert.True(result.distro = "openjdk" && result.version = "21")


[<Fact>]
let ``Jpvm Clean Test`` () =
    Clean()
    Assert.True(DirectoryIsEmpty(CACHE_PATH))

[<Fact>]
let ``Jpvm Remove Test`` () =
    let result =
        Remove(
            { JdkVersionInfo.distro = "openjdk"
              JdkVersionInfo.version = "21" }
        )

    Assert.True(result)

[<Fact>]
let ``DownloadVersionList Test`` () =
    DownloadVersionList(null)
    Assert.True(true)

[<Fact>]
let ``SysArch Test`` () = Assert.True(string (SysArch) = "amd64")

[<Fact>]
let ``SysOS Test`` () = Assert.True(string (SysOS) = "windows")

let ``WalkDir Test`` () =
    let dict = WalkDir(JDK_PATH)
    dict.Keys |> Seq.toList |> List.iter (fun k -> printfn $"%s{k} %s{dict[k]}")

let ``GetEnvironment Test`` () =
    let javaHome = GetEnvironment("JAVA_HOME")

    match javaHome with
    | Some(javaHome) -> printfn $"{javaHome}"
    | None -> printfn "not found"

let ``SystemProxyStatus Test`` () =
    SystemProxyStatus() |> fun b -> printf $"{b}\n"
    
let ``SetSystemProxy Test`` () =
    SetSystemProxy "127.0.0.1" 7890

let ``CloseSystemProxy Test`` () =
    CloseSystemProxy()

let ``HostAddresses Test`` () =
    HostAddresses() |> Seq.iter (fun ip -> printf $"{ip}\n")

let ``NetworkDevicesOSX Test`` () =
    NetworkDevicesOSX() |> Seq.iter (fun d -> printf $"{d}\n")