using System;
using System.IO;
using System.Threading.Tasks;
using Markdig;
using PuppeteerSharp;

namespace mdx.Exporters;

public class PdfMarkdownExporter : IMarkdownExporter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string OutputFormat => "pdf";

    public void Export(string markdownContent, string outputPath)
    {
        // Convert markdown to HTML
        var html = GenerateHtml(markdownContent);
        
        // Generate PDF using Puppeteer
        GeneratePdfAsync(html, outputPath).GetAwaiter().GetResult();
    }

    private static string GenerateHtml(string markdown)
    {
        var htmlContent = Markdown.ToHtml(markdown, Pipeline);
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
            line-height: 1.6;
            padding: 2em;
            max-width: 50em;
            margin: auto;
        }}
        pre {{
            background-color: #f6f8fa;
            padding: 1em;
            border-radius: 4px;
            overflow-x: auto;
        }}
        code {{
            font-family: 'Consolas', 'Monaco', monospace;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
    }

    private static async Task GeneratePdfAsync(string html, string outputPath)
    {
        await new BrowserFetcher().DownloadAsync();
        
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();
        
        await page.SetContentAsync(html);
        await page.PdfAsync(outputPath, new PdfOptions 
        { 
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "1cm",
                Right = "1cm",
                Bottom = "1cm",
                Left = "1cm"
            }
        });
    }
}