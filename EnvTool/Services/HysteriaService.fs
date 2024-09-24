namespace EnvTool.Services

open System.Diagnostics
open EnvTool.Utils.SysInfo

module HysteriaService =

    type ProcessStatusTuple = bool * Option<Process>

    let StartHysteriaProcess (exec: string) (config: string) : ProcessStatusTuple =
        let p = new Process()
#if Windows
        p.StartInfo.FileName <- $"\"{exec}\""
        p.StartInfo.Arguments <- $"-c \"{config}\""
#else
        p.StartInfo.FileName <- exec
        p.StartInfo.Arguments <- $"-c {config}"
#endif
        p.StartInfo.WorkingDirectory <- "/"
        p.StartInfo.UseShellExecute <- false
        p.StartInfo.RedirectStandardInput <- true
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.CreateNoWindow <- true
        p.Start() |> ignore

        if p.WaitForExit(1000) then
            p.CancelErrorRead()
            (false, None)
        else
            (true, Some(p))

    let rec KillHysteriaProcess (proc: Option<Process>) (exec: string) =
        match proc with
        | Some(p) ->
            p.Kill()
            p.WaitForExit(100) |> ignore

            if p.HasExited |> not then
                p.Kill()
                p.WaitForExit(100) |> ignore
        | None ->
            let optProcess = GetProcessByPath exec
            KillHysteriaProcess optProcess exec
