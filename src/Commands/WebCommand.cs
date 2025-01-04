abstract class WebCommand : Command
{
    public bool Headless { get; set; }
    public bool StripHtml { get; set; }
    public string SaveFolder { get; set; }
    public bool UseBing { get; set; }
    public bool UseGoogle { get; set; }
    public bool DownloadContent { get; set; }
    public int MaxResults { get; set; }

    public WebCommand()
    {
        Headless = false;
        StripHtml = false;
        SaveFolder = null;
        UseBing = false;
        UseGoogle = true;
        DownloadContent = false;
        MaxResults = 10;
    }
}
