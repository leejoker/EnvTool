namespace ProxyTool.ViewModels

open ReactiveUI

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let _mainView = new MainViewModel()

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    do this.ContentViewModel <- _mainView

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.ProxyConfig() =
        let proxyConfigModel = new ProxyConfigViewModel()

        proxyConfigModel.BackCommand.Subscribe(fun _ -> this.ContentViewModel <- _mainView)
        |> ignore

        this.ContentViewModel <- proxyConfigModel
