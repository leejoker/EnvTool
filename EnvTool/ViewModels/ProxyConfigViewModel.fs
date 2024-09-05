namespace EnvTool.ViewModels

open ReactiveUI
open EnvTool.DataModels

type ProxyConfigViewModel(proxy: ProxyConfigModel) as this =
    inherit ViewModelBase()

    let _proxy = proxy
    let mutable _host: string = Unchecked.defaultof<string>
    let mutable _port: int = Unchecked.defaultof<int>

    do this.Host <- _proxy.Host
    do this.Port <- _proxy.Port

    member this.Host
        with get () = _host
        and set (v: string) = this.RaiseAndSetIfChanged(&_host, v) |> ignore

    member this.Port
        with get () = _port
        and set v = this.RaiseAndSetIfChanged(&_port, v) |> ignore
