namespace EnvTool.Services

open System
open System.IO
open System.Xml.Linq

type MavenSource = {
    Id: string
    Name: string
    Url: string
    IsDefault: bool
}

module MavenService =
    let private getSettingsPath () =
#if Windows
        let userProfile =
            Environment.GetEnvironmentVariable("USERPROFILE")
            |> Option.ofObj
            |> Option.orElseWith (fun () -> Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) |> Option.ofObj)
            |> Option.defaultWith (fun () -> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
        Path.Combine(userProfile, ".m2", "settings.xml")
#else
        let home = Environment.GetEnvironmentVariable("HOME")
        Path.Combine(home, ".m2", "settings.xml")
#endif

    let private mavenNs = XNamespace.Get("http://maven.apache.org/SETTINGS/1.0.0")

    let GetVersion () =
        try
            let p = new System.Diagnostics.Process()
            p.StartInfo.FileName <- "mvn"
            p.StartInfo.Arguments <- "-version"
            p.StartInfo.UseShellExecute <- false
            p.StartInfo.RedirectStandardOutput <- true
            p.StartInfo.RedirectStandardError <- true
            p.StartInfo.CreateNoWindow <- true
            p.StartInfo.WorkingDirectory <- Directory.GetCurrentDirectory()

            p.Start() |> ignore
            let output = p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            p.Close()

            // Parse Maven version from output like "Apache Maven 3.9.6 ..."
            let lines = output.Split([|'\n'; '\r'|], StringSplitOptions.RemoveEmptyEntries)
            lines
            |> Array.tryFind (fun line -> line.StartsWith("Apache Maven") || line.StartsWith("Maven"))
            |> Option.map (fun line ->
                let parts = line.Split(' ')
                if parts.Length >= 3 then parts[2] else "")
        with
        | _ -> None

    let GetMavenHome () =
        // First try environment variables
        Environment.GetEnvironmentVariable("MAVEN_HOME")
        |> Option.ofObj
        |> Option.orElseWith (fun () -> Environment.GetEnvironmentVariable("M2_HOME") |> Option.ofObj)
        |> Option.orElseWith (fun () ->
            // Try to find mvn in PATH and infer Maven home
            try
                let p = new System.Diagnostics.Process()
#if Windows
                p.StartInfo.FileName <- "where"
                p.StartInfo.Arguments <- "mvn"
#else
                p.StartInfo.FileName <- "which"
                p.StartInfo.Arguments <- "mvn"
#endif
                p.StartInfo.UseShellExecute <- false
                p.StartInfo.RedirectStandardOutput <- true
                p.StartInfo.RedirectStandardError <- true
                p.StartInfo.CreateNoWindow <- true
                p.Start() |> ignore
                let output = p.StandardOutput.ReadLine()
                p.WaitForExit()
                p.Close()

                // Parse mvn path: e.g., C:\Program Files\Maven\bin\mvn.cmd
                // Maven home is two levels up from bin directory
                match output with
                | null | "" -> None
                | path ->
                    let dir = Path.GetDirectoryName(path)
                    if String.IsNullOrEmpty(dir) then None
                    else
                        let parent = Directory.GetParent(dir)
                        if parent <> null then Some(parent.FullName) else None
            with
            | _ -> None
        )

    let GetSettingsContent () =
        let settingsPath = getSettingsPath ()
        if File.Exists(settingsPath) then
            Some(File.ReadAllText(settingsPath))
        else
            None

    let SaveSettings (content: string) =
        let settingsPath = getSettingsPath ()
        let dir = Path.GetDirectoryName(settingsPath)
        if not (Directory.Exists(dir)) then
            Directory.CreateDirectory(dir) |> ignore
        File.WriteAllText(settingsPath, content)

    let private getDefaultSources () =
        [
            { Id = "central"; Name = "Maven Central"; Url = "https://repo.maven.apache.org/maven2"; IsDefault = true }
            { Id = "aliyun"; Name = "Aliyun Maven"; Url = "https://maven.aliyun.com/repository/public"; IsDefault = false }
        ]

    let GetSources () =
        match GetSettingsContent() with
        | Some(content) ->
            try
                let doc = XDocument.Parse(content)
                let mirrorsElement = doc.Descendants(mavenNs + "mirrors") |> Seq.tryHead

                match mirrorsElement with
                | Some(mirrors) ->
                    mirrors.Elements(mavenNs + "mirror")
                    |> Seq.map (fun (m: XElement) ->
                        let idElem = m.Element(mavenNs + "id")
                        let nameElem = m.Element(mavenNs + "name")
                        let urlElem = m.Element(mavenNs + "url")
                        let id = if idElem <> null then idElem.Value else ""
                        let name = if nameElem <> null then nameElem.Value else ""
                        let url = if urlElem <> null then urlElem.Value else ""
                        let isDefault = idElem <> null && idElem.Value = "central"
                        { Id = id; Name = name; Url = url; IsDefault = isDefault })
                    |> Seq.toList
                | None -> getDefaultSources()
            with
            | _ -> getDefaultSources()
        | None -> getDefaultSources()

    let AddSource (source: MavenSource) =
        let content = GetSettingsContent() |> Option.defaultValue "<settings xmlns=\"http://maven.apache.org/SETTINGS/1.0.0\"></settings>"
        try
            let doc = XDocument.Parse(content)

            let settings =
                match doc.Elements(mavenNs + "settings") |> Seq.tryHead with
                | Some(s) -> s
                | None ->
                    let newSettings = XElement(mavenNs + "settings")
                    doc.Add(newSettings)
                    newSettings

            let mirrors =
                match settings.Elements(mavenNs + "mirrors") |> Seq.tryHead with
                | Some(m) -> m
                | None ->
                    let newMirrors = XElement(mavenNs + "mirrors")
                    settings.Add(newMirrors)
                    newMirrors

            let newMirror = XElement(mavenNs + "mirror")
            newMirror.Add(XElement(mavenNs + "id", source.Id))
            newMirror.Add(XElement(mavenNs + "name", source.Name))
            newMirror.Add(XElement(mavenNs + "url", source.Url))
            mirrors.Add(newMirror)

            SaveSettings(doc.ToString())
            true
        with
        | _ -> false

    let RemoveSource (id: string) =
        match GetSettingsContent() with
        | Some(content) ->
            try
                let doc = XDocument.Parse(content)
                let mirrors = doc.Descendants(mavenNs + "mirrors") |> Seq.tryHead

                match mirrors with
                | Some(mirrorsElement) ->
                    let mirrorToRemove =
                        mirrorsElement.Elements(mavenNs + "mirror")
                        |> Seq.tryFind (fun m ->
                            let idElem = m.Element(mavenNs + "id")
                            idElem <> null && idElem.Value = id)

                    match mirrorToRemove with
                    | Some(m) ->
                        m.Remove()
                        SaveSettings(doc.ToString())
                        true
                    | None -> false
                | None -> false
            with
            | _ -> false
        | None -> false

    let SetDefaultSource (id: string) =
        match GetSettingsContent() with
        | Some(content) ->
            try
                let doc = XDocument.Parse(content)
                let mirrors = doc.Descendants(mavenNs + "mirrors") |> Seq.tryHead

                match mirrors with
                | Some(mirrorsElement) ->
                    // Reset all mirrors to non-default
                    mirrorsElement.Elements(mavenNs + "mirror")
                    |> Seq.iter (fun m ->
                        let idElem = m.Element(mavenNs + "id")
                        if idElem <> null && idElem.Value = "central" then
                            idElem.SetValue("")
                    )

                    // Find the mirror with the given id and add central id to it
                    let targetMirror =
                        mirrorsElement.Elements(mavenNs + "mirror")
                        |> Seq.tryFind (fun m ->
                            let idElem = m.Element(mavenNs + "id")
                            idElem <> null && idElem.Value = id)

                    match targetMirror with
                    | Some(m) ->
                        let idElem = m.Element(mavenNs + "id")
                        if idElem <> null then idElem.SetValue("central")
                        SaveSettings(doc.ToString())
                        true
                    | None -> false
                | None -> false
            with
            | _ -> false
        | None -> false
