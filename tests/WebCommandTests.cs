using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

public class WebCommandTests
{
    [Fact]
    public void WebGetCommand_WaitForSelector_ShouldBeSet()
    {
        // Arrange
        var args = new[] { "web", "get", "https://example.com", "--wait-for", "#main-content" };

        // Act
        var success = CommandLineOptions.Parse(args, out var options, out var ex);

        // Assert
        Assert.True(success);
        Assert.Null(ex);
        Assert.Single(options.Commands);
        var command = options.Commands[0] as WebGetCommand;
        Assert.NotNull(command);
        Assert.Equal("#main-content", command.WaitForSelector);
    }

    [Fact]
    public void WebGetCommand_WaitForSelector_ShouldThrowOnMissingValue()
    {
        // Arrange
        var args = new[] { "web", "get", "https://example.com", "--wait-for" };

        // Act
        var success = CommandLineOptions.Parse(args, out var options, out var ex);

        // Assert
        Assert.False(success);
        Assert.NotNull(ex);
        Assert.Contains("Missing selector for --wait-for", ex.Message);
    }
}