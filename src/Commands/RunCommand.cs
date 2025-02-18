using System;
using System.Collections.Generic;

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
        EnvironmentVariables = new Dictionary<string, string>();
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
        // Validate environment variable names
        foreach (var envVar in EnvironmentVariables)
        {
            if (string.IsNullOrWhiteSpace(envVar.Key))
            {
                throw new CommandLineException("Environment variable names cannot be empty", this);
            }

            // Check for invalid characters in env var names
            if (envVar.Key.Contains("=") || envVar.Key.Contains(" ") || envVar.Key.Contains("\t"))
            {
                throw new CommandLineException($"Environment variable name '{envVar.Key}' contains invalid characters", this);
            }
        }

        return this;
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; }
}
