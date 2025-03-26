using System;
using System.IO;
using mdx.Exporters;
using NUnit.Framework;

[TestFixture]
public class MarkdownExporterTests
{
    private string _testDir;

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "mdx-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
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
    public void PdfExporter_CreatesValidPdf()
    {
        var exporter = new PdfMarkdownExporter();
        var outputPath = Path.Combine(_testDir, "test.pdf");
        
        exporter.Export("# Test\nContent", outputPath);
        
        Assert.That(File.Exists(outputPath));
        Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
    }

    [Test]
    public void DocxExporter_CreatesValidDocx()
    {
        var exporter = new DocxMarkdownExporter();
        var outputPath = Path.Combine(_testDir, "test.docx");
        
        exporter.Export("# Test\nContent", outputPath);
        
        Assert.That(File.Exists(outputPath));
        Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
    }

    [Test]
    public void PptxExporter_CreatesValidPptx()
    {
        var exporter = new PptxMarkdownExporter();
        var outputPath = Path.Combine(_testDir, "test.pptx");
        
        exporter.Export("# Test\nContent", outputPath);
        
        Assert.That(File.Exists(outputPath));
        Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
    }
}