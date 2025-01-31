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
        return this;
    }

    public string ScriptToRun { get; set; }
    public ScriptType Type { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; private set; }

    public void AddEnvironmentVariable(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Environment variable name cannot be empty", nameof(name));
        
        EnvironmentVariables[name] = value ?? string.Empty;
    }

    public void LoadEnvironmentVariablesFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        var lines = System.IO.File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split('=', 2);
            if (parts.Length == 2)
            {
                var name = parts[0].Trim();
                var value = parts[1].Trim();
                // Remove optional quotes around the value
                if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
                    value = value.Substring(1, value.Length - 2);
                
                AddEnvironmentVariable(name, value);
            }
        }
    }
}
