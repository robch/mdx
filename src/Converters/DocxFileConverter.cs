using System;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public class DocxFileConverter : IFileConverter
{
    private enum ListType { None, Bullet, Numbered }

    public bool CanConvert(string fileName)
    {
        return fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public string ConvertToMarkdown(string fileName)
    {
        using var doc = WordprocessingDocument.Open(fileName, false);
        var sb = new StringBuilder();

        var mainPart = doc.MainDocumentPart;
        if (mainPart?.Document?.Body == null)
        {
            return string.Empty;
        }

        foreach (var block in mainPart.Document.Body.Elements())
        {
            if (block is Paragraph paragraph)
            {
                var paraText = new StringBuilder();

                var pStyle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                var headingLevel = GetHeadingLevel(pStyle, doc);

                var listType = GetListType(paragraph, doc);
                if (listType == ListType.Bullet)
                    paraText.Append("- ");
                else if (listType == ListType.Numbered)
                    paraText.Append("1. ");
                
                foreach (var run in paragraph.Elements<Run>())
                {
                    bool bold = run.RunProperties?.Bold != null;
                    bool italic = run.RunProperties?.Italic != null;

                    string textValue = string.Join("", run.Elements<Text>().Select(t => t.Text));

                    if (bold) textValue = $"**{textValue}**";
                    if (italic) textValue = $"*{textValue}*";

                    paraText.Append(textValue);
                }

                if (headingLevel > 0)
                {
                    var hashMarks = new string('#', headingLevel);
                    sb.AppendLine($"{hashMarks} {paraText.ToString().Trim()}");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine(paraText.ToString());
                    sb.AppendLine();
                }
            }
            else if (block is Table table)
            {
                sb.AppendLine(ConvertTableToMarkdown(table, doc));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private int GetHeadingLevel(string styleId, WordprocessingDocument doc)
    {
        if (string.IsNullOrEmpty(styleId)) return 0;

        // OpenXML can define a style named "Heading1", "Heading2", etc. 
        // Some documents might name them differently.
        // So you might parse doc.MainDocumentPart.StyleDefinitionsPart to see if style has <w:name w:val="heading 1" />
        // or you do a simpler substring check like:
        if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            // e.g. Heading1 => 1
            var levelStr = styleId.Substring("Heading".Length);
            if (int.TryParse(levelStr, out int level))
                return Math.Min(level, 6); // clamp at 6 so we don't get # # # # # # # # ...
        }

        return 0;
    }

    private ListType GetListType(Paragraph para, WordprocessingDocument doc)
    {
        var numberingId = para.ParagraphProperties?.NumberingProperties?.NumberingId?.Val;

        if (numberingId is null) return ListType.None;

        var numberingPart = doc.MainDocumentPart.NumberingDefinitionsPart;
        if (numberingPart == null) return ListType.None;

        var abstractNumId = numberingPart.Numbering
             .Elements<NumberingInstance>()
             .First(i => i.NumberID.Value == numberingId.Value)
             ?.AbstractNumId.Val;

        var abstractNum = numberingPart.Numbering
            .Elements<AbstractNum>()
            .FirstOrDefault(an => an.AbstractNumberId?.Value == abstractNumId.Value);

        var level = abstractNum?.Elements<Level>()
            .FirstOrDefault(lvl => lvl.LevelIndex?.Value == 0);

        var numFmt = level?.NumberingFormat?.Val;

        if (numFmt != null && numFmt.Value == NumberFormatValues.Bullet)
            return ListType.Bullet;
        else
            return ListType.Numbered;
    }

    private string ConvertTableToMarkdown(Table table, WordprocessingDocument doc)
    {
        var sb = new StringBuilder();

        foreach (var row in table.Elements<TableRow>())
        {
            var rowTexts = row.Elements<TableCell>()
                .Select(cell => GetCellText(cell, doc))
                .ToArray();
            sb.AppendLine("| " + string.Join(" | ", rowTexts) + " |");

            if (row == table.Elements<TableRow>().First())
                sb.AppendLine("|" + string.Join("|", rowTexts.Select(_ => "---")) + "|");
        }

        return sb.ToString();
    }

    private string GetCellText(TableCell cell, WordprocessingDocument doc)
    {
        var cellText = new StringBuilder();
        var isFirst = true;
        foreach (var para in cell.Elements<Paragraph>())
        {
            if (!isFirst)
                cellText.Append("<br/>");
            else
                isFirst = false;

            var paraText = new StringBuilder();

            var listType = GetListType(para, doc);
            if (listType == ListType.Bullet)
                paraText.Append("* ");
            else if (listType == ListType.Numbered)
                paraText.Append("1. ");

            foreach (var run in para.Elements<Run>())
            {
                bool bold = run.RunProperties?.Bold != null;
                bool italic = run.RunProperties?.Italic != null;

                string textValue = string.Join("", run.Elements<Text>().Select(t => t.Text));

                if (bold) textValue = $"**{textValue}**";
                if (italic) textValue = $"*{textValue}*";

                paraText.Append(textValue);
            }

            cellText.Append(paraText);
        }

        return cellText.ToString().Trim();
    }
}