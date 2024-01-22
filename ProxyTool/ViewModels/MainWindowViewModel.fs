namespace ProxyTool.ViewModels

open ReactiveUI

type MainWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>

    do this.ContentViewModel <- new MainViewModel()

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore

    member this.ProxyConfig() =
        this.ContentViewModel <- new ProxyConfigViewModel()
