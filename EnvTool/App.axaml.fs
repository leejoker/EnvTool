namespace EnvTool

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open EnvTool.ViewModels
open EnvTool.Views

type App() as this =
    inherit Application()

    do MainWindow.HideState <- false

    override this.Initialize() = AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow(DataContext = MainWindowViewModel())
        | :? ISingleViewApplicationLifetime as singleViewLifetime ->
            singleViewLifetime.MainView <- MainView(DataContext = MainWindowViewModel())
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

    member this.ShowOrHide (sender: obj) (args: EventArgs) =
        let desktop = this.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime

        if not MainWindow.HideState then
            desktop.Windows |> Seq.iter (fun (w: Window) -> w.Hide())
            MainWindow.HideState <- true
        else
            desktop.Windows
            |> Seq.iter (fun (w: Window) ->
                w.Show()
                w.WindowState <- WindowState.Normal)

            MainWindow.HideState <- false

    member this.Exit (sender: obj) (args: EventArgs) =
        let desktop = this.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime
        desktop.TryShutdown() |> ignore
