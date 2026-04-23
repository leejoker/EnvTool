namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type MavenManagementWindow() as this =
    inherit Window()

    do this.Width <- 900
    do this.Height <- 650
    do this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        this.WindowStartupLocation <- WindowStartupLocation.CenterScreen