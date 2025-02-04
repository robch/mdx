using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

abstract class WebCommand : Command
{
    public WebCommand()
    {
        Interactive = false;

        SearchProvider = WebSearchProvider.Google;
        MaxResults = 10;

        ExcludeURLContainsPatternList = new();

        Browser = BrowserType.Chromium;
        GetContent = false;
        StripHtml = false;

        SaveFolder = null;

        PageInstructionsList = new();
        WaitForSelectors = new();
        WaitForTimeout = 30000; // Default 30 seconds timeout
    }

    public bool Interactive { get; set; }

    public WebSearchProvider SearchProvider { get; set; }
    public List<Regex> ExcludeURLContainsPatternList { get; set; }
    public int MaxResults { get; set; }

    public BrowserType Browser { get; set; }
    public bool GetContent { get; set; }
    public bool StripHtml { get; set; }

    public string SaveFolder { get; set; }

    public List<Tuple<string, string>> PageInstructionsList;

    public string SavePageOutput { get; set; }

    /// <summary>
    /// List of CSS or XPath selectors to wait for before considering the page loaded
    /// </summary>
    public List<string> WaitForSelectors { get; set; }

    /// <summary>
    /// Maximum time in milliseconds to wait for selectors to appear
    /// </summary>
    public int WaitForTimeout { get; set; }
}
