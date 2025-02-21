/// <summary>
/// Interface for converting markdown content to different document formats
/// </summary>
public interface IMarkdownExporter
{
    /// <summary>
    /// Gets the file extension this exporter produces (e.g. ".pdf", ".docx", ".pptx")
    /// </summary>
    string OutputExtension { get; }

    /// <summary>
    /// Converts markdown content to the target format and saves to the specified file
    /// </summary>
    /// <param name="markdown">The markdown content to convert</param>
    /// <param name="outputPath">The output file path where the converted document should be saved</param>
    void ExportMarkdown(string markdown, string outputPath);
}