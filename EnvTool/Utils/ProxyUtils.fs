namespace EnvTool.Utils

open EnvTool.Utils.SysInfo
open EnvTool.Utils.CmdUtils
#if Windows
open Microsoft.Win32
#endif
#if Linux
open System.IO
#endif

module ProxyUtils =
    
    let defaultWindowsProxyIgnore = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;127.0.0.1;<local>"
    
    let GitProxyEnabled () : bool =
        let gitProxy =
#if Windows            
            RunCmdCommand "git config --global --list | findstr proxy"
#else
            RunCmdCommand "git config --global --list | grep proxy"
#endif
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
        match GetEnvironment "XDG_CURRENT_DESKTOP" with
        | Some(deskType) ->
            if deskType = "KDE" then
                let kioslaverc = Path.Join([|(GetEnvironment "HOME").Value; ".config"; "kioslaverc"|])
                let value = RunCmdCommand $"kreadconfig5 --file {kioslaverc} --group \"Proxy Settings\" --key \"ProxyType\""
                value = "1"
            else
                let value = RunCmdCommand $"gsettings get org.gnome.system.proxy mode"
                value <> "none"
        | None -> false
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
        internetSettings.SetValue("ProxyOverride", defaultWindowsProxyIgnore)
#endif
#if Linux
        let proxyTypes: string array = [|"http"; "https"|]
        match GetEnvironment "XDG_CURRENT_DESKTOP" with
        | Some(deskType) ->
            proxyTypes |> Seq.iter (fun t ->
                    if deskType = "KDE" then
                        let kioslaverc = Path.Join([|(GetEnvironment "HOME").Value; ".config"; "kioslaverc"|])
                        RunCmdCommand $"kwriteconfig5 --file {kioslaverc} --group \"Proxy Settings\" --key \"ProxyType\" 1" |> ignore
                        RunCmdCommand $"kwriteconfig5 --file {kioslaverc} --group \"Proxy Settings\" --key \"{t}Proxy\" {t}://{host}:{port}" |> ignore
                    else
                        RunCmdCommand $"gsettings set org.gnome.system.proxy mode \"manual\"" |> ignore
                        RunCmdCommand $"gsettings set org.gnome.system.proxy.{t} host {host}" |> ignore
                        RunCmdCommand $"gsettings set org.gnome.system.proxy.{t} port {port}" |> ignore
                )
        | None -> ()
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
        match GetEnvironment "XDG_CURRENT_DESKTOP" with
        | Some(deskType) ->
            if deskType = "KDE" then
                let kioslaverc = Path.Join([|(GetEnvironment "HOME").Value; ".config"; "kioslaverc"|])
                RunCmdCommand $"kwriteconfig5 --file {kioslaverc} --group \"Proxy Settings\" --key \"ProxyType\" 0" |> ignore
            else
                RunCmdCommand $"gsettings set org.gnome.system.proxy mode \"none\"" |> ignore
        | None -> ()
#endif
#if OSX
       NetworkDevicesOSX()
        |> Seq.iter (fun dev ->
            RunCmdCommand $"/usr/sbin/networksetup -setwebproxystate {dev} off" |> ignore
            RunCmdCommand $"/usr/sbin/networksetup -setsecurewebproxystate {dev} off" |> ignore
            )
#endif

    let HysteriaProxyEnabled (exec: string): bool =
        GetProcessByPath(exec).IsSome