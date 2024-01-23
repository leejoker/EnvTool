namespace ProxyTool.ViewModels

open ReactiveUI
open System

type ProxyConfigViewModel() as this =
    inherit ViewModelBase()

    let mutable _host: string = "127.0.0.1"
    let mutable _port: int = 10809

    let mutable backCommand =
        Unchecked.defaultof<ReactiveCommand<Reactive.Unit, Reactive.Unit>>

    do this.BackCommand <- ReactiveCommand.Create(fun () -> ())
    do this.Host <- _host
    do this.Port <- _port

    member this.BackCommand
        with get () = backCommand
        and private set v = backCommand <- v

    member this.Host
        with get () = _host
        and set (v: string) = this.RaiseAndSetIfChanged(&_host, v) |> ignore

    member this.Port
        with get () = _port
        and set v = this.RaiseAndSetIfChanged(&_port, v) |> ignore
