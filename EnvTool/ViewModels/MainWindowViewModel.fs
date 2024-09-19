namespace EnvTool.ViewModels

open ReactiveUI
open EnvTool.Services
open EnvTool.ViewModels.Utils.MessageBoxUtils
open System

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable globalStatement = GlobalStatement()

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    do globalStatement.InitGlobalStatement()

    do this.ContentViewModel <- MainViewModel globalStatement

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.BackToMainView() =
        this.ContentViewModel <- MainViewModel(globalStatement)

    member this.Confirm() =
        let proxyConfig = this.ContentViewModel :?> ProxyConfigViewModel
        let hostOk = String.IsNullOrWhiteSpace(proxyConfig.Host) |> not
        let portOk = proxyConfig.Port > 0 && proxyConfig.Port < 65535

        if hostOk && portOk then
            globalStatement.SaveProxyConfig()
            CreateTipBox "保存成功" |> ignore

    member this.ProxyConfig() =
        let proxyConfigModel = ProxyConfigViewModel(globalStatement)
        this.ContentViewModel <- proxyConfigModel
