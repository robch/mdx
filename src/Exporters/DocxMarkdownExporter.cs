using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;

/// <summary>
/// Exports markdown to DOCX format using OpenXML
/// </summary>
public class DocxMarkdownExporter : IMarkdownExporter
{
    public string OutputExtension => ".docx";

    public void ExportMarkdown(string markdown, string outputPath)
    {
        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Convert markdown to simple paragraphs
        var html = Markdown.ToHtml(markdown);
        foreach (var line in html.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var para = new Paragraph(new Run(new Text(line.Trim())));
            body.AppendChild(para);
        }
    }
}