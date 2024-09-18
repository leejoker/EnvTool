namespace EnvTool.Services

open EnvTool.DataModels
open System.IO
open Newtonsoft.Json
open EnvTool.Utils.SysInfo

type ProxyConfigService() =

    let configDir = $"{CurrentWorkDir()}config"
    let configPath: string = $"{configDir}/proxyConfig.json"
    let defaultProxyConfig: ProxyConfigModel = ProxyConfigModel("127.0.0.1", 10809)

    member this.SaveConfig(proxy: ProxyConfigModel) =
        Directory.Exists(configDir)
        |> function
            | false -> Directory.CreateDirectory(configDir) |> ignore
            | _ -> ()

        File.WriteAllText(configPath, JsonConvert.SerializeObject(proxy))

    member this.LoadFromFile(filePath: string) =
        File.ReadAllText(filePath) |> JsonConvert.DeserializeObject<ProxyConfigModel>

    member this.LoadConfig() =
        Directory.Exists(configDir)
        |> function
            | false -> defaultProxyConfig
            | true ->
                Directory.EnumerateFiles(configDir)
                |> Seq.filter (_.ToString().EndsWith("proxyConfig.json"))
                |> fun l ->
                    match Seq.isEmpty l with
                    | true -> defaultProxyConfig
                    | false -> l |> Seq.head |> this.LoadFromFile
