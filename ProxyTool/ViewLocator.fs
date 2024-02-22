namespace ProxyTool

open Avalonia.Controls.Templates
open System
open Avalonia.Controls
open ProxyTool.ViewModels

type ViewLocator() =
    interface IDataTemplate with
        member this.Build(data: obj) : Avalonia.Controls.Control =
            data.GetType().FullName
            |> fun x ->
                match x with
                | _ when x <> null ->
                    x.Replace("ViewModel", "View")
                    |> Type.GetType
                    |> Activator.CreateInstance
                    |> fun x -> x :?> Control
                | _ ->
                    let t = new TextBlock()
                    t.Text <- "Not Found: " + x
                    t

        member this.Match(data: obj) : bool = data :? ViewModelBase
