namespace ProxyTool.Utils

open System
open System.IO
open System.Net
open System.Net.Http
open System.Threading

module FileUtils =
    let SplitFile (path: string) =
        let array = path.Split(Path.DirectorySeparatorChar)

        let fileName =
            array[array.Length - 1] |> fun s -> s.Substring(0, s.LastIndexOf("."))

        let dir = array[0 .. array.Length - 2] |> Path.Combine
        (dir, fileName)

    // TODO
    // let WalkDir (path: string)=


    let rec CreateDirectory (path: string) : string =
        let dir = DirectoryInfo(path)

        if not dir.Exists then
            if not dir.Parent.Exists then
                CreateDirectory(dir.Parent.FullName)
            else
                dir.Create()
                path
        else
            path

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

    let DownloadFileTask (url: string, path: string, progress: IProgress<double>) =
        async {
            let handler = new HttpClientHandler()
            handler.AutomaticDecompression <- DecompressionMethods.GZip
            use httpClient = new HttpClient()
            httpClient.Timeout <- Timeout.InfiniteTimeSpan

            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.163 Safari/535.1"
            )

            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            response.EnsureSuccessStatusCode() |> ignore

            let totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1L)
            let mutable bytesSoFar = 0L

            use! contentStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask

            use fileStream =
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true)

            let buffer = Array.zeroCreate<byte> (1024 * 1024)
            let mutable isMoreToRead = true

            while isMoreToRead do
                let! bytesRead = contentStream.ReadAsync(buffer, 0, buffer.Length) |> Async.AwaitTask

                if bytesRead = 0 then
                    isMoreToRead <- false
                else
                    do! fileStream.WriteAsync(buffer, 0, bytesRead) |> Async.AwaitTask
                    bytesSoFar <- bytesSoFar + int64 bytesRead

                    match progress with
                    | _ when not (progress = null) -> progress.Report((float bytesSoFar / float totalBytes) * 100.0)
                    | _ -> ()
        }
