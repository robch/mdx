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
        ThreadCount = 0;
    }

    abstract public bool IsEmpty();
    abstract public string GetCommandName();

    public List<string> InstructionsList;
    public bool UseBuiltInFunctions;

    public string SaveOutput;

    public int ThreadCount;
}
