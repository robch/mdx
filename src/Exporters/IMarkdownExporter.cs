namespace mdx.Exporters;

/// <summary>
/// Interface for exporting markdown content to other file formats
/// </summary>
public interface IMarkdownExporter
{
    /// <summary>
    /// Get the supported output format extension (without dot)
    /// </summary>
    string OutputFormat { get; }

    /// <summary>
    /// Export markdown content to the target format
    /// </summary>
    /// <param name="markdownContent">The markdown content to export</param>
    /// <param name="outputPath">Path where to save the exported file</param>
    void Export(string markdownContent, string outputPath);
}