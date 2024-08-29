namespace ProxyTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainWindow() as this =
    inherit Window()

    do this.Height <- 450
    do this.Width <- 300
    do this.CanResize <- false
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.WindowStartupLocation <- WindowStartupLocation.CenterScreen
