using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;

namespace mdx.Exporters;

public class DocxMarkdownExporter : IMarkdownExporter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string OutputFormat => "docx";

    public void Export(string markdownContent, string outputPath)
    {
        using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Parse markdown to HTML first (easier to process)
        var html = Markdown.ToHtml(markdownContent, Pipeline);
        
        // Simple HTML parsing to generate basic DOCX paragraphs
        var lines = html.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var line in lines)
        {
            var para = new Paragraph(new Run(new Text(line)));
            body.AppendChild(para);
        }

        mainPart.Document.Save();
    }
}