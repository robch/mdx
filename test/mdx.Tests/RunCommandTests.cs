using Xunit;

public class RunCommandTests
{
    [Fact]
    public void AddEnvironmentVariable_ValidInput_AddsToEnvironmentVariables()
    {
        // Arrange
        var command = new RunCommand();
        var name = "TEST_VAR";
        var value = "test_value";

        // Act
        command.AddEnvironmentVariable(name, value);

        // Assert
        Assert.Equal(value, command.EnvironmentVariables[name]);
    }

    [Fact]
    public void AddEnvironmentVariable_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var command = new RunCommand();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => command.AddEnvironmentVariable("", "test_value"));
        Assert.Throws<ArgumentException>(() => command.AddEnvironmentVariable(null, "test_value"));
    }

    [Fact]
    public void LoadEnvironmentVariablesFromFile_ValidFile_LoadsVariables()
    {
        // Arrange
        var command = new RunCommand();
        var filePath = Path.GetTempFileName();
        File.WriteAllLines(filePath, new[]
        {
            "# Comment line",
            "TEST_VAR1=value1",
            "TEST_VAR2=\"value 2\"",
            "",
            "TEST_VAR3=value3"
        });

        try
        {
            // Act
            command.LoadEnvironmentVariablesFromFile(filePath);

            // Assert
            Assert.Equal("value1", command.EnvironmentVariables["TEST_VAR1"]);
            Assert.Equal("value 2", command.EnvironmentVariables["TEST_VAR2"]);
            Assert.Equal("value3", command.EnvironmentVariables["TEST_VAR3"]);
            Assert.Equal(3, command.EnvironmentVariables.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void LoadEnvironmentVariablesFromFile_EmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var command = new RunCommand();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => command.LoadEnvironmentVariablesFromFile(""));
        Assert.Throws<ArgumentException>(() => command.LoadEnvironmentVariablesFromFile(null));
    }

    [Fact]
    public void LoadEnvironmentVariablesFromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var command = new RunCommand();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => command.LoadEnvironmentVariablesFromFile(nonExistentPath));
    }

    [Fact]
    public async Task RunCommand_WithEnvironmentVariables_PassesVariablesToProcess()
    {
        // Arrange
        var command = new RunCommand();
        command.AddEnvironmentVariable("TEST_VAR", "test_value");

        // Set script based on OS
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        command.ScriptToRun = isWindows
            ? "echo %TEST_VAR%"
            : "echo $TEST_VAR";

        // Act
        var (output, exitCode) = await ProcessHelpers.RunShellCommandAsync(
            command.ScriptToRun,
            null,
            environmentVariables: command.EnvironmentVariables);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("test_value", output.Trim());
    }
}