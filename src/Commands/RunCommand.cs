using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class RunCommand : Command
{
    public enum ScriptType
    {
        Default, // Uses cmd on Windows, bash on Linux/Mac
        Cmd,
        Bash,
        PowerShell
    }

    public RunCommand() : base()
    {
        ScriptToRun = string.Empty;
        Type = ScriptType.Default;
        TimeoutMilliseconds = -1; // No timeout by default
    }

    public override bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(ScriptToRun);
    }

    public override string GetCommandName()
    {
        return "run";
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
    public int TimeoutMilliseconds { get; set; }

    public async Task<(bool success, string output)> ExecuteWithTimeout()
    {
        using var process = new Process();
        var isWindows = OperatingSystem.IsWindows();

        process.StartInfo.FileName = Type switch
        {
            ScriptType.Cmd => "cmd",
            ScriptType.PowerShell => "powershell",
            ScriptType.Bash => "bash",
            _ => isWindows ? "cmd" : "bash"
        };

        process.StartInfo.Arguments = Type switch
        {
            ScriptType.Cmd => $"/c {ScriptToRun}",
            ScriptType.PowerShell => $"-Command {ScriptToRun}",
            ScriptType.Bash => $"-c {ScriptToRun}",
            _ => isWindows ? $"/c {ScriptToRun}" : $"-c {ScriptToRun}"
        };

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        var output = string.Empty;
        var error = string.Empty;

        process.OutputDataReceived += (sender, e) => 
        {
            if (e.Data != null)
                output += e.Data + Environment.NewLine;
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                error += e.Data + Environment.NewLine;
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (TimeoutMilliseconds > 0)
            {
                // Wait for the process to exit or timeout
                if (!await Task.Run(() => process.WaitForExit(TimeoutMilliseconds)))
                {
                    // Try graceful termination first
                    process.CloseMainWindow();
                    
                    // Wait a bit for graceful termination
                    if (!process.WaitForExit(1000))
                    {
                        // Send Ctrl+C signal
                        if (!process.HasExited)
                        {
                            try
                            {
                                if (isWindows)
                                {
                                    process.Kill(true); // true means the process tree is killed
                                }
                                else
                                {
                                    // Send SIGINT (Ctrl+C equivalent)
                                    Process.Start("kill", $"-SIGINT {process.Id}");
                                    
                                    // Wait a bit for SIGINT to take effect
                                    if (!process.WaitForExit(1000))
                                    {
                                        process.Kill(); // Force kill if still running
                                    }
                                }
                            }
                            catch
                            {
                                // If Ctrl+C fails, force kill
                                process.Kill();
                            }
                        }
                    }

                    return (false, $"Process timed out after {TimeoutMilliseconds}ms. Output so far:\n{output}\nError output:\n{error}");
                }
            }
            else
            {
                process.WaitForExit();
            }

            return (process.ExitCode == 0, output + error);
        }
        catch (Exception ex)
        {
            return (false, $"Error executing process: {ex.Message}\nOutput so far:\n{output}\nError output:\n{error}");
        }
    }
}
