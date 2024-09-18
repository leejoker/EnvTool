namespace EnvTool.ViewModels

open ReactiveUI
open EnvTool.Services
open EnvTool.DataModels
open EnvTool.Utils.ProxyUtils
open EnvTool.ViewModels.Utils.MessageBoxUtils
open System

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable _mainViewModel = Unchecked.defaultof<MainViewModel>

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    let mutable proxyConfigModel: ProxyConfigModel =
        Unchecked.defaultof<ProxyConfigModel>

    let proxyConfigService = ProxyConfigService()

    let mainView = MainViewModel()

    do this.ProxyConfigModel <- proxyConfigService.LoadConfig()

    do mainView.Host <- proxyConfigModel.Host
    do mainView.Port <- proxyConfigModel.Port
    do this.ContentViewModel <- mainView

    member private this.CreateMainViewModel (host: string) (port: int) : MainViewModel =
        let mainViewModel = MainViewModel()
        mainViewModel.Host <- host
        mainViewModel.Port <- port

        this.ProxyConfigModel <- ProxyConfigModel(host, port)
        mainViewModel

    member this.ProxyConfigModel
        with get () = proxyConfigModel
        and set v = proxyConfigModel <- v

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.BackToMainView() =
        let proxyConfig = this.ContentViewModel :?> ProxyConfigViewModel
        _mainViewModel <- this.CreateMainViewModel proxyConfig.Host proxyConfig.Port

        if _mainViewModel.GitProxyEnabled then
            SetGitProxy proxyConfig.Host proxyConfig.Port

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
        let proxyConfigModel = ProxyConfigViewModel(this.ProxyConfigModel)
        this.ContentViewModel <- proxyConfigModel
