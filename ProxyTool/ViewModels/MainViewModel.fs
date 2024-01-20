namespace ProxyTool.ViewModels

open ProxyTool.ViewModels.Utils.CmdUtils
open ProxyTool.ViewModels.Utils.MessageBoxUtils

type MainViewModel() =
    inherit ViewModelBase()

    member this.HandleSetGitProxy(host: string, port: int) =
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
