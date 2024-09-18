namespace EnvTool.ViewModels

open System.Diagnostics
open EnvTool.Utils.ProxyUtils
open EnvTool.Services.HysteriaService

type MainViewModel() =
    inherit ViewModelBase()

    let mutable host = Unchecked.defaultof<string>
    let mutable port = Unchecked.defaultof<int>
    let mutable hysteriaExec = Unchecked.defaultof<string>
    let mutable hysteriaConfig = Unchecked.defaultof<string>
    let mutable hysteriaProcess = None
    
    let mutable gitProxyEnabled = false
    let mutable systemProxyEnabled = false
    let mutable hysteriaProxyEnabled = false
    let mutable hysteriaEnabled = false

    do systemProxyEnabled <- SystemProxyStatus()
    do gitProxyEnabled <- GitProxyEnabled()
    
    //TODO add hysteriaStatus

    member this.GitProxyEnabled
        with get () = gitProxyEnabled
        and set v = gitProxyEnabled <- v

    member this.SystemProxyEnabled
        with get () = systemProxyEnabled
        and set v = systemProxyEnabled <- v

    member this.HysteriaProxyEnabled
        with get () = hysteriaProxyEnabled
        and set v = hysteriaProxyEnabled <- v

    member this.HysteriaEnabled
        with get () = hysteriaEnabled
        and set v = hysteriaEnabled <- v
        
    member this.Host
        with get () = host
        and set v = host <- v

    member this.Port
        with get () = port
        and set v = port <- v

    member this.HysteriaExec
        with get () = hysteriaExec
        and set v = hysteriaExec <- v

    member this.HysteriaConfig
        with get () = hysteriaConfig
        and set v = hysteriaConfig <- v

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

    member this.HandleHysteriaProxy() =
        if hysteriaProxyEnabled then
            let (result, proc) = StartHysteriaProcess hysteriaExec hysteriaConfig
            if result then
                hysteriaProcess <- proc
        else
            KillHysteriaProcess hysteriaProcess
