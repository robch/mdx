using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        foreach (var pattern in args)
        {
            var files = EnumerateFiles(pattern);
            if (!files.Any())
            {
                Console.WriteLine($"## Pattern: {pattern} - No files found\n");
                return 2;
            }

            foreach (var file in files)
            {
                PrintFileContentAsync(file);
            }
        }

        return 0;
    }

    static void PrintUsage()
    {
        Console.WriteLine("MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
                          "Copyright(c) 2024, Rob Chambers. All rights reserved.\n\n" +
                          "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]]"+"\n\n" +
                          "EXAMPLES:\n\n" +
                          "  mdcc file1.cs\n" +
                          "  mdcc file1.md file2.md\n" +
                          "  mdcc \"**/*.cs\" \"*.md\"\n" +
                          "  mdcc \"src/**/*.py\" \"scripts/*.sh\"");
    }

    static IEnumerable<string> EnumerateFiles(string pattern)
    {
        try
        {
            var matcher = new Matcher();
            matcher.AddInclude(pattern);

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

    static void PrintFileContentAsync(string fileName)
    {
        try
        {
            var content = File.ReadAllText(fileName);

            var isMarkdown = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            var backticks = isMarkdown
                ? new string('`', GetMaxBacktickCharSequence(content))
                : "```";

            Console.WriteLine($"## {fileName}\n\n{backticks}\n{content}\n{backticks}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"## {fileName} - Error reading file: {ex.Message}\n");
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
