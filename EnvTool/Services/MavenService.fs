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
        let userProfile = Environment.GetEnvironmentVariable("USERPROFILE")
        Path.Combine(userProfile, ".m2", "settings.xml")
#else
        let home = Environment.GetEnvironmentVariable("HOME")
        Path.Combine(home, ".m2", "settings.xml")
#endif

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
        Environment.GetEnvironmentVariable("MAVEN_HOME")
        |> Option.ofObj
        |> Option.orElseWith (fun () -> Environment.GetEnvironmentVariable("M2_HOME") |> Option.ofObj)

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
                let mirrorsElement = doc.Descendants("mirrors") |> Seq.tryHead

                match mirrorsElement with
                | Some(mirrors) ->
                    mirrors.Elements("mirror")
                    |> Seq.map (fun (m: XElement) ->
                        let idAttr = m.Attribute(XName.Get("id"))
                        let nameAttr = m.Attribute(XName.Get("name"))
                        let urlElem = m.Element(XName.Get("url"))
                        let id = if idAttr <> null then idAttr.Value else ""
                        let name = if nameAttr <> null then nameAttr.Value else ""
                        let url = if urlElem <> null then urlElem.Value else ""
                        let isDefault = idAttr <> null && idAttr.Value = "central"
                        { Id = id; Name = name; Url = url; IsDefault = isDefault })
                    |> Seq.toList
                | None -> getDefaultSources()
            with
            | _ -> getDefaultSources()
        | None -> getDefaultSources()

    let AddSource (source: MavenSource) =
        let content = GetSettingsContent() |> Option.defaultValue "<settings></settings>"
        try
            let doc = XDocument.Parse(content)

            let settings =
                match doc.Elements("settings") |> Seq.tryHead with
                | Some(s) -> s
                | None ->
                    let newSettings = XElement("settings")
                    doc.Add(newSettings)
                    newSettings

            let mirrors =
                match settings.Elements("mirrors") |> Seq.tryHead with
                | Some(m) -> m
                | None ->
                    let newMirrors = XElement("mirrors")
                    settings.Add(newMirrors)
                    newMirrors

            let newMirror = XElement("mirror")
            newMirror.Add(XAttribute("id", source.Id))
            newMirror.Add(XAttribute("name", source.Name))
            newMirror.Add(XElement("url", source.Url))
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
                let mirrors = doc.Descendants("mirrors") |> Seq.tryHead

                match mirrors with
                | Some(mirrorsElement) ->
                    let mirrorToRemove =
                        mirrorsElement.Elements("mirror")
                        |> Seq.tryFind (fun m -> m.Attribute("id").Value = id)

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
                let mirrors = doc.Descendants("mirrors") |> Seq.tryHead

                match mirrors with
                | Some(mirrorsElement) ->
                    // Reset all mirrors to non-default
                    mirrorsElement.Elements("mirror")
                    |> Seq.iter (fun m ->
                        let existingId = m.Attribute("id") |> Option.ofObj |> Option.map (fun a -> a.Value)
                        if existingId = Some("central") then
                            m.Attribute("id").Remove()
                    )

                    // Find the mirror with the given id and add central id to it
                    let targetMirror =
                        mirrorsElement.Elements("mirror")
                        |> Seq.tryFind (fun m -> m.Attribute("id").Value = id)

                    match targetMirror with
                    | Some(m) ->
                        m.Attribute("id").SetValue("central")
                        SaveSettings(doc.ToString())
                        true
                    | None -> false
                | None -> false
            with
            | _ -> false
        | None -> false
