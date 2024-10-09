namespace EnvTool.Services

open System
open System.Diagnostics
open EnvTool.DataModels
open EnvTool.Utils.ProxyUtils
open EnvTool.Utils.SysInfo

type GlobalStatement() =

    let appName = "EnvTool"

    let proxyConfigService = ProxyConfigService()

    let mutable globalProxyConfigModel: ProxyConfigModel =
        Unchecked.defaultof<ProxyConfigModel>

    let mutable globalHysteriaProcess: Option<Process> = None

    let mutable bootUpEnabled: bool = Unchecked.defaultof<bool>

    let mutable gitProxyEnabled: bool = Unchecked.defaultof<bool>

    let mutable systemProxyEnabled: bool = Unchecked.defaultof<bool>

    let mutable hysteriaProxyEnabled: bool = Unchecked.defaultof<bool>

    let mutable hysteriaEnabled: bool = Unchecked.defaultof<bool>

    member this.InitGlobalStatement() =
        if globalProxyConfigModel = Unchecked.defaultof<ProxyConfigModel> then
            globalProxyConfigModel <- proxyConfigService.LoadConfig()

        bootUpEnabled <- GetApplicationBootUp appName
        gitProxyEnabled <- GitProxyEnabled()
        systemProxyEnabled <- SystemProxyStatus()
        hysteriaEnabled <- globalProxyConfigModel.HysteriaEnabled = "true"

        if String.IsNullOrWhiteSpace(globalProxyConfigModel.HysteriaExec) |> not then
            hysteriaProxyEnabled <- HysteriaProxyEnabled(globalProxyConfigModel.HysteriaExec)

    member this.AppName = appName

    member this.GlobalProxyConfigModel
        with get () = globalProxyConfigModel
        and set v = globalProxyConfigModel <- v

    member this.GlobalHysteriaProcess
        with get () = globalHysteriaProcess
        and set v = globalHysteriaProcess <- v

    member this.BootUpEnabled
        with get () = bootUpEnabled
        and set v = bootUpEnabled <- v

    member this.GitProxyEnabled
        with get () = gitProxyEnabled
        and set v = gitProxyEnabled <- v

    member this.SystemProxyEnabled
        with get () = systemProxyEnabled
        and set v = systemProxyEnabled <- v

    member this.HysteriaProxyEnabled
        with get () = hysteriaProxyEnabled
        and set v = hysteriaProxyEnabled <- v

    member this.HysteriaEnabled
        with get () = hysteriaEnabled
        and set v = hysteriaEnabled <- v


    member this.SaveProxyConfig() =
        proxyConfigService.SaveConfig(globalProxyConfigModel)
