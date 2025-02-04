using System;
using System.Threading.Tasks;
using Xunit;

public class WebCommandIntegrationTests
{
    [Fact]
    public async Task WebGetCommand_WaitForSelector_ShouldWaitForElement()
    {
        // Arrange
        var command = new WebGetCommand();
        command.Urls.Add("https://example.com");
        command.WaitForSelector = "#main-content";
        command.GetContent = true;

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("main-content", result);
    }

    [Fact]
    public async Task WebGetCommand_WaitForSelector_ShouldHandleTimeout()
    {
        // Arrange
        var command = new WebGetCommand();
        command.Urls.Add("https://example.com");
        command.WaitForSelector = "#non-existent-element";
        command.GetContent = true;

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Timeout waiting for selector", result);
    }

    [Fact]
    public async Task WebGetCommand_WaitForSelector_ShouldWorkWithComplexSelectors()
    {
        // Arrange
        var command = new WebGetCommand();
        command.Urls.Add("https://example.com");
        command.WaitForSelector = "div.content > article:first-child";
        command.GetContent = true;

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("article", result);
    }
}