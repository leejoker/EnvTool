namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Interactivity

type AddSourceDialogWindow() as this =
    inherit Window()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)

    member private this.OnOkClick(sender: obj, e: RoutedEventArgs) =
        this.Close(true)

    member private this.OnCancelClick(sender: obj, e: RoutedEventArgs) =
        this.Close(false)
