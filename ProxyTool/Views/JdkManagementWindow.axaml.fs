namespace ProxyTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type JdkManagementWindow() as this =
    inherit Window()

    do this.Width <- 800
    do this.Height <- 450
    do this.CanResize <- false
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.WindowStartupLocation <- WindowStartupLocation.CenterScreen
