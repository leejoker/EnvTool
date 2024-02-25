namespace ProxyTool.Utils

open System.Diagnostics
open System

module CmdUtils =
    let private SetProcessInfo (p: Process) =
#if Windows
        let sys = "cmd.exe"
#else
        let sys = "/bin/sh"
#endif
        p.StartInfo.FileName <- sys
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardInput <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.CreateNoWindow <- true

        p.Start()
        |> function
            | true -> Ok p
            | false -> Error p

    let private RunCmd (p: Process, command: string) : Process =
        p.StandardInput.WriteLine(command + "&exit")
        p.StandardInput.AutoFlush <- true
        p

    let RunCmdCommand cmd : String =
        let p = new Process()

        let output =
            p
            |> SetProcessInfo
            |> function
                | Ok p -> RunCmd(p, cmd) |> fun p -> p.StandardOutput.ReadToEnd()
                | _ -> ""


        p.WaitForExit()
        p.Close()
        output
