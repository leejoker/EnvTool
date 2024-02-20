module Tests

open System
open System.Collections.Generic
open Xunit
open ProxyTool.Services
open ProxyTool.Services.JpvmModule
open ProxyTool.Utils.FileUtils
open ProxyTool.Utils.SysInfo

[<Fact>]
let ``Jpvm Current Test`` () =
    let result = Current()
    Assert.True(result.distro = "zulu")


[<Fact>]
let ``Jpvm Clean Test`` () =
    Clean()
    Assert.True(DirectoryIsEmpty(CACHE_PATH))

[<Fact>]
let ``Jpvm Remove Test`` () =
    let result =
        Remove(
            { JdkVersionInfo.distro = "dragonwell"
              JdkVersionInfo.version = "11" }
        )

    Assert.True(result)

[<Fact>]
let ``Jpvm Install Test`` () =
    Install(
        { JdkVersionInfo.distro = "openjdk"
          JdkVersionInfo.version = "21" },
        null
    )

    Assert.True(true)

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
    dict.Keys |> Seq.toList |> List.iter (fun k -> Console.WriteLine(k + " " + dict[k]))
    Assert.True(true)