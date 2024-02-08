module Tests

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
    let result = Clean()
    Assert.True(DirectoryIsEmpty(CACHE_PATH))

[<Fact>]
let ``Jpvm Remove Test`` () =
    let result =
        Remove(
            { JdkVersionInfo.distro = "dragonwell"
              JdkVersionInfo.verison = "11" }
        )

    Assert.True(result)

[<Fact>]
let ``SysArch Test`` () = Assert.True(string (SysArch) = "amd64")

[<Fact>]
let ``SysOS Test`` () = Assert.True(string (SysOS) = "windows")
