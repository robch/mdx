using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class FindFilesCommand : Command
{
    public FindFilesCommand()
    {
        Globs = new();
        ExcludeGlobs = new();
        ExcludeFileNamePatternList = new();

        IncludeFileContainsPatternList = new();
        ExcludeFileContainsPatternList = new();

        IncludeLineContainsPatternList = new();
        IncludeLineCountBefore = 0;
        IncludeLineCountAfter = 0;
        IncludeLineNumbers = false;

        RemoveAllLineContainsPatternList = new();
        FileInstructionsList = new();
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

    override public string GetCommandName()
    {
        return "";
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

    public string SaveFileOutput;
}
