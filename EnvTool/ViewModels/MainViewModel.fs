namespace EnvTool.ViewModels

open EnvTool.Services
open EnvTool.Utils.ProxyUtils
open EnvTool.Utils.SysInfo
open EnvTool.Services.HysteriaService

type MainViewModel() =
    inherit ViewModelBase()

    let mutable globalStatement = Unchecked.defaultof<GlobalStatement>

    new(globalStatement: GlobalStatement) as this =
        MainViewModel()
        then this.GlobalStatement <- globalStatement

    member this.GlobalStatement
        with get () = globalStatement
        and set v = globalStatement <- v

    member this.GitProxyEnabled
        with get () = globalStatement.GitProxyEnabled
        and set v = globalStatement.GitProxyEnabled <- v

    member this.BootUpEnabled
        with get () = globalStatement.BootUpEnabled
        and set v = globalStatement.BootUpEnabled <- v

    member this.SystemProxyEnabled
        with get () = globalStatement.SystemProxyEnabled
        and set v = globalStatement.SystemProxyEnabled <- v

    member this.HysteriaProxyEnabled
        with get () = globalStatement.HysteriaProxyEnabled
        and set v = globalStatement.HysteriaProxyEnabled <- v

    member this.HysteriaEnabled
        with get () = globalStatement.HysteriaEnabled
        and set v = globalStatement.HysteriaEnabled <- v

    member this.HandleBootUp() =
        if globalStatement.BootUpEnabled then
            SetApplicationBootUp globalStatement.AppName ($"{CurrentWorkDir()}{CurrentExeName()}")
        else
            RemoveApplicationBootUp globalStatement.AppName

    member this.HandleSystemProxy() =
        if globalStatement.SystemProxyEnabled then
            SetSystemProxy globalStatement.GlobalProxyConfigModel.Host globalStatement.GlobalProxyConfigModel.Port
        else
            CloseSystemProxy()

    member this.HandleGitProxy() =
        if globalStatement.GitProxyEnabled then
            SetGitProxy globalStatement.GlobalProxyConfigModel.Host globalStatement.GlobalProxyConfigModel.Port
        else
            RemoveGitProxy()

    member this.HandleHysteriaProxy() =
        if globalStatement.HysteriaProxyEnabled then
            let result, proc =
                StartHysteriaProcess
                    globalStatement.GlobalProxyConfigModel.HysteriaExec
                    globalStatement.GlobalProxyConfigModel.HysteriaConfig

            if result then
                globalStatement.GlobalHysteriaProcess <- proc
        else
            KillHysteriaProcess
                globalStatement.GlobalHysteriaProcess
                globalStatement.GlobalProxyConfigModel.HysteriaExec
