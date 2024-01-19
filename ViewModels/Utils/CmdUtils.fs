namespace ProxyTool.ViewModels.Utils

open System.Diagnostics
open System

module CmdUtils =
    let private SetProcessInfo (p: Process) : Process =
        p.StartInfo.FileName <- "cmd.exe"
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardInput <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.CreateNoWindow <- true
        p.Start() |> ignore
        p

    let private RunCmd (p: Process, command: string) : Process =
        p.StandardInput.WriteLine(command + "&exit")
        p.StandardInput.AutoFlush <- true
        p

    let RunCmdCommand cmd : String =
        let p = new Process()

        let output =
            p
            |> SetProcessInfo
            |> (fun p -> RunCmd(p, cmd))
            |> fun p -> p.StandardOutput.ReadToEnd()

        p.WaitForExit()
        p.Close()
        output
