abstract class WebCommand : Command
{
    public WebCommand()
    {
        Headless = false;
        StripHtml = false;
        SaveFolder = null;
        UseBing = false;
        UseGoogle = true;
        GetContent = false;
        MaxResults = 10;
    }

    public bool Headless { get; set; }
    public bool StripHtml { get; set; }
    public string SaveFolder { get; set; }
    public bool UseBing { get; set; }
    public bool UseGoogle { get; set; }
    public bool GetContent { get; set; }
    public int MaxResults { get; set; }
}
