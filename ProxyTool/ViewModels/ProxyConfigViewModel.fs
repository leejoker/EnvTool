namespace ProxyTool.ViewModels

open ReactiveUI
open System

type ProxyConfigViewModel() as this =
    inherit ViewModelBase()

    let mutable backCommand =
        Unchecked.defaultof<ReactiveCommand<Reactive.Unit, Reactive.Unit>>

    do this.BackCommand <- ReactiveCommand.Create(fun () -> ())

    member this.BackCommand
        with get () = backCommand
        and private set v = backCommand <- v
