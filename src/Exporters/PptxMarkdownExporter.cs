using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Drawing = DocumentFormat.OpenXml.Drawing;
using Markdig;

namespace mdx.Exporters;

public class PptxMarkdownExporter : IMarkdownExporter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string OutputFormat => "pptx";

    public void Export(string markdownContent, string outputPath)
    {
        using var presentation = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
        var presentationPart = presentation.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        var slideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree()),
            new ColorMap { Background1 = "lt1", Text1 = "dk1", Background2 = "lt2", Text2 = "dk2", Accent1 = "accent1", Accent2 = "accent2", Accent3 = "accent3", Accent4 = "accent4", Accent5 = "accent5", Accent6 = "accent6", Hyperlink = "hlink", FollowedHyperlink = "folHlink" }
        );
        slideMasterPart.SlideMaster = slideMaster;

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        var slideLayout = new SlideLayout(new CommonSlideData(new ShapeTree()));
        slideLayoutPart.SlideLayout = slideLayout;

        slideMaster.SlideLayoutIdList = new SlideLayoutIdList(new SlideLayoutId { Id = 2147483649U, RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart) });

        presentationPart.Presentation.SlideMasterIdList = new SlideMasterIdList(new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) });
        presentationPart.Presentation.SlideIdList = new SlideIdList();
        presentationPart.Presentation.SlideSize = new SlideSize { Cx = 9144000, Cy = 6858000, Type = SlideSizeValues.Screen16x9 };
        presentationPart.Presentation.NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };

        // Parse markdown to HTML first
        var html = Markdown.ToHtml(markdownContent, Pipeline);
        
        // Split content by double newline to create slides
        var slides = html.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim());

        foreach (var slideContent in slides)
        {
            CreateSlideWithText(presentationPart, slideLayoutPart, slideContent);
        }
    }

    private void CreateSlideWithText(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, string text)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        var slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.Slide = slide;

        slide.CommonSlideData = new CommonSlideData(
            new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(),
                new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2, Name = "Title 1" },
                        new NonVisualShapeDrawingProperties(new Drawing.ShapeLocks { NoGrouping = true }),
                        new ApplicationNonVisualDrawingProperties(new PlaceholderShape())),
                    new ShapeProperties(),
                    new TextBody(
                        new Drawing.BodyProperties(),
                        new Drawing.ListStyle(),
                        new Drawing.Paragraph(new Drawing.Run(new Drawing.Text { Text = text }))))));

        slidePart.AddPart(slideLayoutPart, "rId1");
        presentationPart.Presentation.SlideIdList.Append(new SlideId { Id = 256U + (uint)presentationPart.Presentation.SlideIdList.Count(), RelationshipId = presentationPart.GetIdOfPart(slidePart) });
    }
}