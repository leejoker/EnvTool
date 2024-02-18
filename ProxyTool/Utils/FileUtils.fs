namespace ProxyTool.Utils

open System
open System.IO
open System.Net.Http

module FileUtils =
    let rec CreateDirectory (path: string) =
        let dir = DirectoryInfo(path)

        if not dir.Exists then
            if not dir.Parent.Exists then
                CreateDirectory(dir.Parent.FullName) |> ignore
            else
                dir.Create()
        else
            ()

    let DirectoryIsEmpty (path: string) =
        match Directory.Exists(path) with
        | true -> Directory.GetDirectories(path).Length = 0 && Directory.GetFiles(path).Length = 0
        | false -> true

    let rec CleanFile (path: string) =
        match path with
        | _ when File.Exists(path) -> path |> File.Delete
        | _ when Directory.Exists(path) && not (DirectoryIsEmpty(path)) ->
            Directory.EnumerateFileSystemEntries(path)
            |> Seq.toList
            |> List.iter (fun f -> f |> CleanFile)

            Directory.Delete(path)
        | _ when Directory.Exists(path) && DirectoryIsEmpty(path) -> path |> Directory.Delete
        | _ -> ()

    let DownloadFileAsync (url: string, path: string, progress: IProgress<double>) =
        task {
            use httpClient = new HttpClient()

            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.163 Safari/535.1"
            )

            let! response =
                httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                |> Async.AwaitTask

            response.EnsureSuccessStatusCode() |> ignore

            let totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L)
            let mutable bytesSoFar = 0L

            use! contentStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask

            use fileStream =
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true)

            let buffer = Array.zeroCreate<byte> 8192
            let mutable isMoreToRead = true

            while isMoreToRead do
                let! bytesRead = contentStream.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask

                if bytesRead = 0 then
                    isMoreToRead <- false
                else
                    do! fileStream.WriteAsync(buffer, 0, bytesRead) |> Async.AwaitTask
                    bytesSoFar <- bytesSoFar + int64 bytesRead

                    match progress with
                    | null -> progress.Report((float bytesSoFar / float totalBytes) * 100.0)
                    | _ -> ()
        }
