using System;
using System.Collections.Generic;
using System.Linq;

namespace mdx.Exporters;

/// <summary>
/// Manages available markdown exporters
/// </summary>
public class MarkdownExporters
{
    private readonly Dictionary<string, IMarkdownExporter> _exporters = new();

    public MarkdownExporters()
    {
        RegisterExporters();
    }

    /// <summary>
    /// Register all available exporters
    /// </summary>
    private void RegisterExporters()
    {
        RegisterExporter(new PdfMarkdownExporter());
        RegisterExporter(new DocxMarkdownExporter());
        RegisterExporter(new PptxMarkdownExporter());
    }

    /// <summary>
    /// Register a single exporter
    /// </summary>
    private void RegisterExporter(IMarkdownExporter exporter)
    {
        _exporters[exporter.OutputFormat.ToLowerInvariant()] = exporter;
    }

    /// <summary>
    /// Get an exporter for the specified format
    /// </summary>
    public IMarkdownExporter GetExporter(string format)
    {
        return _exporters.TryGetValue(format.ToLowerInvariant(), out var exporter)
            ? exporter
            : throw new ArgumentException($"No exporter found for format: {format}");
    }

    /// <summary>
    /// Get all supported export formats
    /// </summary>
    public IEnumerable<string> SupportedFormats => _exporters.Keys;
}