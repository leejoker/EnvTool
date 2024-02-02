namespace ProxyTool.ViewModels

open ReactiveUI
open ProxyTool.Services
open ProxyTool.DataModels
open ProxyTool.ViewModels.Utils.MessageBoxUtils

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable _mainViewModel = Unchecked.defaultof<MainViewModel>

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    let mutable proxyConfigModel: ProxyConfigModel =
        Unchecked.defaultof<ProxyConfigModel>

    let proxyConfigService = ProxyConfigService()

    do this.ProxyConfigModel <- proxyConfigService.LoadConfig()

    do this.ContentViewModel <- new MainViewModel(this.ProxyConfigModel)

    member this.ProxyConfigModel
        with get () = proxyConfigModel
        and set v = proxyConfigModel <- v

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.ProxyConfig() =
        let proxyConfigModel = new ProxyConfigViewModel(this.ProxyConfigModel)

        proxyConfigModel.BackCommand.Subscribe(fun _ ->
            _mainViewModel <- new MainViewModel(this.ProxyConfigModel)
            this.ContentViewModel <- _mainViewModel)
        |> ignore

        proxyConfigModel.ConfirmCommand.Subscribe(fun p ->
            this.ProxyConfigModel <- ProxyConfigModel(p.Host, p.Port)
            proxyConfigService.SaveConfig(this.ProxyConfigModel)
            CreateTipBox "保存成功" |> ignore
            _mainViewModel <- new MainViewModel(this.ProxyConfigModel)
            this.ContentViewModel <- _mainViewModel)
        |> ignore

        this.ContentViewModel <- proxyConfigModel
