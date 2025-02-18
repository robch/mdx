using System;

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
        WorkingDirectory = string.Empty;
    }

    override public string GetCommandName()
    {
        return "run";
    }

    override public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(ScriptToRun);
    }

    override public Command Validate()
    {
        if (!string.IsNullOrEmpty(WorkingDirectory) && !System.IO.Directory.Exists(WorkingDirectory))
        {
            throw new CommandLineException($"Working directory '{WorkingDirectory}' does not exist");
        }
        return this;
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
    public string WorkingDirectory { get; set; }
}
