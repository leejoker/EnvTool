namespace ProxyTool.Services

open ProxyTool.DataModels
open System.IO
open System.Text.Json

type ProxyConfigService() =

    let configPath: string = "./config/proxyConfig.json"
    let defaultProxyConfig: ProxyConfigModel = new ProxyConfigModel("127.0.0.1", 10809)

    member this.SaveConfig(proxy: ProxyConfigModel) =
        Directory.Exists("config")
        |> function
            | false -> Directory.CreateDirectory("./config") |> ignore
            | _ -> ()

        use fs = File.OpenWrite(configPath)
        JsonSerializer.Serialize(fs, proxy)

    member this.LoadFromFile(filePath: string) =
        use fs = File.OpenRead(filePath)
        JsonSerializer.Deserialize<ProxyConfigModel>(fs)

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
