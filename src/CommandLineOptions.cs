using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class CommandLineOptions
{
    public const string DefaultOptionsFileName = "options";
    public const string DefaultSaveFileOutputTemplate = "{filePath}/{fileBase}-output.md";
    public const string DefaultSaveOutputTemplate = "output.md";

    public CommandLineOptions()
    {
        Debug = false;
        Verbose = false;
        Commands = new List<Command>();

        AllOptions = null;
        SaveOptionsTemplate = null;
    }

    public bool Debug;
    public bool Verbose;
    public List<Command> Commands;

    public string[] AllOptions;
    public string SaveOptionsTemplate;

    public static bool Parse(string[] args, out CommandLineOptions options, out InputException ex)
    {
        options = null;
        ex = null;

        try
        {
            var allInputs = ExpandedInputsFromCommandLine(args);
            options = ParseInputOptions(allInputs);
            return options.Commands.Any();
        }
        catch (InputException e)
        {
            ex = e;
            return false;
        }
    }

    public List<string> SaveOptions(string fileName)
    {
        var filesSaved = new List<string>();

        var options = AllOptions
            .Where(x => x != "--save" && x != SaveOptionsTemplate)
            .Select(x => SingleLineOrNewAtFile(x, fileName, ref filesSaved));

        var asMultiLineString = string.Join('\n', options);
        File.WriteAllText(fileName, asMultiLineString, Encoding.UTF8);

        filesSaved.Insert(0, fileName);
        return filesSaved;
    }

    private string SingleLineOrNewAtFile(string text, string baseFileName, ref List<string> additionalFiles)
    {
        var isMultiLine = text.Contains('\n') || text.Contains('\r');
        if (!isMultiLine) return text;

        var additionalFileCount = additionalFiles.Count + 1;
        var additionalFileName = FileHelpers.GetFileNameFromTemplate(baseFileName, "{filepath}/{filebase}-" + additionalFileCount + "{fileext}");
        additionalFiles.Add(additionalFileName);

        File.WriteAllText(additionalFileName, text);

        return "@" + additionalFileName;
    }

    private static IEnumerable<string> ExpandedInputsFromCommandLine(string[] args)
    {
        var hasArgs = args.Any();
        if (hasArgs && File.Exists(DefaultOptionsFileName))
        {
            args = new[] { "@@" + DefaultOptionsFileName, "--and" }.Concat(args).ToArray();
        }

        return args.SelectMany(arg => ExpandedInput(arg));
    }
    
    private static IEnumerable<string> ExpandedInput(string input)
    {
        return input.StartsWith("@@")
            ? ExpandedAtAtFileInput(input)
            : input.StartsWith("@")
                ? [ExpandedAtFileInput(input)]
                : [input];
    }

    private static IEnumerable<string> ExpandedAtAtFileInput(string input)
    {
        if (!input.StartsWith("@@")) throw new ArgumentException("Not an @@ file input");

        var fileName = input.Substring(2);
        var fileNameOk = fileName == "-" || File.Exists(fileName);
        if (fileNameOk)
        {
            var lines = fileName == "-"
                ? ConsoleHelpers.GetAllLinesFromStdin()
                : File.ReadLines(fileName);

            return lines.SelectMany(line => ExpandedInput(line));
        }

        return [input];
    }

    private static string ExpandedAtFileInput(string input)
    {
        if (!input.StartsWith("@")) throw new ArgumentException("Not an @ file input");

        var fileName = input.Substring(1);
        var fileNameOk = fileName == "-" || File.Exists(fileName);
        return fileNameOk
            ? FileHelpers.ReadAllText(fileName)
            : input;
    }

    private static CommandLineOptions ParseInputOptions(IEnumerable<string> allInputs)
    {
        CommandLineOptions commandLineOptions = new();
        Command currentCommand = null;

        WebCommand currentWebCommand = null;
        FindFilesCommand currentFindFilesCommand = null;

        var args = commandLineOptions.AllOptions = allInputs.ToArray();
        for (int i = 0; i < args.Count(); i++)
        {
            var arg = args[i];
            if (arg == "--" && !currentCommand.IsEmpty())
            {
                commandLineOptions.Commands.Add(currentCommand);
                currentCommand = null;
                currentWebCommand = null;
                currentFindFilesCommand = null;
                continue;
            }

            if (currentCommand == null)
            {
                var name1 = GetInputOptionArgs(i, args, max: 1).FirstOrDefault();
                var name2 = GetInputOptionArgs(i + 1, args, max: 1).FirstOrDefault();
                if (name1 == "web" && (name2 == "search" || name2 == "get"))
                {
                    i += 1;
                    currentCommand = currentWebCommand = new WebCommand();
                    continue;
                }

                currentCommand = currentFindFilesCommand = new FindFilesCommand();
            }

            if (arg == "--and")
            {
                continue; // ignore --and ... used when combining @@ files
            }
            else if (arg == "--debug")
            {
                commandLineOptions.Debug = true;
            }
            else if (arg == "--verbose")
            {
                commandLineOptions.Verbose = true;
            }
            else if (arg == "--help")
            {
                throw new HelpRequestedInputException();
            }
            else if (arg == "--save-options" || arg == "--save")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveOptionsTemplate = max1Arg.FirstOrDefault() ?? DefaultOptionsFileName;
                commandLineOptions.SaveOptionsTemplate = saveOptionsTemplate;
                i += max1Arg.Count();
            }
            else if (arg == "--contains" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentFindFilesCommand.IncludeFileContainsPatternList.AddRange(asRegExs);
                currentFindFilesCommand.IncludeLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--file-contains" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentFindFilesCommand.IncludeFileContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--file-not-contains" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentFindFilesCommand.ExcludeFileContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--line-contains" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentFindFilesCommand.IncludeLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--lines" && currentFindFilesCommand != null)
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                var count = ValidateLineCount(arg, countStr);
                currentFindFilesCommand.IncludeLineCountBefore = count;
                currentFindFilesCommand.IncludeLineCountAfter = count;
            }
            else if (arg == "--lines-before" && currentFindFilesCommand != null)
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentFindFilesCommand.IncludeLineCountBefore = ValidateLineCount(arg, countStr);
            }
            else if (arg == "--lines-after" && currentFindFilesCommand != null)
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentFindFilesCommand.IncludeLineCountAfter = ValidateLineCount(arg, countStr);
            }
            else if (arg == "--remove-all-lines" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentFindFilesCommand.RemoveAllLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--line-numbers" && currentFindFilesCommand != null)
            {
                currentFindFilesCommand.IncludeLineNumbers = true; 
            }
            else if (arg.StartsWith("--") && arg.EndsWith("file-instructions") && currentFindFilesCommand != null)
            {
                var instructions = GetInputOptionArgs(i + 1, args);
                if (instructions.Count() == 0)
                {
                    throw new InputException($"Missing instructions for {arg}");
                }
                var fileNameCriteria = arg != "--file-instructions"
                    ? arg.Substring(2, arg.Length - 20)
                    : string.Empty;
                var withCriteria = instructions.Select(x => Tuple.Create(x, fileNameCriteria));
                currentFindFilesCommand.FileInstructionsList.AddRange(withCriteria);
                i += instructions.Count();
            }
            else if (arg == "--exclude" && currentFindFilesCommand != null)
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                if (patterns.Count() == 0)
                {
                    throw new InputException($"Missing patterns for {arg}");
                }

                var containsSlash = (string x) => x.Contains('/') || x.Contains('\\');
                var asRegExs = patterns
                    .Where(x => !containsSlash(x))
                    .Select(x => ValidateFilePatternToRegExPattern(arg, x));
                var asGlobs = patterns
                    .Where(x => containsSlash(x))
                    .ToList();

                currentFindFilesCommand.ExcludeFileNamePatternList.AddRange(asRegExs);
                currentFindFilesCommand.ExcludeGlobs.AddRange(asGlobs);
                i += patterns.Count();
            }
            else if (arg == "--save-file-output")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveFileOutput = max1Arg.FirstOrDefault() ?? DefaultSaveFileOutputTemplate;
                currentCommand.SaveFileOutput = saveFileOutput;
                i += max1Arg.Count();
            }
            else if (arg == "--instructions")
            {
                var instructions = GetInputOptionArgs(i + 1, args);
                if (instructions.Count() == 0)
                {
                    throw new InputException($"Missing instructions for {arg}");
                }
                currentCommand.InstructionsList.AddRange(instructions);
                i += instructions.Count();
            }
            else if (arg == "--save-output")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveOutput = max1Arg.FirstOrDefault() ?? DefaultSaveOutputTemplate;
                currentCommand.SaveOutput = saveOutput;
                i += max1Arg.Count();
            }
            else if (arg == "--built-in-functions")
            {
                currentCommand.UseBuiltInFunctions = true;
            }
            else if (arg == "--threads")
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentCommand.ThreadCount = ValidateInt(arg, countStr, "thread count");
            }
            else if (arg.StartsWith("--"))
            {
                throw new InputException($"Invalid argument: {arg}");
            }
            else
            {
                currentFindFilesCommand.Globs.Add(arg);
            }
        }

        if (currentCommand != null && !currentCommand.IsEmpty())
        {
            commandLineOptions.Commands.Add(currentCommand);
        }

        var findFilesCommands = commandLineOptions.Commands.OfType<FindFilesCommand>().ToList();
        foreach (var findFileCommand in findFilesCommands.Where(x => !x.Globs.Any()))
        {
            findFileCommand.Globs.Add("**");
        }

        return commandLineOptions;
    }

    private static IEnumerable<string> GetInputOptionArgs(int startAt, string[] args, int max = int.MaxValue)
    {
        for (int i = startAt; i < args.Length && i - startAt < max; i++)
        {
            if (args[i].StartsWith("--"))
            {
                yield break;
            }

            yield return args[i];
        }
    }

    private static IEnumerable<Regex> ValidateRegExPatterns(string arg, IEnumerable<string> patterns)
    {
        patterns = patterns.ToList();
        if (!patterns.Any())
        {
            throw new InputException($"Missing regular expression patterns for {arg}");
        }

        return patterns.Select(x => ValidateRegExPattern(arg, x));
    }

    private static Regex ValidateRegExPattern(string arg, string pattern)
    {
        try
        {
            return new Regex(pattern);
        }
        catch (Exception)
        {
            throw new InputException($"Invalid regular expression pattern for {arg}: {pattern}");
        }
    }

    private static Regex ValidateFilePatternToRegExPattern(string arg, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new InputException($"Missing file pattern for {arg}");
        }

        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        var patternPrefix = isWindows ? "(?i)^" : "^";
        var regexPattern = patternPrefix + pattern
            .Replace(".", "\\.")
            .Replace("*", ".*")
            .Replace("?", ".") + "$";

        try
        {
            return new Regex(regexPattern);
        }
        catch (Exception)
        {
            throw new InputException($"Invalid file pattern for {arg}: {pattern}");
        }
    }

    private static int ValidateLineCount(string arg, string countStr)
    {
        return ValidateInt(arg, countStr, "line count");
    }

    private static int ValidateInt(string arg, string countStr, string argDescription)
    {
        if (string.IsNullOrEmpty(countStr))
        {
            throw new InputException($"Missing {argDescription} for {arg}");
        }

        if (!int.TryParse(countStr, out var count))
        {
            throw new InputException($"Invalid {argDescription} for {arg}: {countStr}");
        }

        return count;
    }
}
