namespace ProxyTool.ViewModels

open ProxyTool.ViewModels.Utils.CmdUtils
open ProxyTool.ViewModels.Utils.MessageBoxUtils
open ProxyTool.DataModels
open System

type MainViewModel(proxy: ProxyConfigModel) as this =
    inherit ViewModelBase()

    let _proxy = proxy
    let mutable host = _proxy.Host
    let mutable port = _proxy.Port
    let mutable gitProxyEnabled = false

    do this.CheckSysGitSatus()

    member this.GitProxyEnabled
        with get () = gitProxyEnabled
        and set v = gitProxyEnabled <- v

    member this.Host
        with get () = host
        and set v = host <- v

    member this.Port
        with get () = port
        and set v = port <- v

    member this.CheckSysGitSatus() =
        let gitProxy = RunCmdCommand "git config --global --list | findstr proxy"
        gitProxyEnabled <- (gitProxy.Contains "http.proxy" || gitProxy.Contains "https.proxy")

    member this.HandleGitProxy() =
        let gitHttpProxy = $"http://{host}:{port}"
        let gitHttpsProxy = $"https://{host}:{port}"

        if gitProxyEnabled then
            RunCmdCommand
                $"git config --global http.proxy {gitHttpProxy} && git config --global https.proxy {gitHttpsProxy}"
            |> ignore
        else
            RunCmdCommand "git config --global --unset http.proxy && git config --global --unset https.proxy"
            |> ignore
