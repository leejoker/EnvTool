namespace ProxyTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml


type ProxyConfigView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)
