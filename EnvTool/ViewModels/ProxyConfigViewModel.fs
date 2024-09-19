namespace EnvTool.ViewModels

open EnvTool.Services
open ReactiveUI

type ProxyConfigViewModel() =
    inherit ViewModelBase()

    let mutable globalStatement: GlobalStatement = Unchecked.defaultof<GlobalStatement>

    let mutable host = Unchecked.defaultof<string>
    let mutable port = Unchecked.defaultof<int>
    let mutable exec = Unchecked.defaultof<string>
    let mutable config = Unchecked.defaultof<string>

    new(globalStatement: GlobalStatement) as this =
        ProxyConfigViewModel()

        then
            this.GlobalStatement <- globalStatement
            this.Host <- globalStatement.GlobalProxyConfigModel.Host
            this.Port <- globalStatement.GlobalProxyConfigModel.Port
            this.HysteriaExec <- globalStatement.GlobalProxyConfigModel.HysteriaExec
            this.HysteriaConfig <- globalStatement.GlobalProxyConfigModel.HysteriaConfig

    member this.GlobalStatement
        with get () = globalStatement
        and set v = globalStatement <- v

    member this.Host
        with get () = host
        and set (v: string) =
            this.RaiseAndSetIfChanged(&host, v) |> ignore
            globalStatement.GlobalProxyConfigModel.Host <- v

    member this.Port
        with get () = port
        and set v =
            this.RaiseAndSetIfChanged(&port, v) |> ignore
            globalStatement.GlobalProxyConfigModel.Port <- v

    member this.HysteriaEnabled
        with get () = globalStatement.HysteriaEnabled
        and set v = globalStatement.HysteriaEnabled <- v

    member this.HysteriaExec
        with get () = exec
        and set v =
            this.RaiseAndSetIfChanged(&exec, v) |> ignore
            globalStatement.GlobalProxyConfigModel.HysteriaExec <- v

    member this.HysteriaConfig
        with get () = config
        and set v =
            this.RaiseAndSetIfChanged(&config, v) |> ignore
            globalStatement.GlobalProxyConfigModel.HysteriaConfig <- v
