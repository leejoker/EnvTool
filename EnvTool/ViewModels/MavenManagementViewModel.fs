namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open System.Windows.Input
open EnvTool.Services
open EnvTool.Utils.SysInfo
open ReactiveUI
open System.Reactive.Linq

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
    let mutable selectedTabIndex = 0
    let sources = ObservableCollection<MavenSourceItem>()

    let mutable isAddDialogOpen = false
    let mutable newSourceId = ""
    let mutable newSourceName = ""
    let mutable newSourceUrl = ""

    let showAddDialogCmd = ReactiveCommand.Create(Action(this.ShowAddDialog))
    let addSourceFromDialogCmd = ReactiveCommand.Create(Action(this.AddSourceFromDialog))
    let cancelAddDialogCmd = ReactiveCommand.Create(Action(this.CancelAddDialog))

    let loadDataCmd = ReactiveCommand.Create(Action(this.LoadData))
    let saveSettingsCmd = ReactiveCommand.Create(Action(this.SaveSettings))
    let removeSourceCmd = ReactiveCommand.Create(Action(this.RemoveSource))
    let setDefaultSourceCmd = ReactiveCommand.Create(Action(this.SetDefaultSource))

    do
        this.WhenAnyValue(fun (x: MavenManagementViewModel) -> x.SelectedTabIndex)
        |> Observable.filter (fun idx -> idx = 1)
        |> Observable.subscribe (fun _ -> this.LoadSettingsContent())
        |> ignore

        this.LoadData()

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

    member this.SelectedTabIndex
        with get () = selectedTabIndex
        and set v = this.RaiseAndSetIfChanged(&selectedTabIndex, v) |> ignore

    member this.Sources: ObservableCollection<MavenSourceItem> = sources

    member this.IsAddDialogOpen
        with get () = isAddDialogOpen
        and set v = this.RaiseAndSetIfChanged(&isAddDialogOpen, v) |> ignore

    member this.NewSourceId
        with get () = newSourceId
        and set v = this.RaiseAndSetIfChanged(&newSourceId, v) |> ignore

    member this.NewSourceName
        with get () = newSourceName
        and set v = this.RaiseAndSetIfChanged(&newSourceName, v) |> ignore

    member this.NewSourceUrl
        with get () = newSourceUrl
        and set v = this.RaiseAndSetIfChanged(&newSourceUrl, v) |> ignore

    member this.ShowAddDialogCommand: ICommand = showAddDialogCmd
    member this.AddSourceFromDialogCommand: ICommand = addSourceFromDialogCmd
    member this.CancelAddDialogCommand: ICommand = cancelAddDialogCmd

    member this.LoadData() =
        // Auto-detect and set MAVEN_HOME if not already set
        let detectedHome =
            match MavenService.GetMavenHome() with
            | Some(mavenHome) ->
                let currentMavenHome = Environment.GetEnvironmentVariable("MAVEN_HOME")
                if String.IsNullOrEmpty(currentMavenHome) then
                    ignore (SetUserEnvironmentVariable "MAVEN_HOME" mavenHome)
                Some(mavenHome)
            | None -> None

        this.MavenHome <- match detectedHome with Some(h) -> h | None -> "Not found"

        this.CurrentVersion <-
            match MavenService.GetVersion() with
            | Some(v) -> v
            | None -> "Not found"

        sources.Clear()
        MavenService.GetSources()
        |> List.iter (fun (s: MavenSource) ->
            let item = { Id = s.Id; Name = s.Name; Url = s.Url; IsDefault = s.IsDefault }
            sources.Add(item))

    member this.LoadSettingsContent() =
        this.SettingsContent <-
            match MavenService.GetSettingsContent() with
            | Some(c) -> c
            | None -> ""

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

    member this.ShowAddDialog() =
        this.IsAddDialogOpen <- true

    member this.CancelAddDialog() =
        this.IsAddDialogOpen <- false
        this.NewSourceId <- ""
        this.NewSourceName <- ""
        this.NewSourceUrl <- ""

    member this.AddSourceFromDialog() =
        if not (String.IsNullOrWhiteSpace(this.NewSourceId)) &&
           not (String.IsNullOrWhiteSpace(this.NewSourceName)) &&
           not (String.IsNullOrWhiteSpace(this.NewSourceUrl)) then
            this.AddSource(this.NewSourceId, this.NewSourceName, this.NewSourceUrl)
            this.IsAddDialogOpen <- false
            this.NewSourceId <- ""
            this.NewSourceName <- ""
            this.NewSourceUrl <- ""
