using System.Collections.Generic;
using System.Linq;

class WebGetCommand : WebCommand
{
    public WebGetCommand()
    {
        Urls = new List<string>();
    }
    
    public List<string> Urls { get; set; }

    override public bool IsEmpty()
    {
        return !Urls.Any();
    }
}
