using System.Collections.Generic;
using System.Linq;

class WebGetCommand : WebCommand
{
    public WebGetCommand()
    {
        Urls = new List<string>();
    }
    
    public List<string> Urls { get; set; }

    override public string GetCommandName()
    {
        return "web get";
    }

    override public bool IsEmpty()
    {
        
        return !Urls.Any();
    }

    override public Command Validate()
    {
        if (!string.IsNullOrEmpty(JavaScriptToExecute) && JavaScriptToExecute.EndsWith(".js"))
        {
            if (!File.Exists(JavaScriptToExecute))
            {
                throw new WebGetCommandLineException($"JavaScript file not found: {JavaScriptToExecute}");
            }
        }
        return this;
    }
}
