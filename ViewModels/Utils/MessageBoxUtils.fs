namespace ProxyTool.ViewModels.Utils

open System.Diagnostics
open System
open MsBox.Avalonia
open MsBox.Avalonia.Enums

module MessageBoxUtils =
    let CreateTipBox msg =
        MessageBoxManager.GetMessageBoxStandard("提示", msg, ButtonEnum.Ok)
        |> fun t -> t.ShowAsync()
