namespace ProxyTool.Services

open ProxyTool.DataModels

type ProxyConfigService() =

    let proxyConfig: ProxyConfigModel = new ProxyConfigModel("127.0.0.1", 10809)

    member this.GetProxyConfig = ref proxyConfig
