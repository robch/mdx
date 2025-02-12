using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

abstract class Command
{
    public Command()
    {
        InstructionsList = new List<string>();
        UseBuiltInFunctions = false;
        SaveChatHistory = string.Empty;
        ThreadCount = 0;
        RepeatCount = 1; // Default to 1 execution
    }

    abstract public string GetCommandName();
    abstract public bool IsEmpty();
    abstract public Command Validate();

    public List<string> InstructionsList;
    public bool UseBuiltInFunctions;
    public string SaveChatHistory;

    public string SaveOutput;

    public int ThreadCount;

    public int RepeatCount; // Number of times to repeat the command
}
