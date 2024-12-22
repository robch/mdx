using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class InputOptions
{
    public const string DefaultOptionsFileName = "options";
    public const string DefaultSaveFileOutputTemplate = "{filePath}/{fileBase}-output.md";
    public const string DefaultSaveOutputTemplate = "output.md";

    public InputOptions()
    {
        Debug = false;
        Verbose = false;
        Groups = new List<InputGroup>();

        AllOptions = null;
        SaveOptionsTemplate = null;
    }

    public bool Debug;
    public bool Verbose;
    public List<InputGroup> Groups;

    public string[] AllOptions;
    public string SaveOptionsTemplate;

    public static bool Parse(string[] args, out InputOptions options, out InputException ex)
    {
        options = null;
        ex = null;

        try
        {
            var allInputs = ExpandedInputsFromCommandLine(args);
            options = ParseInputOptions(allInputs);
            return options.Groups.Any();
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

    private static InputOptions ParseInputOptions(IEnumerable<string> allInputs)
    {
        var inputOptions = new InputOptions();
        var currentGroup = new InputGroup();

        var args = inputOptions.AllOptions = allInputs.ToArray();
        for (int i = 0; i < args.Count(); i++)
        {
            var arg = args[i];
            if (arg == "--" && !currentGroup.IsEmpty())
            {
                inputOptions.Groups.Add(currentGroup);
                currentGroup = new InputGroup();
            }
            else if (arg == "--and")
            {
                continue; // ignore --and ... used when combining @@ files
            }
            else if (arg == "--debug")
            {
                inputOptions.Debug = true;
            }
            else if (arg == "--verbose")
            {
                inputOptions.Verbose = true;
            }
            else if (arg == "--help")
            {
                throw new HelpRequestedInputException();
            }
            else if (arg == "--save-options" || arg == "--save")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveOptionsTemplate = max1Arg.FirstOrDefault() ?? DefaultOptionsFileName;
                inputOptions.SaveOptionsTemplate = saveOptionsTemplate;
                i += max1Arg.Count();
            }
            else if (arg == "--contains")
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentGroup.IncludeFileContainsPatternList.AddRange(asRegExs);
                currentGroup.IncludeLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--file-contains")
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentGroup.IncludeFileContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--file-not-contains")
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentGroup.ExcludeFileContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--line-contains")
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentGroup.IncludeLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--lines")
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                var count = ValidateLineCount(arg, countStr);
                currentGroup.IncludeLineCountBefore = count;
                currentGroup.IncludeLineCountAfter = count;
            }
            else if (arg == "--lines-before")
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.IncludeLineCountBefore = ValidateLineCount(arg, countStr);
            }
            else if (arg == "--lines-after")
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.IncludeLineCountAfter = ValidateLineCount(arg, countStr);
            }
            else if (arg == "--remove-all-lines")
            {
                var patterns = GetInputOptionArgs(i + 1, args);
                var asRegExs = ValidateRegExPatterns(arg, patterns);
                currentGroup.RemoveAllLineContainsPatternList.AddRange(asRegExs);
                i += patterns.Count();
            }
            else if (arg == "--line-numbers")
            {
                currentGroup.IncludeLineNumbers = true; 
            }
            else if (arg.StartsWith("--") && arg.EndsWith("file-instructions"))
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
                currentGroup.FileInstructionsList.AddRange(withCriteria);
                i += instructions.Count();
            }
            else if (arg == "--save-file-output")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveFileOutput = max1Arg.FirstOrDefault() ?? DefaultSaveFileOutputTemplate;
                currentGroup.SaveFileOutput = saveFileOutput;
                i += max1Arg.Count();
            }
            else if (arg == "--instructions")
            {
                var instructions = GetInputOptionArgs(i + 1, args);
                if (instructions.Count() == 0)
                {
                    throw new InputException($"Missing instructions for {arg}");
                }
                currentGroup.InstructionsList.AddRange(instructions);
                i += instructions.Count();
            }
            else if (arg == "--save-output")
            {
                var max1Arg = GetInputOptionArgs(i + 1, args, max: 1);
                var saveOutput = max1Arg.FirstOrDefault() ?? DefaultSaveOutputTemplate;
                currentGroup.SaveOutput = saveOutput;
                i += max1Arg.Count();
            }
            else if (arg == "--built-in-functions")
            {
                currentGroup.UseBuiltInFunctions = true;
            }
            else if (arg == "--threads")
            {
                var countStr = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.ThreadCount = ValidateInt(arg, countStr, "thread count");
            }
            else if (arg == "--exclude")
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

                currentGroup.ExcludeFileNamePatternList.AddRange(asRegExs);
                currentGroup.ExcludeGlobs.AddRange(asGlobs);
                i += patterns.Count();
            }
            else if (arg.StartsWith("--"))
            {
                throw new InputException($"Invalid argument: {arg}");
            }
            else
            {
                currentGroup.Globs.Add(arg);
            }
        }

        if (!currentGroup.IsEmpty())
        {
            inputOptions.Groups.Add(currentGroup);
        }

        foreach (var group in inputOptions.Groups.Where(x => !x.Globs.Any()))
        {
            group.Globs.Add("**");
        }

        return inputOptions;
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
