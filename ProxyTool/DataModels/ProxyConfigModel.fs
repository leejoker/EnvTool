namespace ProxyTool.DataModels

type ProxyConfigModel(host: string, port: int) =

    let mutable _host: string = host
    let mutable _port: int = port

    member this.Host
        with get () = _host
        and set (v: string) = _host <- v

    member this.Port
        with get () = _port
        and set v = _port <- v
