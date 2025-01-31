using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class RunCommandTests
{
    [Fact]
    public void RunCommand_ShouldInitializeWithEmptyEnvironmentVariables()
    {
        var command = new RunCommand();
        Assert.NotNull(command.EnvironmentVariables);
        Assert.Empty(command.EnvironmentVariables);
    }

    [Fact]
    public void CommandLineOptions_ShouldParseEnvironmentVariables()
    {
        var args = new[] { "run", "-e", "KEY1=value1", "--env", "KEY2=value2", "echo test" };
        var success = CommandLineOptions.Parse(args, out var options, out var ex);

        Assert.True(success);
        Assert.Null(ex);
        Assert.Single(options.Commands);

        var runCommand = options.Commands[0] as RunCommand;
        Assert.NotNull(runCommand);
        Assert.Equal(2, runCommand.EnvironmentVariables.Count);
        Assert.Equal("value1", runCommand.EnvironmentVariables["KEY1"]);
        Assert.Equal("value2", runCommand.EnvironmentVariables["KEY2"]);
    }

    [Fact]
    public void CommandLineOptions_ShouldThrowOnInvalidEnvironmentVariableFormat()
    {
        var args = new[] { "run", "-e", "INVALID_FORMAT", "echo test" };
        var success = CommandLineOptions.Parse(args, out var options, out var ex);

        Assert.False(success);
        Assert.NotNull(ex);
        Assert.Contains("Invalid environment variable format", ex.Message);
    }

    [Fact]
    public void CommandLineOptions_ShouldThrowOnMissingEnvironmentVariableValue()
    {
        var args = new[] { "run", "-e", "echo test" };
        var success = CommandLineOptions.Parse(args, out var options, out var ex);

        Assert.False(success);
        Assert.NotNull(ex);
        Assert.Contains("Missing value for -e option", ex.Message);
    }

    [Theory]
    [InlineData("cmd")]
    [InlineData("bash")]
    [InlineData("powershell")]
    public async Task RunCommand_ShouldPassEnvironmentVariablesToProcess(string shell)
    {
        // Skip test if we're on Windows and testing bash, or on Unix and testing cmd/powershell
        if ((Environment.OSVersion.Platform == PlatformID.Win32NT && shell == "bash") ||
            (Environment.OSVersion.Platform != PlatformID.Win32NT && (shell == "cmd" || shell == "powershell")))
        {
            return;
        }

        var command = new RunCommand
        {
            Type = shell switch
            {
                "cmd" => RunCommand.ScriptType.Cmd,
                "bash" => RunCommand.ScriptType.Bash,
                "powershell" => RunCommand.ScriptType.PowerShell,
                _ => RunCommand.ScriptType.Default
            },
            EnvironmentVariables = new Dictionary<string, string>
            {
                { "TEST_VAR", "test_value" }
            }
        };

        // Use the appropriate echo syntax for each shell
        command.ScriptToRun = shell switch
        {
            "cmd" => "echo %TEST_VAR%",
            "bash" => "echo $TEST_VAR",
            "powershell" => "echo $env:TEST_VAR",
            _ => "echo %TEST_VAR%"
        };

        var (output, exitCode) = await ProcessHelpers.RunShellCommandAsync(
            command.ScriptToRun,
            shell,
            command.EnvironmentVariables);

        Assert.Equal(0, exitCode);
        Assert.Contains("test_value", output.Trim());
    }
}