namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open System.Windows.Input
open EnvTool.Services
open ReactiveUI

type MavenSourceItem = {
    Id: string
    Name: string
    Url: string
    IsDefault: bool
}

type MavenManagementViewModel() as this =
    inherit ViewModelBase()

    let mutable currentVersion = ""
    let mutable mavenHome = ""
    let mutable settingsContent = ""
    let mutable selectedSource: MavenSourceItem option = None
    let sources = ObservableCollection<MavenSourceItem>()

    let loadDataCmd = ReactiveCommand.Create(Action(this.LoadData))
    let saveSettingsCmd = ReactiveCommand.Create(Action(this.SaveSettings))
    let removeSourceCmd = ReactiveCommand.Create(Action(this.RemoveSource))
    let setDefaultSourceCmd = ReactiveCommand.Create(Action(this.SetDefaultSource))

    member this.LoadDataCommand: ICommand = loadDataCmd
    member this.SaveSettingsCommand: ICommand = saveSettingsCmd
    member this.RemoveSourceCommand: ICommand = removeSourceCmd
    member this.SetDefaultSourceCommand: ICommand = setDefaultSourceCmd

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
        this.CurrentVersion <-
            match MavenService.GetVersion() with
            | Some(v) -> v
            | None -> "Not found"

        this.MavenHome <-
            match MavenService.GetMavenHome() with
            | Some(h) -> h
            | None -> "Not configured"

        this.SettingsContent <-
            match MavenService.GetSettingsContent() with
            | Some(c) -> c
            | None -> ""

        sources.Clear()
        MavenService.GetSources()
        |> List.iter (fun (s: MavenSource) ->
            let item = { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }
            sources.Add(item))

    member this.SaveSettings() =
        MavenService.SaveSettings(this.SettingsContent)

    member this.AddSource(id: string, name: string, url: string) =
        let source = { Id = id; Name = name; Url = url; IsDefault = false }
        let mavenSource: MavenSource = { Id = source.Id; Name = source.Name; Url = source.Url; IsDefault = source.IsDefault }
        if MavenService.AddSource(mavenSource) then
            sources.Clear()
            MavenService.GetSources()
            |> List.iter (fun (s: MavenSource) ->
                let item = { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }
                sources.Add(item))

    member this.RemoveSource() =
        match selectedSource with
        | None -> ()
        | Some(source) ->
            if MavenService.RemoveSource(source.Id) then
                sources.Clear()
                MavenService.GetSources()
                |> List.iter (fun (s: MavenSource) ->
                    let item = { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }
                    sources.Add(item))
                this.SelectedSource <- None

    member this.SetDefaultSource() =
        match selectedSource with
        | None -> ()
        | Some(source) ->
            if MavenService.SetDefaultSource(source.Id) then
                sources.Clear()
                MavenService.GetSources()
                |> List.iter (fun (s: MavenSource) ->
                    let item = { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }
                    sources.Add(item))
