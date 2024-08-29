namespace ProxyTool.Views

open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open ProxyTool.ViewModels


type ProxyConfigView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)

    member this.OpenJdkMgmtWin (sender: obj) (args: RoutedEventArgs) =
        let win = JdkManagementWindow(DataContext = JavaConfigWindowViewModel())
        win.Show()
