using System;
using System.IO;
using mdx.Commands;
using mdx.Exporters;
using NUnit.Framework;

[TestFixture]
public class ExportCommandTests
{
    private string _testDir;
    private string _inputFile;
    private string _outputFile;

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mdx-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        _inputFile = Path.Combine(_testDir, "input.md");
        _outputFile = Path.Combine(_testDir, "output.pdf");

        // Create a test markdown file
        File.WriteAllText(_inputFile, "# Test Heading\nTest content");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Test]
    public void ExportCommand_ValidateThrowsWhenFormatMissing()
    {
        var cmd = new ExportCommand { OutputPath = "test.pdf", Files = { "input.md" } };
        Assert.Throws<CommandLineException>(() => cmd.Validate());
    }

    [Test]
    public void ExportCommand_ValidateThrowsWhenOutputMissing()
    {
        var cmd = new ExportCommand { Format = "pdf", Files = { "input.md" } };
        Assert.Throws<CommandLineException>(() => cmd.Validate());
    }

    [Test]
    public void ExportCommand_ValidateThrowsWhenNoFiles()
    {
        var cmd = new ExportCommand { Format = "pdf", OutputPath = "test.pdf" };
        Assert.Throws<CommandLineException>(() => cmd.Validate());
    }

    [Test]
    public void ExportCommand_ValidateThrowsWhenFormatInvalid()
    {
        var cmd = new ExportCommand { Format = "invalid", OutputPath = "test.pdf", Files = { "input.md" } };
        Assert.Throws<CommandLineException>(() => cmd.Validate());
    }

    [Test]
    public void ExportCommand_Execute_CreatesOutputFile()
    {
        var cmd = new ExportCommand 
        { 
            Format = "pdf", 
            OutputPath = _outputFile,
            Files = { _inputFile }
        };

        cmd.Execute();

        Assert.That(File.Exists(_outputFile));
        Assert.That(new FileInfo(_outputFile).Length, Is.GreaterThan(0));
    }
}