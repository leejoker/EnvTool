namespace EnvTool.ViewModels

open ReactiveUI
open System
open EnvTool.DataModels

type ProxyConfigViewModel(proxy: ProxyConfigModel) as this =
    inherit ViewModelBase()

    let _proxy = proxy
    let mutable _host: string = Unchecked.defaultof<string>
    let mutable _port: int = Unchecked.defaultof<int>

    let mutable backCommand =
        Unchecked.defaultof<ReactiveCommand<Reactive.Unit, Reactive.Unit>>

    let mutable confirmCommand =
        Unchecked.defaultof<ReactiveCommand<Reactive.Unit, ProxyConfigModel>>

    let isValidHostObservable =
        this.WhenAnyValue((fun x -> x.Host), (fun x -> String.IsNullOrWhiteSpace(x) |> not))

    let isValidPortObservable =
        this.WhenAnyValue((fun x -> x.Port), (fun x -> (x > 0 && x < 65535)))

    let isValidObservable = Observable.merge isValidHostObservable isValidPortObservable

    do this.Host <- _proxy.Host
    do this.Port <- _proxy.Port

    do this.BackCommand <- ReactiveCommand.Create(fun () -> ())

    do
        this.ConfirmCommand <-
            ReactiveCommand.Create(
                Func<ProxyConfigModel>(fun () -> ProxyConfigModel(this.Host, this.Port)),
                isValidObservable
            )

    member this.BackCommand
        with get () = backCommand
        and private set v = backCommand <- v

    member this.ConfirmCommand
        with get () = confirmCommand
        and private set v = confirmCommand <- v

    member this.Host
        with get () = _host
        and set (v: string) = this.RaiseAndSetIfChanged(&_host, v) |> ignore

    member this.Port
        with get () = _port
        and set v = this.RaiseAndSetIfChanged(&_port, v) |> ignore
