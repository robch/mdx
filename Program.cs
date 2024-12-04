using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

struct InputGroup
{
    public InputGroup()
    {
        Globs = new List<string>();
        IncludeFilePatterns = new List<Regex>();
        ExcludeFilePatterns = new List<Regex>();
        IncludeLinePatterns = new List<Regex>();
        ExcludeLinePatterns = new List<Regex>();
    }

    public List<string> Globs;
    public List<Regex> IncludeFilePatterns;
    public List<Regex> ExcludeFilePatterns;
    public List<Regex> IncludeLinePatterns;
    public List<Regex> ExcludeLinePatterns;
}

internal class InputException : Exception
{
    public InputException(string message) : base(message)
    {
    }
}

class Program
{
    static int Main(string[] args)
    {
        if (!ParseInputs(args, out var groups, out var ex))
        {
            PrintBanner();
            if (ex != null)
            {
                PrintException(ex);
                return 2;
            }
            else
            {
                PrintUsage();
                return 1;
            }
        }

        var processedFiles = new HashSet<string>();
        foreach (var group in groups)
        {
            var files = FindMatchingFiles(group.Globs, group.IncludeFilePatterns, group.ExcludeFilePatterns);
            foreach (var file in files)
            {
                if (!processedFiles.Contains(file))
                {
                    PrintFileContent(file, group.IncludeLinePatterns, group.ExcludeLinePatterns);
                    processedFiles.Add(file);
                }
            }
        }

        return 0;
    }

