namespace EnvTool.DataModels

open Newtonsoft.Json

type ProxyConfigModel(host: string, port: int) =

    let mutable _host: string = host
    let mutable _port: int = port
    let mutable _gitProxyEnabled: bool = false

    [<JsonProperty("global.host")>]
    member this.Host
        with get () = _host
        and set (v: string) = _host <- v

    [<JsonProperty("global.port")>]
    member this.Port
        with get () = _port
        and set v = _port <- v
