namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Interactivity
open EnvTool.ViewModels


type MavenManagementView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)

    member this.OnOpenAddDialog(sender: obj, e: RoutedEventArgs) =
        let dialog = new AddSourceDialogWindow()
        let vm = this.DataContext :?> MavenManagementViewModel
        dialog.DataContext <- vm
        let result = dialog.ShowDialog<bool>(this)
        result |> ignore