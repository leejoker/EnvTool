namespace EnvTool.ViewModels.Utils

open MsBox.Avalonia
open MsBox.Avalonia.Enums

module MessageBoxUtils =
    let CreateTipBox msg =
        MessageBoxManager.GetMessageBoxStandard("提示", msg, ButtonEnum.Ok)
        |> fun t -> t.ShowAsync()
