using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class FileHelpers
{
    public static bool IsFileMatch(string fileName, List<Regex> includeFileContainsPatternList, List<Regex> excludeFileContainsPatternList)
    {
        var checkContent = includeFileContainsPatternList.Any() || excludeFileContainsPatternList.Any();
        if (!checkContent) return true;

        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {fileName} ...");

            var content = File.ReadAllText(fileName, Encoding.UTF8);
            var includeFile = includeFileContainsPatternList.All(regex => regex.IsMatch(content));
            var excludeFile = excludeFileContainsPatternList.Count > 0 && excludeFileContainsPatternList.Any(regex => regex.IsMatch(content));

            return includeFile && !excludeFile;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    public static string GetFileNameFromTemplate(string fileName, string template)
    {
        string filePath = Path.GetDirectoryName(fileName);
        string fileBase = Path.GetFileNameWithoutExtension(fileName);
        string fileExt = Path.GetExtension(fileName).TrimStart('.');
        string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        ConsoleHelpers.PrintDebugLine($"filePath: {filePath}");
        ConsoleHelpers.PrintDebugLine($"fileBase: {fileBase}");
        ConsoleHelpers.PrintDebugLine($"fileExt: {fileExt}");
        ConsoleHelpers.PrintDebugLine($"timeStamp: {timeStamp}");

        return template
            .Replace("{fileName}", fileName)
            .Replace("{filename}", fileName)
            .Replace("{filePath}", filePath)
            .Replace("{filepath}", filePath)
            .Replace("{fileBase}", fileBase)
            .Replace("{filebase}", fileBase)
            .Replace("{fileExt}", fileExt)
            .Replace("{fileext}", fileExt)
            .Replace("{timeStamp}", timeStamp)
            .Replace("{timestamp}", timeStamp)
            .Trim(' ', '/', '\\');
    }
    
    public static IEnumerable<string> FilesFromGlobs(List<string> globs)
    {
        foreach (var glob in globs)
        {
            foreach (var file in FilesFromGlob(glob))
            {
                yield return file;
            }
        }
    }

    public static IEnumerable<string> FilesFromGlob(string glob)
    {
        ConsoleHelpers.PrintStatus($"Finding files: {glob} ...");
        try
        {
            var matcher = new Microsoft.Extensions.FileSystemGlobbing.Matcher();
            matcher.AddInclude(MakeRelativePath(glob));

            var directoryInfo = new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(Directory.GetCurrentDirectory()));
            var matchResult = matcher.Execute(directoryInfo);

            return matchResult.Files.Select(file => MakeRelativePath(Path.Combine(Directory.GetCurrentDirectory(), file.Path)));
        }
        catch (Exception)
        {
            return Enumerable.Empty<string>();
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    public static IEnumerable<string> FindMatchingFiles(
        List<string> globs,
        List<string> excludeGlobs,
        List<Regex> excludeFileNamePatternList,
        List<Regex> includeFileContainsPatternList,
        List<Regex> excludeFileContainsPatternList)
    {
        var excludeFiles = new HashSet<string>(FilesFromGlobs(excludeGlobs));
        var files = FilesFromGlobs(globs)
            .Where(file => !excludeFiles.Contains(file))
            .Where(file => !excludeFileNamePatternList.Any(regex => regex.IsMatch(Path.GetFileName(file))))
            .ToList();

        if (files.Count == 0)
        {
            ConsoleHelpers.PrintLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files found\n");
            return Enumerable.Empty<string>();
        }

        var filtered = files.Where(file => IsFileMatch(file, includeFileContainsPatternList, excludeFileContainsPatternList)).ToList();
        if (filtered.Count == 0)
        {
            ConsoleHelpers.PrintLine($"## Pattern: {string.Join(" ", globs)}\n\n - No files matched criteria\n");
            return Enumerable.Empty<string>();
        }

        return filtered.Distinct();
    }

    public static string MakeRelativePath(string fullPath)
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
}