using System;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

public class PptxFileConverter : IFileConverter
{
    public bool CanConvert(string fileName)
    {
        return fileName.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase);
    }

    public string ConvertToMarkdown(string fileName)
    {
        using var presentationDoc = PresentationDocument.Open(fileName, false);
        var sb = new StringBuilder();

        var presentationPart = presentationDoc.PresentationPart;
        if (presentationPart == null)
        {
            return string.Empty;
        }

        var slideNumber = 1;
        var slideIdList = presentationPart.Presentation.SlideIdList;
        foreach (var slideId in slideIdList.Elements<SlideId>())
        {
            var slidePart = presentationPart.GetPartById(slideId.RelationshipId) as SlidePart;
            sb.AppendLine(ConvertSlideToMarkdown(slidePart, slideNumber++));
        }

        return sb.ToString();
    }

    private string ConvertSlideToMarkdown(SlidePart slidePart, int slideNumber)
    {
        var sb = new StringBuilder();
        var slide = slidePart.Slide;

        // Extract the slide title
        var titleShape = slide.Descendants<Shape>().FirstOrDefault(s => IsTitleShape(s));
        if (titleShape != null)
        {
            var titleText = GetTextFromShape(titleShape);
            sb.AppendLine($"# Slide #{slideNumber}: {titleText}");
        }
        else
        {
            sb.AppendLine($"# Slide #{slideNumber}");
        }

        // Extract other text boxes and shapes
        foreach (var shape in slide.Descendants<Shape>().Where(s => !IsTitleShape(s)))
        {
            var text = GetTextFromShape(shape);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }
        }

        // Extract tables
        foreach (var graphicFrame in slide.Descendants<GraphicFrame>())
        {
            var tableMarkdown = ConvertTableToMarkdown(graphicFrame);
            if (!string.IsNullOrWhiteSpace(tableMarkdown))
            {
                sb.AppendLine(tableMarkdown);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private bool IsTitleShape(Shape shape)
    {
        var placeholder = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape;
        return placeholder != null && placeholder.Type != null &&
            placeholder.Type.HasValue && placeholder.Type.Value == PlaceholderValues.Title;
    }

    private string GetTextFromShape(Shape shape)
    {
        // Some shapes may not have text bodies at all
        if (shape.TextBody == null)
            return string.Empty;

        var sb = new StringBuilder();
        
        // Go through each Paragraph in the shape
        var paragraphs = shape.TextBody.Elements<A.Paragraph>();
        bool firstParagraph = true;

        foreach (var paragraph in paragraphs)
        {
            // Build up this paragraph's text from its Runs
            var paragraphText = new StringBuilder();

            foreach (var run in paragraph.Elements<A.Run>())
            {
                // If the <a:t> (A.Text) child is null (rare, but can happen), skip
                if (run?.Text?.Text == null)
                    continue;

                paragraphText.Append(run.Text.Text);
            }

            // If there's any actual text, append it to the shape's text builder
            var trimmed = paragraphText.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(trimmed))
            {
                if (!firstParagraph)
                {
                    // If not the first paragraph, 
                    // we’ll add a line break before appending new paragraph text
                    sb.AppendLine();
                }
                sb.Append(trimmed);
                firstParagraph = false;
            }
        }

        return sb.ToString();
    }

    private string ConvertTableToMarkdown(GraphicFrame graphicFrame)
    {
        var sb = new StringBuilder();
        var table = graphicFrame.Descendants<A.Table>().FirstOrDefault();
        if (table == null) return string.Empty;

        foreach (var row in table.Elements<A.TableRow>())
        {
            var rowTexts = row.Elements<A.TableCell>()
                .Select(cell => GetCellText(cell))
                .ToArray();
            sb.AppendLine("| " + string.Join(" | ", rowTexts) + " |");

            if (row == table.Elements<A.TableRow>().First())
                sb.AppendLine("|" + string.Join("|", rowTexts.Select(_ => "---")) + "|");
        }

        return sb.ToString();
    }

    private string GetCellText(A.TableCell cell)
    {
        var cellText = new StringBuilder();
        var isFirst = true;
        foreach (var paragraph in cell.TextBody.Elements<A.Paragraph>())
        {
            var paragraphText = GetParagraphText(paragraph).TrimEnd();
            if (string.IsNullOrWhiteSpace(paragraphText)) continue;

            if (!isFirst)
                cellText.Append("<br/>");
            else
                isFirst = false;

            cellText.Append(paragraphText);
        }

        return cellText.ToString().Trim();
    }

    private string GetParagraphText(A.Paragraph paragraph)
    {
        return string.Join(" ", paragraph.Elements<A.Run>().Select(run => run.Text.Text));
    }
}
