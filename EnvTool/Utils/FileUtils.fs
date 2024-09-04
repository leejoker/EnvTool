namespace EnvTool.Utils

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Net
open System.Net.Http
open System.Threading
open ICSharpCode.SharpZipLib.GZip
open ICSharpCode.SharpZipLib.Tar

open EnvTool.Utils.CmdUtils

module FileUtils =
    //------------------------------ private functions -----------------------------------------
    let rec private InnerWalkDir (path: string) (dict: byref<Dictionary<string, string>>) =
        if File.Exists(path) then
            dict.Add(path, "f")
        else
            dict.Add(path, "d")

            if Directory.GetFileSystemEntries(path).Length <> 0 then
                let fileList = Directory.EnumerateFileSystemEntries(path) |> Seq.toList

                for f in fileList do
                    InnerWalkDir f &dict

    let rec private InnerCreateDirectory (path: string) =
        let dir = DirectoryInfo(path)

        if not dir.Exists then
            if not dir.Parent.Exists then
                InnerCreateDirectory(dir.Parent.FullName)

            dir.Create()
        else
            ()
    //------------------------------ public functions -------------------------------------------
    let DeCompressFile (source: string, dest: string) =
        if source.ToLower().EndsWith("zip") then
            ZipFile.ExtractToDirectory(source, dest)
        else if source.ToLower().EndsWith("tar.gz") then
            use file = File.OpenRead(source)
            use gzipStream = new GZipInputStream(file)
            use tarArchive = TarArchive.CreateInputTarArchive(gzipStream, null)
            tarArchive.ExtractContents(dest)
        else
            raise (Exception("unknown compressed file type"))

    let SplitFile (path: string) =
        let array = path.Split(Path.DirectorySeparatorChar)

        let fileName =
            array[array.Length - 1]
            |> fun s ->
                if s.LastIndexOf(".") = -1 then
                    s
                else
                    s.Substring(0, s.LastIndexOf("."))

        let dir = array[0 .. array.Length - 2] |> Path.Combine
#if Windows
        (dir, fileName)
#else
        ("/" + dir, fileName)
#endif

    let rec CreateDirectory (path: string) =
        InnerCreateDirectory(path)
        path

    let WalkDir (path: string) =
        let mutable dict = Dictionary<string, string>()
        InnerWalkDir path &dict
        dict

    let DirectoryIsEmpty (path: string) =
        match Directory.Exists(path) with
        | true -> Directory.GetDirectories(path).Length = 0 && Directory.GetFiles(path).Length = 0
        | false -> true

    let rec CleanFile (path: string) =
        match path with
        | _ when File.Exists(path) -> path |> File.Delete
        | _ when Directory.Exists(path) && not (DirectoryIsEmpty(path)) ->
            Directory.EnumerateFileSystemEntries(path) |> Seq.toList |> List.iter CleanFile

            Directory.Delete(path)
        | _ when Directory.Exists(path) && DirectoryIsEmpty(path) -> path |> Directory.Delete
        | _ -> ()

    let CleanDir (path: string) =
        if Directory.Exists(path) then
            Directory.EnumerateDirectories(path) |> Seq.toList |> List.iter CleanFile
        else
            ()

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
                    | _ when progress <> null -> progress.Report((float bytesSoFar / float totalBytes) * 100.0)
                    | _ -> ()
        }

    let RunnableAccess path = RunCmdCommand $"chmod +x {path}/*"
