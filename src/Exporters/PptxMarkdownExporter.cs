using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Markdig;
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

/// <summary>
/// Exports markdown to PPTX format using OpenXML
/// </summary>
public class PptxMarkdownExporter : IMarkdownExporter
{
    public string OutputExtension => ".pptx";

    public void ExportMarkdown(string markdown, string outputPath)
    {
        using var presentation = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        var slideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.Shape(
                    new P.ShapeProperties()))));
        slideMaster.Save(slideMasterPart);

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        var slideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.Shape(
                    new P.ShapeProperties()))));
        slideLayout.Save(slideLayoutPart);

        // Add slides for each markdown section
        var sections = markdown.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section)) continue;

            var slidePart = presentationPart.AddNewPart<SlidePart>();
            var slide = new Slide(new CommonSlideData(new ShapeTree()));
            
            // Create text box with markdown content
            var textBox = new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties() { Id = 2U, Name = "Text Box" },
                    new P.NonVisualShapeDrawingProperties(new A.ShapeStyle()),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(),
                new P.TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text(section.Trim())))));

            slide.CommonSlideData.ShapeTree.AppendChild(textBox);
            slide.Save(slidePart);
        }

        presentation.Close();
    }
}