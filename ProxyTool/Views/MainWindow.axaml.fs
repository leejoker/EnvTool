namespace ProxyTool.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainWindow() as this =
    inherit Window()

    static let mutable hideState = Unchecked.defaultof<bool>

    do this.Height <- 450
    do this.Width <- 300
    do this.CanResize <- false
    do this.InitializeComponent()

    do
        this
            .GetObservable(Window.WindowStateProperty)
            .Subscribe(fun state ->
                if state = WindowState.Minimized then
                    MainWindow.HideState <- true
                    this.Hide())
        |> ignore

    static member HideState
        with get () = hideState
        and set v = hideState <- v

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.WindowStartupLocation <- WindowStartupLocation.CenterScreen
