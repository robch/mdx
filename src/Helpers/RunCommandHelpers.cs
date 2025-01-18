using System;
using System.Diagnostics;
using System.Threading.Tasks;

static class RunCommandHelpers
{
    public static async Task<string> ExecuteCommand(RunCommand command)
    {
        var startInfo = new ProcessStartInfo();
        
        switch (command.Type)
        {
            case RunCommand.ScriptType.Cmd:
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c {command.ScriptToRun}";
                break;
            case RunCommand.ScriptType.Bash:
                startInfo.FileName = "bash";
                startInfo.Arguments = $"-c \"{command.ScriptToRun}\"";
                break;
            case RunCommand.ScriptType.PowerShell:
                startInfo.FileName = "powershell.exe";
                startInfo.Arguments = $"-Command \"{command.ScriptToRun}\"";
                break;
            case RunCommand.ScriptType.Default:
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/c {command.ScriptToRun}";
                }
                else
                {
                    startInfo.FileName = "bash";
                    startInfo.Arguments = $"-c \"{command.ScriptToRun}\"";
                }
                break;
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed with exit code {process.ExitCode}. Error: {error}");
        }

        return string.IsNullOrEmpty(error) ? output : $"{output}\nErrors:\n{error}";
    }
}
