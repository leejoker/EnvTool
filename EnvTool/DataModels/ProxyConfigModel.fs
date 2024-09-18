namespace EnvTool.DataModels

open Newtonsoft.Json

type ProxyConfigModel(host: string, port: int) =

    let mutable _host: string = host
    let mutable _port: int = port
    let mutable _hysteriaEnabled: string = Unchecked.defaultof<string>
    let mutable _hysteriaExec: string = Unchecked.defaultof<string>
    let mutable _hysteriaConfig: string = Unchecked.defaultof<string>

    [<JsonProperty("global.host")>]
    member this.Host
        with get () = _host
        and set (v: string) = _host <- v

    [<JsonProperty("global.port")>]
    member this.Port
        with get () = _port
        and set v = _port <- v

    [<JsonProperty("h2", NullValueHandling = NullValueHandling.Ignore)>]
    member this.HysteriaEnabled
        with get () = _hysteriaEnabled
        and set v = _hysteriaEnabled <- v

    [<JsonProperty("h2.exec", NullValueHandling = NullValueHandling.Ignore)>]
    member this.HysteriaExec
        with get () = _hysteriaExec
        and set v = _hysteriaExec <- v

    [<JsonProperty("h2.config", NullValueHandling = NullValueHandling.Ignore)>]
    member this.HysteriaConfig
        with get () = _hysteriaConfig
        and set v = _hysteriaConfig <- v
