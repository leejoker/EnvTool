namespace EnvTool.ViewModels

open ReactiveUI
open EnvTool.Services
open EnvTool.DataModels
open EnvTool.ViewModels.Utils.MessageBoxUtils
open System

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable _mainViewModel = Unchecked.defaultof<MainViewModel>

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    let mutable proxyConfigModel: ProxyConfigModel =
        Unchecked.defaultof<ProxyConfigModel>

    let mutable hysteriaProcess = None

    let proxyConfigService = ProxyConfigService()

    do
        if proxyConfigModel = Unchecked.defaultof<ProxyConfigModel> then
            proxyConfigModel <- proxyConfigService.LoadConfig()

    do this.ContentViewModel <- this.InitMainView proxyConfigModel

    member private this.InitProxyConfigByView(proxyConfigView: ProxyConfigViewModel) =
        let proxyConfig = ProxyConfigModel(proxyConfigView.Host, proxyConfigView.Port)

        if proxyConfigView.HysteriaEnabled then
            proxyConfig.HysteriaEnabled <- "true"
            proxyConfig.HysteriaExec <- proxyConfigView.HysteriaExec
            proxyConfig.HysteriaConfig <- proxyConfigView.HysteriaConfig

        this.ProxyConfigModel <- proxyConfig

    member private this.InitMainView(proxyConfig: ProxyConfigModel) : MainViewModel =
        let mainViewParam = MainViewModel()
        mainViewParam.Host <- proxyConfig.Host
        mainViewParam.Port <- proxyConfig.Port

        if proxyConfig.HysteriaEnabled = "true" then
            mainViewParam.HysteriaEnabled <- true
            mainViewParam.HysteriaExec <- proxyConfig.HysteriaExec
            mainViewParam.HysteriaConfig <- proxyConfig.HysteriaConfig

        mainViewParam.HysteriaProcess <- hysteriaProcess

        match hysteriaProcess with
        | Some _ -> mainViewParam.HysteriaProxyEnabled <- true
        | None -> ()

        mainViewParam

    member private this.CreateMainViewModel(proxyConfigView: ProxyConfigViewModel) : MainViewModel =
        this.InitProxyConfigByView proxyConfigView
        this.InitMainView this.ProxyConfigModel

    member this.ProxyConfigModel
        with get () = proxyConfigModel
        and set v = proxyConfigModel <- v

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.BackToMainView() =
        let proxyConfig = this.ContentViewModel :?> ProxyConfigViewModel
        _mainViewModel <- this.CreateMainViewModel proxyConfig
        this.ContentViewModel <- _mainViewModel

    member this.Confirm() =
        let proxyConfig = this.ContentViewModel :?> ProxyConfigViewModel
        let hostOk = String.IsNullOrWhiteSpace(proxyConfig.Host) |> not
        let portOk = proxyConfig.Port > 0 && proxyConfig.Port < 65535
        let hysteriaEnabled = proxyConfig.HysteriaEnabled
        let hysteriaExecOk = String.IsNullOrWhiteSpace(proxyConfig.HysteriaExec) |> not
        let hysteriaConfigOk = String.IsNullOrWhiteSpace(proxyConfig.HysteriaConfig) |> not

        if hostOk && portOk then
            let config = ProxyConfigModel(proxyConfig.Host, proxyConfig.Port)

            if hysteriaEnabled && hysteriaExecOk && hysteriaConfigOk then
                config.HysteriaEnabled <- "true"
                config.HysteriaExec <- proxyConfig.HysteriaExec
                config.HysteriaConfig <- proxyConfig.HysteriaConfig

            proxyConfigService.SaveConfig(config)
            CreateTipBox "保存成功" |> ignore

    member this.ProxyConfig() =
        let mainView = this.ContentViewModel :?> MainViewModel
        hysteriaProcess <- mainView.HysteriaProcess
        let proxyConfigModel = ProxyConfigViewModel(this.ProxyConfigModel)
        this.ContentViewModel <- proxyConfigModel
