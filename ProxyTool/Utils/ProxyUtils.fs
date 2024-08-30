namespace ProxyTool.Utils

open Microsoft.Win32
open ProxyTool.Utils.CmdUtils

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

    let SystemProxyStatus () : bool =
#if Windows        
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings")

        let value = internetSettings.GetValue("ProxyEnable")
        value = 1
#endif

    let SetSystemProxy (host: string) (port: int) =
#if Windows
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings", true)

        internetSettings.SetValue("ProxyEnable", 1)
        internetSettings.SetValue("ProxyServer", $"{host}:{port}")
    // TODO 增加忽略
#endif

    let CloseSystemProxy () =
#if Windows
        let internetSettings =
            Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings", true)

        internetSettings.SetValue("ProxyEnable", 0)
#endif
