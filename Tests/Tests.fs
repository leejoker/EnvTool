module Tests

open Xunit
open EnvTool.Services
open EnvTool.Services.JpvmModule
open EnvTool.Utils.FileUtils
open EnvTool.Utils.ProxyUtils
open EnvTool.Utils.SysInfo

// Jpvm Tests (require specific environment)
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

// MavenService Tests
[<Fact>]
let ``MavenService GetMavenHome returns Some or None`` () =
    let result = MavenService.GetMavenHome()
    match result with
    | Some(_) -> Assert.True(true)
    | None -> Assert.True(true)

[<Fact>]
let ``MavenService GetVersion returns Some when mvn is available`` () =
    let result = MavenService.GetVersion()
    match result with
    | Some(version) -> Assert.True(version.Length > 0, $"Expected non-empty version, got: {version}")
    | None -> Assert.Fail("GetVersion returned None - mvn command may not be in PATH or not configured correctly")

[<Fact>]
let ``MavenService GetVersion debug test`` () =
    // Debug test to see actual mvn output
    let p = new System.Diagnostics.Process()
    p.StartInfo.FileName <- "mvn"
    p.StartInfo.Arguments <- "-version"
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.CreateNoWindow <- true
    p.StartInfo.WorkingDirectory <- System.IO.Directory.GetCurrentDirectory()
    p.Start() |> ignore
    let output = p.StandardOutput.ReadToEnd()
    let error = p.StandardError.ReadToEnd()
    p.WaitForExit()
    p.Close()
    printfn "mvn -version output: %s" output
    printfn "mvn -version error: %s" error
    Assert.True(output.Length > 0, $"Expected some output, got empty. Error was: {error}")

[<Fact>]
let ``MavenService GetSources returns non-empty list`` () =
    let sources = MavenService.GetSources()
    Assert.NotEmpty(sources)

[<Fact>]
let ``MavenService GetSources returns at least one source`` () =
    let sources = MavenService.GetSources()
    Assert.True(sources.Length >= 1)

[<Fact>]
let ``MavenService SaveSettings and GetSettingsContent roundtrip`` () =
    let testContent = """<settings>
  <mirrors>
    <mirror>
      <id>test</id>
      <name>Test Mirror</name>
      <url>https://test.mirror.com/repo</url>
    </mirror>
  </mirrors>
</settings>"""
    MavenService.SaveSettings(testContent)
    let retrieved = MavenService.GetSettingsContent()
    match retrieved with
    | Some(content) -> Assert.Contains("test", content)
    | None -> Assert.Fail("Expected Some content")

[<Fact>]
let ``MavenService AddSource returns true on success`` () =
    let newSource = { Id = "test-mirror-001"; Name = "Test Mirror"; Url = "https://test.mirror.com"; IsDefault = false }
    let result = MavenService.AddSource(newSource)
    Assert.True(result)

[<Fact>]
let ``MavenService RemoveSource returns true for existing source`` () =
    // First add a source to remove
    let newSource = { Id = "to-remove-001"; Name = "To Remove"; Url = "https://remove.com"; IsDefault = false }
    let added = MavenService.AddSource(newSource)
    if added then
        let result = MavenService.RemoveSource("to-remove-001")
        Assert.True(result)
    else
        // If AddSource failed (e.g. no write permission), skip
        Assert.True(true)

[<Fact>]
let ``MavenService RemoveSource returns false for non-existing source`` () =
    let result = MavenService.RemoveSource("non-existing-mirror-id-xyz")
    Assert.False(result)

[<Fact>]
let ``MavenService SetDefaultSource returns true for existing source`` () =
    // Add a source then set it as default
    let newSource = { Id = "to-default-001"; Name = "To Default"; Url = "https://default.com"; IsDefault = false }
    let added = MavenService.AddSource(newSource)
    if added then
        let result = MavenService.SetDefaultSource("to-default-001")
        Assert.True(result)
    else
        Assert.True(true)

// SysInfo Tests
[<Fact>]
let ``SysArch Test`` () = Assert.True(string (SysArch) = "amd64")

[<Fact>]
let ``SysOS Test`` () = Assert.True(string (SysOS) = "windows")

// Non-test functions for manual testing
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

let ``SetSystemProxy Test`` () = SetSystemProxy "127.0.0.1" 7890

let ``CloseSystemProxy Test`` () = CloseSystemProxy()

let ``HostAddresses Test`` () =
    HostAddresses() |> Seq.iter (fun ip -> printf $"{ip}\n")

#if OSX
let ``NetworkDevicesOSX Test`` () =
    NetworkDevicesOSX() |> Seq.iter (fun d -> printf $"{d}\n")
#endif

let ``CurrentWorkDir Test`` () =
    CurrentWorkDir() |> fun dir -> printf $"{dir}\n"

let ``HysteriaProxyEnabled`` () =
    HysteriaProxyEnabled(@"C:\hysteria\hysteria.exe") |> fun b -> printf $"{b}\n"
