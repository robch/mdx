using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

class Program
{
    static int Main(string[] args)
    {
        var allInputs = ExpandedInputsFromCommandLine(args);
        if (!allInputs.Any())
        {
            PrintUsage();
            return 1;
        }

        if (!ValidateInputs(allInputs, out var invalidOption))
        {
            Console.WriteLine($"## Invalid option: {invalidOption}\n");
            return 2;
        }

        var files = FindMatchingFilesFromInputs(allInputs);
        foreach (var file in files)
        {
            PrintFileContent(file);
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

    private static bool ValidateInputs(IEnumerable<string> inputs, out string invalidOption)
    {
        var inputsAsList = inputs.ToList();
        for (int i = 0; i < inputsAsList.Count(); i++)
        {
            var input = inputsAsList[i];
            if (input == "--") continue;
            if (input.StartsWith("--contains") && i + 1 < inputsAsList.Count())
            {
                i++;
                continue;
            }
            if (input.StartsWith("--remove") && i + 1 < inputsAsList.Count())
            {
                i++;
                continue;
            }
            if (input.StartsWith("--"))
            {
                invalidOption = input;
                return false;
            }
        }

        invalidOption = null;
        return true;
    }

    private static List<string> FindMatchingFilesFromInputs(IEnumerable<string> inputs)
    {
        var files = new List<string>();

        var globs = new List<string>();
        var includePatterns = new List<string>();
        var excludePatterns = new List<string>();

        var inputsAsList = inputs.ToList();
        for (int i = 0; i < inputsAsList.Count(); i++)
        {
            var input = inputsAsList[i];
            if (input == "--")
            {
                AddMatchingFiles(files, globs, includePatterns, excludePatterns);
            }
            else if (input.StartsWith("--contains") && i + 1 < inputsAsList.Count())
            {
                i++;
                var contains = inputsAsList[i];
                includePatterns.Add(contains);
            }
            else if (input.StartsWith("--remove") && i + 1 < inputsAsList.Count())
            {
                i++;
                var remove = inputsAsList[i];
                excludePatterns.Add(remove);
            }
            else
            {
                globs.Add(input);
            }
        }

        AddMatchingFiles(files, globs, includePatterns, excludePatterns);
        return files.Distinct().ToList();
    }

    private static void AddMatchingFiles(List<string> files, List<string> globs, List<string> includePatterns, List<string> excludePatterns)
    {
        files.AddRange(FindMatchingFiles(globs, includePatterns, excludePatterns));
        globs.Clear();
        includePatterns.Clear();
        excludePatterns.Clear();
    }

    private static IEnumerable<string> FindMatchingFiles(List<string> globs, List<string> includePatterns, List<string> excludePatterns)
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

    private static bool IsFileMatch(string file, List<string> includePatterns, List<string> excludePatterns)
    {
        var checkContent = includePatterns.Any() || excludePatterns.Any();
        if (!checkContent) return true;

        var content = File.ReadAllText(file);
        var includeMatch = includePatterns.All(pattern => Regex.IsMatch(content, pattern));
        var excludeMatch = excludePatterns.Count > 0 && excludePatterns.Any(pattern => Regex.IsMatch(content, pattern));

        return includeMatch && !excludeMatch;
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

    static void PrintUsage()
    {
        Console.WriteLine("MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
                          "Copyright(c) 2024, Rob Chambers. All rights reserved.\n\n" +
                          "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]"+"\n\n" +
                          "OPTIONS:\n\n" +
                          "  --contains REGEX  Include only files that contain the specified regex pattern\n" +
                          "  --remove REGEX    Exclude files that contain the specified regex pattern\n\n" +
                          "EXAMPLES:\n\n" +
                          "  mdcc file1.cs\n" +
                          "  mdcc file1.md file2.md\n" +
                          "  mdcc \"**/*.cs\" \"*.md\"\n" +
                          "  mdcc \"src/**/*.py\" \"scripts/*.sh\"");
    }

    static void PrintFileContent(string fileName)
    {
        try
        {
            var content = File.ReadAllText(fileName);

            var isMarkdown = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            var backticks = isMarkdown
                ? new string('`', GetMaxBacktickCharSequence(content))
                : "```";

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
