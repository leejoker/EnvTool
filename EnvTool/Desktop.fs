namespace EnvTool

open System
open Avalonia
open Avalonia.Logging
open ReactiveUI.Avalonia
open EnvTool

module Program =
    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI(fun _ -> ())
            .UseWin32()
            .UseSkia()

    [<EntryPoint; STAThread>]
    let main argv =
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
