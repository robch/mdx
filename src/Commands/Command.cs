using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

abstract class Command
{
    abstract public bool IsEmpty();

    public List<string> InstructionsList;
    public bool UseBuiltInFunctions;

    public string SaveFileOutput;
    public string SaveOutput;

    public int ThreadCount;
}
