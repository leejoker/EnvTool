namespace EnvTool.Views

open Avalonia.Controls
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open EnvTool.ViewModels


type ProxyConfigView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)

    member this.OpenJdkMgmtWin (sender: obj) (args: RoutedEventArgs) =
        let win = JavaConfigWindow(DataContext = JavaConfigWindowViewModel())
        win.Show()
