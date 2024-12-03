using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class MDCC
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
            }
            else
            {
                foreach (var file in files)
                {
                    PrintFileContentAsync(file);
                }
            }
        }

        return 0;
    }

    static void PrintUsage()
    {
        Console.WriteLine("MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
                          "Copyright(c) 2024, Rob Chambers. All rights reserved.\n\n" +
                          "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]]\n\n" +
                          "EXAMPLES:\n\n" +
                          "  mdcc file1.cs\n" +
                          "  mdcc file1.md file2.md\n" +
                          "  mdcc \"**/*.cs\" \"*.md\"\n" +
                          "  mdcc \"src/**/*.py\" \"scripts/*.sh\"\n");
    }

    static IEnumerable<string> EnumerateFiles(string pattern)
    {
        try
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, MatchCasing = MatchCasing.CaseInsensitive };
            string directory = Path.GetDirectoryName(pattern);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }
            var files = Directory.EnumerateFiles(directory, Path.GetFileName(pattern), options);
            return files.Select(file => MakeRelativePath(file));
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
            Console.WriteLine($"## {fileName}\n\n```\n{content}\n```\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"## {fileName} - Error reading file: {ex.Message}\n");
        }
    }
}
