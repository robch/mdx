using System;
using System.IO;
using System.Linq;

public class ExportCommand : Command
{
    public override string[] Names => new[] { "export" };

    public override string Description => "Export markdown to other formats (PDF/DOCX/PPTX)";

    public override string ExtendedDescription => @"Exports markdown files to various document formats.

Examples:
    mdx export document.md document.pdf
    mdx export notes.md presentation.pptx
    mdx export report.md report.docx

Supported output formats: " + string.Join(", ", MarkdownExporters.GetSupportedExtensions());

    public override void Run(CommandLineOptions options)
    {
        if (options.Arguments.Count != 2)
        {
            throw new CommandLineException(this, "Export command requires 2 arguments: <input.md> <output.ext>");
        }

        var inputPath = options.Arguments[0];
        var outputPath = options.Arguments[1];

        if (!File.Exists(inputPath))
        {
            throw new CommandLineException(this, $"Input file not found: {inputPath}");
        }

        if (!inputPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new CommandLineException(this, $"Input file must be a markdown file (.md extension)");
        }

        try
        {
            var markdown = File.ReadAllText(inputPath);
            var exporter = MarkdownExporters.GetExporter(outputPath);
            exporter.ExportMarkdown(markdown, outputPath);
            Console.WriteLine($"Successfully exported to {outputPath}");
        }
        catch (ArgumentException ex)
        {
            throw new CommandLineException(this, ex.Message);
        }
        catch (Exception ex)
        {
            throw new CommandLineException(this, $"Export failed: {ex.Message}");
        }
    }
}