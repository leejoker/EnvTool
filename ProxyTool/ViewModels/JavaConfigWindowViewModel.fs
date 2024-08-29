namespace ProxyTool.ViewModels

open ReactiveUI

type JavaConfigWindowViewModel() as this =
    inherit ViewModelBase()

    let mutable _mainViewModel = Unchecked.defaultof<JdkManagementViewModel>

    let mutable contentViewModel: ViewModelBase = Unchecked.defaultof<ViewModelBase>


    do this.ContentViewModel <- JdkManagementViewModel()

    member this.ContentViewModel
        with get () = contentViewModel
        and private set (value: ViewModelBase) = this.RaiseAndSetIfChanged(&contentViewModel, value) |> ignore
