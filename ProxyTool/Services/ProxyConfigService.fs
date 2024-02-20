namespace ProxyTool.Services

open ProxyTool.DataModels
open System.IO
open Newtonsoft.Json

type ProxyConfigService() =

    let configPath: string = "./config/proxyConfig.json"
    let defaultProxyConfig: ProxyConfigModel = new ProxyConfigModel("127.0.0.1", 10809)

    member this.SaveConfig(proxy: ProxyConfigModel) =
        Directory.Exists("config")
        |> function
            | false -> Directory.CreateDirectory("./config") |> ignore
            | _ -> ()

        File.WriteAllText(configPath, JsonConvert.SerializeObject(proxy))

    member this.LoadFromFile(filePath: string) =
        File.ReadAllText(filePath) |> JsonConvert.DeserializeObject<ProxyConfigModel>

    member this.LoadConfig() =
        Directory.Exists("config")
        |> function
            | false -> defaultProxyConfig
            | true ->
                Directory.EnumerateFiles("./config")
                |> Seq.filter (fun x -> x.ToString().EndsWith("proxyConfig.json"))
                |> fun l ->
                    match Seq.isEmpty l with
                    | true -> defaultProxyConfig
                    | false -> l |> Seq.head |> this.LoadFromFile
