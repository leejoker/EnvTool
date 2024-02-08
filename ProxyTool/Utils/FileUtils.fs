namespace ProxyTool.Utils

open System.IO

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
