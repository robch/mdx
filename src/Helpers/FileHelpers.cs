using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class FileHelpers
{
    public static void EnsureDirectoryExists(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }

    public static bool IsFileMatch(string fileName, List<Regex> includeFileContainsPatternList, List<Regex> excludeFileContainsPatternList)
    {
        var checkContent = includeFileContainsPatternList.Any() || excludeFileContainsPatternList.Any();
        if (!checkContent) return true;

        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {fileName} ...");

            var content = ReadAllText(fileName);
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
            if (glob == "-") return [ glob ]; // special case for stdin

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

        ConsoleHelpers.PrintDebugLine($"DEBUG: 1: Found files ({files.Count()}): ");
        files.ForEach(x => ConsoleHelpers.PrintDebugLine($"DEBUG: 1: - {x}"));
        ConsoleHelpers.PrintDebugLine("");

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

        var distinct = filtered.Distinct().ToList();
        ConsoleHelpers.PrintDebugLine($"DEBUG: 2: Found files ({distinct.Count()} distinct/filtered): ");
        distinct.ForEach(x => ConsoleHelpers.PrintDebugLine($"DEBUG: 2: - {x}"));

        return distinct;
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

    public static string ReadAllText(string fileName)
    {
        var content = fileName == "-"
            ? string.Join("\n", ConsoleHelpers.GetAllLinesFromStdin())
            : File.ReadAllText(fileName, Encoding.UTF8);

        return content;
    }

    public static string ReadAllText(string fileName, out bool isStdin, out bool isMarkdown, out bool isBinary)
    {
        isStdin = fileName == "-";
        isBinary = !isStdin && File.ReadAllBytes(fileName).Any(x => x == 0);
        isMarkdown = isBinary || fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

        return !isBinary
            ? FileHelpers.ReadAllText(fileName)
            : FileConverters.ConvertToMarkdown(fileName);
    }

    public static string GetFriendlyLastModified(FileInfo fileInfo)
    {
        var modified = fileInfo.LastWriteTime;
        var modifiedSeconds = (int)((DateTime.Now - modified).TotalSeconds);
        var modifiedMinutes = modifiedSeconds / 60;
        var modifiedHours = modifiedSeconds / 3600;
        var modifiedDays = modifiedSeconds / 86400;

        var formatted =
            modifiedMinutes < 1 ? "just now" :
            modifiedMinutes == 1 ? "1 minute ago" :
            modifiedMinutes < 60 ? $"{modifiedMinutes} minutes ago" :
            modifiedHours == 1 ? "1 hour ago" :
            modifiedHours < 24 ? $"{modifiedHours} hours ago" :
            modifiedDays == 1 ? "1 day ago" :
            modifiedDays < 7 ? $"{modifiedDays} days ago" :
            modified.ToString();

        return formatted;
    }

    public static string GetFriendlySize(FileInfo fileInfo)
    {
        var size = fileInfo.Length;
        var sizeFormatted = size >= 1024 * 1024 * 1024
            ? $"{size / (1024 * 1024 * 1024)} GB"
            : size >= 1024 * 1024
                ? $"{size / (1024 * 1024)} MB"
                : size >= 1024
                    ? $"{size / 1024} KB"
                    : $"{size} bytes";
        return sizeFormatted;
    }

    public static string GenerateUniqueFileNameFromUrl(string url, string saveToFolder)
    {
        FileHelpers.EnsureDirectoryExists(saveToFolder);

        var uri = new Uri(url);
        var path = uri.Host + uri.AbsolutePath + uri.Query;

        var parts = path.Split(_invalidFileNameCharsForWeb, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var check = Path.Combine(saveToFolder, string.Join("-", parts));
        if (!File.Exists(check)) return check;

        while (true)
        {
            var guidPart = Guid.NewGuid().ToString().Substring(0, 8);
            var fileTimePart = DateTime.Now.ToFileTimeUtc().ToString();
            var tryThis = check + "-" + fileTimePart + "-" + guidPart;
            if (!File.Exists(tryThis)) return tryThis;
        }
    }

    private static char[] GetInvalidFileNameCharsForWeb()
    {
        var invalidCharList = Path.GetInvalidFileNameChars().ToList();
        for (char c = (char)0; c < 128; c++)
        {
            if (!char.IsLetterOrDigit(c)) invalidCharList.Add(c);
        }
        return invalidCharList.Distinct().ToArray();
    }

    private static char[] _invalidFileNameCharsForWeb = GetInvalidFileNameCharsForWeb();
}