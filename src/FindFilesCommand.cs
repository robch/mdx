using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class FindFilesCommand : Command
{
    public FindFilesCommand()
    {
        Globs = new List<string>();
        ExcludeGlobs = new List<string>();
        ExcludeFileNamePatternList = new List<Regex>();

        IncludeFileContainsPatternList = new List<Regex>();
        ExcludeFileContainsPatternList = new List<Regex>();

        IncludeLineContainsPatternList = new List<Regex>();
        IncludeLineCountBefore = 0;
        IncludeLineCountAfter = 0;
        IncludeLineNumbers = false;

        RemoveAllLineContainsPatternList = new List<Regex>();
        FileInstructionsList = new List<Tuple<string, string>>();
        InstructionsList = new List<string>();
        UseBuiltInFunctions = false;
        
        ThreadCount = 0;
    }

    override public bool IsEmpty()
    {
        return !Globs.Any() &&
            !ExcludeGlobs.Any() &&
            !ExcludeFileNamePatternList.Any() &&
            !IncludeFileContainsPatternList.Any() &&
            !ExcludeFileContainsPatternList.Any() &&
            !IncludeLineContainsPatternList.Any() &&
            IncludeLineCountBefore == 0 &&
            IncludeLineCountAfter == 0 &&
            IncludeLineNumbers == false &&
            !RemoveAllLineContainsPatternList.Any() &&
            !FileInstructionsList.Any() &&
            ThreadCount == 0;
    }

    public List<string> Globs;
    public List<string> ExcludeGlobs;
    public List<Regex> ExcludeFileNamePatternList;

    public List<Regex> IncludeFileContainsPatternList;
    public List<Regex> ExcludeFileContainsPatternList;

    public List<Regex> IncludeLineContainsPatternList;
    public int IncludeLineCountBefore;
    public int IncludeLineCountAfter;
    public bool IncludeLineNumbers;

    public List<Regex> RemoveAllLineContainsPatternList;

    public List<Tuple<string, string>> FileInstructionsList;
}
