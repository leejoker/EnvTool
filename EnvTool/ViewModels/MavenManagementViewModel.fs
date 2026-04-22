namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open EnvTool.Services
open ReactiveUI

type MavenSourceItem = {
    Id: string
    Name: string
    Url: string
    IsDefault: bool
}

type MavenManagementViewModel() =
    inherit ViewModelBase()

    let mutable currentVersion = ""
    let mutable mavenHome = ""
    let mutable settingsContent = ""
    let mutable selectedSource: MavenSourceItem option = None
    let sources = ObservableCollection<MavenSourceItem>()

    member this.CurrentVersion
        with get () = currentVersion
        and set v = this.RaiseAndSetIfChanged(&currentVersion, v) |> ignore

    member this.MavenHome
        with get () = mavenHome
        and set v = this.RaiseAndSetIfChanged(&mavenHome, v) |> ignore

    member this.SettingsContent
        with get () = settingsContent
        and set v = this.RaiseAndSetIfChanged(&settingsContent, v) |> ignore

    member this.SelectedSource
        with get () = selectedSource
        and set v = this.RaiseAndSetIfChanged(&selectedSource, v) |> ignore

    member this.Sources: ObservableCollection<MavenSourceItem> = sources

    member this.LoadData() =
        // Load Maven version
        this.CurrentVersion <-
            match MavenService.GetVersion() with
            | Some(v) -> v
            | None -> "Not found"

        // Load Maven home
        this.MavenHome <-
            match MavenService.GetMavenHome() with
            | Some(h) -> h
            | None -> "Not configured"

        // Load settings content
        this.SettingsContent <-
            match MavenService.GetSettingsContent() with
            | Some(c) -> c
            | None -> ""

        // Load sources
        sources.Clear()
        MavenService.GetSources()
        |> List.iter (fun s -> sources.Add(s))

    member this.SaveSettings() =
        MavenService.SaveSettings(this.SettingsContent)

    member this.AddSource(id: string, name: string, url: string) =
        let source = { Id = id; Name = name; Url = url; IsDefault = false }
        if MavenService.AddSource(source) then
            sources.Clear()
            MavenService.GetSources() |> List.iter (fun s -> sources.Add(s))

    member this.RemoveSource() =
        match selectedSource with
        | None -> ()
        | Some(source) ->
            if MavenService.RemoveSource(source.Id) then
                sources.Clear()
                MavenService.GetSources() |> List.iter (fun s -> sources.Add(s))
                this.SelectedSource <- None

    member this.SetDefaultSource() =
        match selectedSource with
        | None -> ()
        | Some(source) ->
            if MavenService.SetDefaultSource(source.Id) then
                sources.Clear()
                MavenService.GetSources() |> List.iter (fun s -> sources.Add(s))