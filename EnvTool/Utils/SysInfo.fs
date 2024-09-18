namespace EnvTool.Utils

open System.IO
open System.Net
open System.Net.NetworkInformation
open System.Runtime.InteropServices
open System
open Microsoft.FSharp.Collections
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

#if Linux || OSX
    let GetUserProfile =
        let bashProfile = ".bash_profile"
        let bashrc = ".bashrc"
        let zshrc = ".zshrc"
        let home = Environment.GetEnvironmentVariable("HOME")

        if File.Exists $"{home}{Path.DirectorySeparatorChar}{zshrc}" then
            $"{home}{Path.DirectorySeparatorChar}{zshrc}"
        else if File.Exists $"{home}{Path.DirectorySeparatorChar}{bashrc}" then
            $"{home}{Path.DirectorySeparatorChar}{bashrc}"
        else
            $"{home}{Path.DirectorySeparatorChar}{bashProfile}"
#endif

    let SetUserEnvironmentVariable name value =
#if Windows
        Registry.CurrentUser |> _.OpenSubKey("Environment", true) |> _.SetValue(name, value)
        true
#else
        let profile = GetUserProfile
        let lines = ref (File.ReadAllLines profile)
        lines.Value |> Array.tryFindIndex (_.StartsWith($"export {name}"))
        |> fun index ->
            match index with
            | Some(index) -> lines.Value[index] <- $"export {name}={value}"
            | None -> lines.Value <- List.toArray ($"export {name}={value}"::(Array.toList lines.Value))
        File.WriteAllLines(profile, lines.Value)
        true
#endif

    let GetEnvironment name =
#if Windows
        match Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) with
        | null -> None
        | value -> Some value
#else
        let profile = GetUserProfile
        let lines = File.ReadAllLines profile
        lines |> Array.tryFind (_.StartsWith($"export {name}"))
        |> fun opt ->
            match opt with
            | Some(opt) -> opt.Split("=") |> Array.tryItem 1
            | None -> None
#endif

    let AddPathValue value origin =
#if Windows
        let originPath = GetEnvironment("PATH")
        match originPath with
        |Some(originPath) ->
                let o = ref originPath
                if origin <> null then
                       if o.Value.StartsWith origin then
                           o.Value <- (o.Value).Replace($"{origin}{Path.DirectorySeparatorChar}bin;","")
                       else
                           o.Value <- (o.Value).Replace($";{origin}{Path.DirectorySeparatorChar}bin","")
                SetUserEnvironmentVariable "PATH" $"{value}{Path.DirectorySeparatorChar}bin;{o.Value}"
        | None -> SetUserEnvironmentVariable "PATH" $"{value}{Path. DirectorySeparatorChar}bin"
#else
        let originPath = GetEnvironment("PATH")
        match originPath with
        |Some(originPath) ->
                let o = ref originPath
                if origin <> null then
                       if o.Value.StartsWith origin then
                           o.Value <- (o.Value).Replace($"{origin}{Path.DirectorySeparatorChar}bin:","")
                       else
                           o.Value <- (o.Value).Replace($":{origin}{Path.DirectorySeparatorChar}bin","")
                SetUserEnvironmentVariable "PATH" $"{value}{Path.DirectorySeparatorChar}bin:{o.Value}"
        | None -> SetUserEnvironmentVariable "PATH" $"{value}{Path.DirectorySeparatorChar}bin:$PATH"
#endif

    let IPAddresses () =
        let hostName = Dns.GetHostName()
        let ipAddresses = Dns.GetHostAddresses(hostName)
        
        ipAddresses
        |> Seq.filter (fun ip -> ip.AddressFamily.ToString() = "InterNetwork")
        |> Seq.map _.ToString() |> Seq.toList
    
    let DNSAddresses () =
        NetworkInterface.GetAllNetworkInterfaces()
        |> Seq.filter (fun ni -> ni.NetworkInterfaceType = NetworkInterfaceType.Ethernet)
        |> Seq.map (_.GetIPProperties().DnsAddresses)
        |> Seq.concat
        |> Seq.filter (fun ip -> ip.AddressFamily.ToString() = "InterNetwork")
        |> Seq.map _.ToString() |> Seq.toList

    let HostAddresses () =
        IPAddresses() @ DNSAddresses()
        
    let CurrentWorkDir () =
        AppDomain.CurrentDomain.BaseDirectory