    private static IEnumerable<string> InputsFromStdio()
    {
        if (Console.IsInputRedirected)
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null) break;
                yield return line.Trim();
            }
        }
    }

    private static IEnumerable<string> InputsFromCommandLine(string[] args)
    {
        foreach (var line in InputsFromStdio())
        {
            yield return line;
        }

        foreach (var arg in args)
        {
            yield return arg;
        }
    }

    private static IEnumerable<string> ExpandedInputsFromCommandLine(string[] args)
    {
        foreach (var input in InputsFromCommandLine(args))
        {
            if (input.StartsWith("@") && File.Exists(input.Substring(1)))
            {
                foreach (var line in File.ReadLines(input.Substring(1)))
                {
                    yield return line.Trim();
                }
            }
            else
            {
                yield return input;
            }
        }
    }
    
    private static bool ParseInputs(string[] args, out List<InputGroup> groups, out InputException ex)
    {
        ex = null;
        groups = null;

        try
        {
            var allInputs = ExpandedInputsFromCommandLine(args);
            groups = ParseInputs(allInputs);
            return groups.Any();
        }
        catch (InputException e)
        {
            ex = e;
            return false;
        }
    }

    private static List<InputGroup> ParseInputs(IEnumerable<string> args)
    {
        var inputGroups = new List<InputGroup>();
        var currentGroup = new InputGroup();

        args = args.ToList();
        for (int i = 0; i < args.Count(); i++)
        {
            var arg = args.ElementAt(i);
            if (arg == "--")
            {
                inputGroups.Add(currentGroup);
                currentGroup = new InputGroup();
            }
            else if (arg == "--contains")
            {
                var pattern = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.IncludeFilePatterns.Add(ValidateRegExPattern(arg, pattern));
            }
            else if (arg == "--remove")
            {
                var pattern = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.ExcludeFilePatterns.Add(ValidateRegExPattern(arg, pattern));
            }
            else if (arg == "--line-contains" || arg == "--contains-line")
            {
                var pattern = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.IncludeLinePatterns.Add(ValidateRegExPattern(arg, pattern));
            }
            else if (arg == "--line-remove" || arg == "--remove-line")
            {
                var pattern = i + 1 < args.Count() ? args.ElementAt(++i) : null;
                currentGroup.ExcludeLinePatterns.Add(ValidateRegExPattern(arg, pattern));
            }
            else if (arg.StartsWith("--"))
            {
                throw new InputException($"{arg} - Invalid argument");
            }
            else
            {
                currentGroup.Globs.Add(arg);
            }
        }

        var groupOk = currentGroup.Globs.Any() ||
            currentGroup.IncludeFilePatterns.Any() || 
            currentGroup.ExcludeFilePatterns.Any() ||
            currentGroup.IncludeLinePatterns.Any() ||
            currentGroup.ExcludeLinePatterns.Any();
        if (groupOk)
        {
            inputGroups.Add(currentGroup);
        }

        return inputGroups;
    }

    private static Regex ValidateRegExPattern(string arg, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            throw new InputException($"{arg} - Missing regular expression pattern");
        }

        try
        {
            return new Regex(pattern);
        }
        catch (Exception)
        {
            throw new InputException($"{arg} {pattern} - Invalid regular expression pattern");
        }
    }

    private static bool IsFileMatch(string file, List<Regex> includePatterns, List<Regex> excludePatterns)
    {
        var checkContent = includePatterns.Any() || excludePatterns.Any();
        if (!checkContent) return true;

        var content = File.ReadAllText(file);
        var includeMatch = includePatterns.All(regex => regex.IsMatch(content));
        var excludeMatch = excludePatterns.Count > 0 && excludePatterns.Any(regex => regex.IsMatch(content));

        return includeMatch && !excludeMatch;
    }

    private static bool IsLineMatch(string line, List<Regex> includePatterns, List<Regex> excludePatterns)
    {
        var includeMatch = includePatterns.All(regex => regex.IsMatch(line));
        var excludeMatch = excludePatterns.Count > 0 && excludePatterns.Any(regex => regex.IsMatch(line));

        return includeMatch && !excludeMatch;
    }

    private static IEnumerable<string> FindMatchingFiles(List<string> globs, List<Regex> includePatterns, List<Regex> excludePatterns)
    {
        var files = FilesFromGlobs(globs).ToList();
        if (files.Count == 0)
        {
            Console.WriteLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files found\n");
            return Enumerable.Empty<string>();
        }

        var filtered = files.Where(file => IsFileMatch(file, includePatterns, excludePatterns)).ToList();
        if (filtered.Count == 0)
        {
            Console.WriteLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files matched criteria\n");
            return Enumerable.Empty<string>();
        }

        return filtered.Distinct();
    }

    private static IEnumerable<string> FilesFromGlobs(List<string> globs)
    {
        foreach (var glob in globs)
        {
            foreach (var file in FilesFromGlob(glob))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> FilesFromGlob(string glob)
    {
        try
        {
            var matcher = new Matcher();
            matcher.AddInclude(MakeRelativePath(glob));

            var directoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(Directory.GetCurrentDirectory()));
            var matchResult = matcher.Execute(directoryInfo);

            return matchResult.Files.Select(file => MakeRelativePath(Path.Combine(Directory.GetCurrentDirectory(), file.Path)));
        }
        catch (Exception)
        {
            return Enumerable.Empty<string>();
        }
    }

    static string MakeRelativePath(string fullPath)
    {
        var currentDirectory = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        fullPath = Path.GetFullPath(fullPath);

        if (fullPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(currentDirectory.Length);
        }

        Uri fullPathUri = new Uri(fullPath);
        Uri currentDirectoryUri = new Uri(currentDirectory);

        string relativePath = Uri.UnescapeDataString(currentDirectoryUri.MakeRelativeUri(fullPathUri).ToString().Replace('/', Path.DirectorySeparatorChar));

        if (Path.DirectorySeparatorChar == '\\')
        {
            relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    static void PrintBanner()
    {
        Console.WriteLine(
            "MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    static void PrintUsage()
    {
        Console.WriteLine(
            "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]"+"\n\n" +
            "OPTIONS:\n\n" +
            "  --remove REGEX         Exclude files that contain the specified regex pattern\n" +
            "  --contains REGEX       Include only files that contain the specified regex pattern\n\n" +
            "  --remove-line REGEX    Exclude lines that contain the specified regex pattern\n" +
            "  --contains-line REGEX  Include only lines that contain the specified regex pattern\n\n" +
            "EXAMPLES:\n\n" +
            "  mdcc file1.cs\n" +
            "  mdcc file1.md file2.md\n" +
            "  mdcc \"**/*.cs\" \"*.md\"\n" +
            "  mdcc \"src/**/*.py\" \"scripts/*.sh\"");
    }

    private static void PrintException(InputException ex)
    {
        Console.WriteLine($"{ex.Message}\n\n");
    }

    static void PrintFileContent(string fileName, List<Regex> includePatterns, List<Regex> excludePatterns)
    {
        try
        {
            var content = File.ReadAllText(fileName);

            var isMarkdown = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            var backticks = isMarkdown
                ? new string('`', GetMaxBacktickCharSequence(content))
                : "```";

            var filterContent = includePatterns.Any() || excludePatterns.Any();
            if (filterContent)
            {
                var lines = content.Split('\n')
                    .Where(line => IsLineMatch(line, includePatterns, excludePatterns))
                    .ToList();
                content = string.Join('\n', lines);
            }

            Console.WriteLine($"## {fileName}\n\n{backticks}\n{content}\n{backticks}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"## {fileName} - Error reading file: {ex.Message}\n\n");
        }
    }

    static int GetMaxBacktickCharSequence(string content)
    {
        int maxConsecutiveBackticks = 0;
        int currentStreak = 0;

        foreach (char c in content)
        {
            if (c == '`')
            {
                currentStreak++;
                if (currentStreak > maxConsecutiveBackticks)
                {
                    maxConsecutiveBackticks = currentStreak;
                }
            }
            else
            {
                currentStreak = 0;
            }
        }

        return Math.Max(3, maxConsecutiveBackticks + 1);
    }
}
