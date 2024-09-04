namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml


type JdkManagementView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)
