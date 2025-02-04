using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class RunCommandStdinTests
{
    [Fact]
    public void Parse_WithStdinFlag_SetsUseStdinRedirection()
    {
        // Arrange
        var args = new[] { "run", "--stdin", "cat" };

        // Act
        var success = CommandLineOptions.Parse(args, out var options, out var ex);
        var runCommand = options.Commands[0] as RunCommand;

        // Assert
        Assert.True(success);
        Assert.NotNull(runCommand);
        Assert.True(runCommand.UseStdinRedirection);
    }

    [Fact]
    public void Parse_WithoutStdinFlag_DoesNotSetUseStdinRedirection()
    {
        // Arrange
        var args = new[] { "run", "cat" };

        // Act
        var success = CommandLineOptions.Parse(args, out var options, out var ex);
        var runCommand = options.Commands[0] as RunCommand;

        // Assert
        Assert.True(success);
        Assert.NotNull(runCommand);
        Assert.False(runCommand.UseStdinRedirection);
    }
}