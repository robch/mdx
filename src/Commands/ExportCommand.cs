using System;
using System.Collections.Generic;
using System.IO;
using mdx.Exporters;

namespace mdx.Commands;

public class ExportCommand : Command
{
    private static readonly MarkdownExporters Exporters = new();

    public string Format { get; set; }
    public string OutputPath { get; set; }
    public List<string> Files { get; set; } = new();

    public override string GetCommandName() => "export";

    public override string GetUsage() => 
        "mdx export [options] <files...>\n" +
        "Options:\n" +
        "  --format    Output format (pdf, docx, or pptx)\n" +
        "  --output    Output file path\n\n" +
        "Example:\n" +
        "  mdx export --format pdf --output output.pdf input.md";

    public override bool IsEmpty() => Files.Count == 0;

    public override Command Validate()
    {
        if (string.IsNullOrEmpty(Format))
        {
            throw new CommandLineException("Export format must be specified with --format");
        }

        if (!Exporters.SupportedFormats.Contains(Format.ToLowerInvariant()))
        {
            throw new CommandLineException($"Unsupported export format: {Format}. Supported formats: {string.Join(", ", Exporters.SupportedFormats)}");
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            throw new CommandLineException("Output path must be specified with --output");
        }

        if (Files.Count == 0)
        {
            throw new CommandLineException("At least one input file must be specified");
        }

        return this;
    }

    public override void Execute()
    {
        var exporter = Exporters.GetExporter(Format);
        var combinedContent = new List<string>();

        foreach (var file in Files)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Input file not found: {file}");
            }

            var content = File.ReadAllText(file);
            combinedContent.Add(content);
        }

        var finalContent = string.Join("\n\n", combinedContent);
        exporter.Export(finalContent, OutputPath);
    }
}