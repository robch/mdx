using System.Collections.Generic;
using System.Linq;

class WebSearchCommand : WebCommand
{
    public WebSearchCommand()
    {
        Terms = new List<string>();
    }

    public List<string> Terms { get; set; }

    override public bool IsEmpty()
    {
        return !Terms.Any();
    }
}
