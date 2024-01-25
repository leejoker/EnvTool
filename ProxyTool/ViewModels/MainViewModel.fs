namespace ProxyTool.ViewModels

open ProxyTool.ViewModels.Utils.CmdUtils
open ProxyTool.ViewModels.Utils.MessageBoxUtils
open ProxyTool.DataModels

type MainViewModel(proxy: ProxyConfigModel) =
    inherit ViewModelBase()

    let _proxy = proxy
    let mutable host = _proxy.Host
    let mutable port = _proxy.Port

    member this.Host
        with get () = host
        and set v = host <- v

    member this.Port
        with get () = port
        and set v = port <- v

    member this.HandleSetGitProxy() =
        let gitHttpProxy = $"http://{host}:{port}"
        let gitHttpsProxy = $"https://{host}:{port}"

        RunCmdCommand
            $"git config --global http.proxy {gitHttpProxy} && git config --global https.proxy {gitHttpsProxy}"
        |> ignore

        CreateTipBox "已设置Git代理"

    member this.HandleUnsetGitProxy() =
        RunCmdCommand "git config --global --unset http.proxy && git config --global --unset https.proxy"
        |> ignore

        CreateTipBox "已解除Git代理"
