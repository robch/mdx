using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Markdig;

/// <summary>
/// Exports markdown to PDF format using iText7
/// </summary>
public class PdfMarkdownExporter : IMarkdownExporter
{
    public string OutputExtension => ".pdf";

    public void ExportMarkdown(string markdown, string outputPath)
    {
        // Convert markdown to HTML first
        var html = Markdown.ToHtml(markdown);

        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        // Simple conversion - future enhancement would be to properly style and format
        foreach (var line in html.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var text = new Paragraph(line.Trim());
            document.Add(text);
        }
    }
}