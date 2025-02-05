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
        Timeout = int.MaxValue;
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
        if (Timeout <= 0)
        {
            throw new CommandLineException("Timeout value must be greater than 0 milliseconds");
        }
        return this;
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
    public int Timeout { get; set; } // Timeout in milliseconds
}
