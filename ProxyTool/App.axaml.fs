namespace ProxyTool

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open ProxyTool.ViewModels
open ProxyTool.Views

type App() as this =
    inherit Application()

    let mutable showStatus = Unchecked.defaultof<Boolean>

    do this.ShowStatus <- true

    override this.Initialize() = AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow(DataContext = MainWindowViewModel())
        | :? ISingleViewApplicationLifetime as singleViewLifetime ->
            singleViewLifetime.MainView <- MainView(DataContext = MainWindowViewModel())
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

    member this.ShowStatus
        with get () = showStatus
        and set v = showStatus <- v

    member this.ShowOrHide (sender: obj) (args: EventArgs) =
        let desktop = this.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime

        if this.ShowStatus then
            desktop.Windows |> Seq.iter (fun (w: Window) -> w.Hide())
            this.ShowStatus <- false
        else
            desktop.Windows |> Seq.iter (fun (w: Window) -> w.Show())
            this.ShowStatus <- true
