namespace EnvTool.ViewModels

open EnvTool.Utils.ProxyUtils

type MainViewModel() =
    inherit ViewModelBase()

    let mutable host = Unchecked.defaultof<string>
    let mutable port = Unchecked.defaultof<int>
    let mutable gitProxyEnabled = false
    let mutable systemProxyEnabled = false

    do systemProxyEnabled <- SystemProxyStatus()
    do gitProxyEnabled <- GitProxyEnabled()

    member this.GitProxyEnabled
        with get () = gitProxyEnabled
        and set v = gitProxyEnabled <- v

    member this.SystemProxyEnabled
        with get () = systemProxyEnabled
        and set v = systemProxyEnabled <- v

    member this.Host
        with get () = host
        and set v = host <- v

    member this.Port
        with get () = port
        and set v = port <- v

    member this.HandleSystemProxy() =
        if systemProxyEnabled then
            SetSystemProxy host port
        else
            CloseSystemProxy()

    member this.HandleGitProxy() =
        if gitProxyEnabled then
            SetGitProxy host port
        else
            RemoveGitProxy()
