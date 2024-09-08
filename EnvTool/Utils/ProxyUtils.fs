namespace EnvTool.Utils

open EnvTool.Utils.CmdUtils

module ProxyUtils =
    let GitProxyEnabled () : bool =
        let gitProxy = RunCmdCommand "git config --global --list | findstr proxy"
        gitProxy.Contains "http.proxy" || gitProxy.Contains "https.proxy"

    let SetGitProxy (host: string) (port: int) =
        let gitHttpProxy = $"http://{host}:{port}"
        let gitHttpsProxy = $"https://{host}:{port}"

        RunCmdCommand
            $"git config --global http.proxy {gitHttpProxy} && git config --global https.proxy {gitHttpsProxy}"
        |> ignore

    let RemoveGitProxy () =
        RunCmdCommand "git config --global --unset http.proxy && git config --global --unset https.proxy"
        |> ignore
    
    
#if OSX
    let NetworkDevicesOSX () : string array =
        RunCmdCommand "/usr/sbin/networksetup -listallnetworkservices"
        |> fun s -> s.Split "\n"
        |> Seq.filter (fun s -> s.Contains "*" |> not)
        |> Seq.toArray
#endif

    let SystemProxyStatus () : bool =
#if Windows        
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings")

        let value = internetSettings.GetValue("ProxyEnable")
        value = 1
#endif
#if Linux
#endif
#if OSX
        let enabledProxyDevice = NetworkDevicesOSX()
                                |> Seq.tryFind (fun dev ->
                                                let output = RunCmdCommand $"/usr/sbin/networksetup -getwebproxy {dev}"
                                                let enabledValue = output.Split "\n" |> Seq.tryFind (fun (s:string) -> s.StartsWith "Enabled")
                                                match enabledValue with
                                                | Some(s) -> s.Split ":" |> Seq.item 1 |> _.Trim() = "Yes"
                                                | None -> false)
        match enabledProxyDevice with
        | Some(_) -> true
        | None -> false
#endif

    let SetSystemProxy (host: string) (port: int) =
#if Windows
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings", true)

        internetSettings.SetValue("ProxyEnable", 1)
        internetSettings.SetValue("ProxyServer", $"{host}:{port}")
    // TODO 增加忽略
#endif
#if Linux
#endif
#if OSX
        NetworkDevicesOSX()
        |> Seq.iter (fun dev ->
            RunCmdCommand $"/usr/sbin/networksetup -setwebproxystate {dev} on" |> ignore
            RunCmdCommand $"/usr/sbin/networksetup -setwebproxy {dev} {host} {port}" |> ignore
            RunCmdCommand $"/usr/sbin/networksetup -setsecurewebproxystate {dev} on" |> ignore
            RunCmdCommand $"/usr/sbin/networksetup -setsecurewebproxy {dev} {host} {port}" |> ignore
            )
#endif

    let CloseSystemProxy () =
#if Windows
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings", true)

        internetSettings.SetValue("ProxyEnable", 0)
#endif
#if Linux
#endif
#if OSX
       NetworkDevicesOSX()
        |> Seq.iter (fun dev ->
            RunCmdCommand $"/usr/sbin/networksetup -setwebproxystate {dev} off" |> ignore
            RunCmdCommand $"/usr/sbin/networksetup -setsecurewebproxystate {dev} off" |> ignore
            )
#endif