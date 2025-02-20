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
        InputActions = new();
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
    public List<WebInputAction> InputActions;

    public string SavePageOutput { get; set; }
}

public class WebInputAction
{
    public WebInputActionType Type { get; set; }
    public string Selector { get; set; }
    public string Value { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public string TargetSelector { get; set; }

    public WebInputAction(WebInputActionType type)
    {
        Type = type;
    }
}

public enum WebInputActionType
{
    Click,
    MouseMove,
    DragDrop,
    Scroll,
    KeyPress,
    TypeText,
    Shortcut
}
}
