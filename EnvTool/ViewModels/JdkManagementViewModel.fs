namespace EnvTool.ViewModels

open System
open System.Collections.ObjectModel
open System.IO
open System.Windows.Input
open EnvTool.Services
open Newtonsoft.Json.Linq
open ReactiveUI

type JdkItem = { Distro: string; Version: string; Path: string; IsCurrent: bool }

type JdkManagementViewModel() as this =
    inherit ViewModelBase()

    let installedJdks = ObservableCollection<JdkItem>()
    let availableJdks = ObservableCollection<JdkItem>()
    let mutable selectedJdk: JdkItem option = None
    let mutable currentVersion = ""
    let mutable isLoading = false

    let mutable availableVersionList: JObject option = None

    let installCmd = ReactiveCommand.Create(Action(this.InstallSelected))
    let useCmd = ReactiveCommand.Create(Action(this.UseSelected))
    let removeCmd = ReactiveCommand.Create(Action(this.RemoveSelected))
    let refreshCmd = ReactiveCommand.Create(Action(this.RefreshInstalled))
    let loadAvailCmd = ReactiveCommand.Create(Action(this.LoadAvailableJdks))

    member this.InstallSelectedCommand: ICommand = installCmd
    member this.UseSelectedCommand: ICommand = useCmd
    member this.RemoveSelectedCommand: ICommand = removeCmd
    member this.RefreshInstalledCommand: ICommand = refreshCmd
    member this.LoadAvailableJdksCommand: ICommand = loadAvailCmd

    member this.InstalledJdks: ObservableCollection<JdkItem> = installedJdks
    member this.AvailableJdks: ObservableCollection<JdkItem> = availableJdks

    member this.SelectedJdk
        with get () = selectedJdk
        and set v = this.RaiseAndSetIfChanged(&selectedJdk, v) |> ignore

    member this.CurrentVersion
        with get () = currentVersion
        and set v = this.RaiseAndSetIfChanged(&currentVersion, v) |> ignore

    member this.IsLoading
        with get () = isLoading
        and set v = this.RaiseAndSetIfChanged(&isLoading, v) |> ignore

    member this.RefreshInstalled() =
        this.IsLoading <- true

        try
            installedJdks.Clear()

            let current = JpvmModule.Current()
            this.CurrentVersion <-
                if String.IsNullOrEmpty current.version then
                    "No JDK configured"
                else
                    $"{current.distro} {current.version}"

            if Directory.Exists(JpvmModule.JDK_PATH) then
                let sysOs = EnvTool.Utils.SysInfo.SysOS
                let sysArch = EnvTool.Utils.SysInfo.SysArch

                Directory.EnumerateDirectories(JpvmModule.JDK_PATH)
                |> Seq.iter (fun distroPath ->
                    let distro = Path.GetFileName(distroPath)

                    Directory.EnumerateDirectories(distroPath)
                    |> Seq.iter (fun versionPath ->
                        let version = Path.GetFileName(versionPath)

                        let osArchPath = Path.Combine(versionPath, sysOs, sysArch)

                        if Directory.Exists(osArchPath) then
                            let jdkBinPath =
                                Directory.EnumerateFiles(osArchPath, "java.exe", SearchOption.AllDirectories)
                                |> Seq.tryHead
                                |> Option.map (fun _ -> osArchPath)
                                |> Option.orElse (
                                    Directory.EnumerateFiles(osArchPath, "java", SearchOption.AllDirectories)
                                    |> Seq.tryHead
                                    |> Option.map (fun _ -> osArchPath)
                                )

                            match jdkBinPath with
                            | Some(path) ->
                                let isCurrent =
                                    not (String.IsNullOrEmpty current.distro)
                                    && current.distro = distro
                                    && not (String.IsNullOrEmpty current.version)
                                    && current.version = version
                                let item = { Distro = distro; Version = version; Path = path; IsCurrent = isCurrent }
                                installedJdks.Add(item)
                            | None -> ()
                        else
                            // Try parent directory for older structure
                            let packagePath = Path.Combine(versionPath, $"{distro}-{version}")
                            if Directory.Exists(packagePath) then
                                let isCurrent =
                                    not (String.IsNullOrEmpty current.distro)
                                    && current.distro = distro
                                    && not (String.IsNullOrEmpty current.version)
                                    && current.version = version
                                let item = { Distro = distro; Version = version; Path = packagePath; IsCurrent = isCurrent }
                                installedJdks.Add(item)
                            else
                                let isCurrent =
                                    not (String.IsNullOrEmpty current.distro)
                                    && current.distro = distro
                                    && not (String.IsNullOrEmpty current.version)
                                    && current.version = version
                                let item = { Distro = distro; Version = version; Path = versionPath; IsCurrent = isCurrent }
                                installedJdks.Add(item)
                    )
                )
        finally
            this.IsLoading <- false

    member this.LoadAvailableJdks() =
        this.IsLoading <- true

        try
            availableJdks.Clear()

            let progress = { new IProgress<double> with member __.Report(_) = () }
            JpvmModule.DownloadVersionList(progress)

            if File.Exists(JpvmModule.VERSION_PATH) then
                let json = JObject.Parse(File.ReadAllText(JpvmModule.VERSION_PATH))
                availableVersionList <- Some(json)

                let sysOs = EnvTool.Utils.SysInfo.SysOS
                let sysArch = EnvTool.Utils.SysInfo.SysArch

                json.Properties()
                |> Seq.iter (fun distroProp ->
                    let distro = distroProp.Name

                    distroProp.Value
                    |> function
                    | :? JObject as versionsObj ->
                        versionsObj.Properties()
                        |> Seq.iter (fun versionProp ->
                            let version = versionProp.Name

                            match versionProp.Value with
                            | :? JObject as osArchObj ->
                                if osArchObj.[sysOs] <> null && osArchObj.[sysOs].[sysArch] <> null then
                                    let item = { Distro = distro; Version = version; Path = ""; IsCurrent = false }
                                    availableJdks.Add(item)
                            | _ -> ()
                        )
                    | _ -> ()
                )
        finally
            this.IsLoading <- false

    member this.InstallSelected() =
        match selectedJdk with
        | None -> ()
        | Some(jdk) when String.IsNullOrEmpty(jdk.Path) ->
            // This is an available JDK to install
            let jdkInfo = { JdkVersionInfo.distro = jdk.Distro; JdkVersionInfo.version = jdk.Version }
            let progress = { new IProgress<double> with member __.Report(_) = () }

            try
                this.IsLoading <- true
                JpvmModule.Install(jdkInfo, progress)
                this.RefreshInstalled()
            finally
                this.IsLoading <- false
        | _ -> ()

    member this.UseSelected() =
        match selectedJdk with
        | None -> ()
        | Some(jdk) ->
            if jdk.IsCurrent then ()
            else
                let jdkInfo = { JdkVersionInfo.distro = jdk.Distro; JdkVersionInfo.version = jdk.Version }
                let result = JpvmModule.Use(jdkInfo)

                if result then
                    this.RefreshInstalled()

    member this.RemoveSelected() =
        match selectedJdk with
        | None -> ()
        | Some(jdk) ->
            if jdk.IsCurrent then () // Cannot remove current JDK
            else
                let jdkInfo = { JdkVersionInfo.distro = jdk.Distro; JdkVersionInfo.version = jdk.Version }
                let result = JpvmModule.Remove(jdkInfo)

                if result then
                    this.RefreshInstalled()

    member this.Initialize() =
        this.RefreshInstalled()
        this.LoadAvailableJdks()
