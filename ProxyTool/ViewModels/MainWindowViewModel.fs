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

    let mainView = MainViewModel()

    do this.ProxyConfigModel <- proxyConfigService.LoadConfig()

    do mainView.Host <- proxyConfigModel.Host
    do mainView.Port <- proxyConfigModel.Port
    do this.ContentViewModel <- mainView

    member private this.CreateMainViewModel (host: string) (port: int) : MainViewModel =
        let mainViewModel = MainViewModel()
        mainViewModel.Host <- host
        mainViewModel.Port <- port
        mainViewModel

    member this.ProxyConfigModel
        with get () = proxyConfigModel
        and set v = proxyConfigModel <- v

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.ProxyConfig() =
        let proxyConfigModel = ProxyConfigViewModel(this.ProxyConfigModel)

        proxyConfigModel.BackCommand.Subscribe(fun _ ->
            _mainViewModel <- this.CreateMainViewModel proxyConfigModel.Host proxyConfigModel.Port
            this.ContentViewModel <- _mainViewModel)
        |> ignore

        proxyConfigModel.ConfirmCommand.Subscribe(fun p ->
            this.ProxyConfigModel <- ProxyConfigModel(p.Host, p.Port)
            proxyConfigService.SaveConfig(this.ProxyConfigModel)
            CreateTipBox "保存成功" |> ignore
            _mainViewModel <- this.CreateMainViewModel proxyConfigModel.Host proxyConfigModel.Port
            this.ContentViewModel <- _mainViewModel)
        |> ignore

        this.ContentViewModel <- proxyConfigModel
