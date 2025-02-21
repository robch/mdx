using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Factory class for creating markdown exporters
/// </summary>
public static class MarkdownExporters
{
    private static readonly Dictionary<string, IMarkdownExporter> _exporters = new()
    {
        { ".pdf", new PdfMarkdownExporter() },
        { ".docx", new DocxMarkdownExporter() },
        { ".pptx", new PptxMarkdownExporter() }
    };

    /// <summary>
    /// Gets an exporter for the specified output format
    /// </summary>
    public static IMarkdownExporter GetExporter(string outputPath)
    {
        var extension = System.IO.Path.GetExtension(outputPath).ToLowerInvariant();
        if (_exporters.TryGetValue(extension, out var exporter))
        {
            return exporter;
        }

        throw new ArgumentException($"No exporter available for {extension} format");
    }

    /// <summary>
    /// Gets all supported output extensions
    /// </summary>
    public static IEnumerable<string> GetSupportedExtensions()
    {
        return _exporters.Keys;
    }
}