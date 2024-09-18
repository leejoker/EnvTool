namespace EnvTool.ViewModels

open ReactiveUI
open EnvTool.DataModels

type ProxyConfigViewModel(proxy: ProxyConfigModel) as this =
    inherit ViewModelBase()

    let _proxy = proxy
    let mutable _host: string = Unchecked.defaultof<string>
    let mutable _port: int = Unchecked.defaultof<int>
    let mutable _hysteriaEnabled: bool = false
    let mutable _hysteriaExec: string = Unchecked.defaultof<string>
    let mutable _hysteriaConfig: string = Unchecked.defaultof<string>

    do this.Host <- _proxy.Host
    do this.Port <- _proxy.Port
    do this.HysteriaEnabled <- (_proxy.HysteriaEnabled = "true")
    do this.HysteriaExec <- _proxy.HysteriaExec
    do this.HysteriaConfig <- _proxy.HysteriaConfig

    member this.Host
        with get () = _host
        and set (v: string) = this.RaiseAndSetIfChanged(&_host, v) |> ignore

    member this.Port
        with get () = _port
        and set v = this.RaiseAndSetIfChanged(&_port, v) |> ignore

    member this.HysteriaEnabled
        with get () = _hysteriaEnabled
        and set v = _hysteriaEnabled <- v

    member this.HysteriaExec
        with get () = _hysteriaExec
        and set v = this.RaiseAndSetIfChanged(&_hysteriaExec, v) |> ignore

    member this.HysteriaConfig
        with get () = _hysteriaConfig
        and set v = this.RaiseAndSetIfChanged(&_hysteriaConfig, v) |> ignore
