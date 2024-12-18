using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (!InputOptions.Parse(args, out var inputOptions, out var ex))
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

        ConsoleHelpers.Configure(inputOptions.Debug, inputOptions.Verbose);

        var shouldSaveOptions = !string.IsNullOrEmpty(inputOptions.SaveOptionsTemplate);
        if (shouldSaveOptions)
        {
            var filesSaved = inputOptions.SaveOptions(inputOptions.SaveOptionsTemplate);

            PrintBanner();
            PrintSavedOptionFiles(filesSaved);

            return 0;
        }

        var threadCountMax = inputOptions.Groups.Max(x => x.ThreadCount);
        var parallelism = threadCountMax > 0 ? threadCountMax : Environment.ProcessorCount;

        var tasks = new List<Task<string>>();
        var throttler = new SemaphoreSlim(parallelism);

        foreach (var group in inputOptions.Groups)
        {
            var files = FileHelpers.FindMatchingFiles(
                group.Globs,
                group.ExcludeGlobs,
                group.ExcludeFileNamePatternList,
                group.IncludeFileContainsPatternList,
                group.ExcludeFileContainsPatternList);

            var delayOutputToApplyInstructions = group.InstructionsList.Any();
            var tasksThisGroup = new List<Task<string>>();
            foreach (var file in files)
            {
                var getCheckSaveTask = GetCheckSaveFileContentAsync(
                    file,
                    throttler,
                    group.IncludeLineContainsPatternList,
                    group.IncludeLineCountBefore,
                    group.IncludeLineCountAfter,
                    group.IncludeLineNumbers,
                    group.RemoveAllLineContainsPatternList,
                    group.FileInstructionsList,
                    group.UseBuiltInFunctions,
                    group.SaveFileOutput);
                
                var taskToAdd = delayOutputToApplyInstructions
                    ? getCheckSaveTask
                    : getCheckSaveTask.ContinueWith(t => {
                        ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                        return t.Result;
                    });

                tasks.Add(taskToAdd);
                tasksThisGroup.Add(taskToAdd);
            }

            var shouldSaveOutput = !string.IsNullOrEmpty(group.SaveOutput);
            if (shouldSaveOutput || delayOutputToApplyInstructions)
            {
                await Task.WhenAll(tasksThisGroup.ToArray());
                var groupOutput = string.Join("\n", tasksThisGroup.Select(t => t.Result));

                if (delayOutputToApplyInstructions)
                {
                    groupOutput = AiInstructionProcessor.ApplyAllInstructions(group.InstructionsList, groupOutput, group.UseBuiltInFunctions);
                    ConsoleHelpers.PrintLine(groupOutput);
                }

                if (shouldSaveOutput)
                {
                    var saveFileName = FileHelpers.GetFileNameFromTemplate("output.md", group.SaveOutput);
                    File.WriteAllText(saveFileName, groupOutput);
                }
            }
        }

        await Task.WhenAll(tasks.ToArray());
        ConsoleHelpers.PrintStatusErase();

        return 0;
    }

    private static void PrintBanner()
    {
        ConsoleHelpers.PrintLine(
            "MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    public static void PrintException(InputException ex)
    {
        ConsoleHelpers.PrintLine($"{ex.Message}\n\n");
    }

    private static void PrintUsage()
    {
        var processorCount = Environment.ProcessorCount;
        ConsoleHelpers.PrintLine(
            "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]\n\n" +
            "OPTIONS\n\n" +
            "  --contains REGEX             Match only files and lines that contain the specified regex pattern\n\n" +
            "  --file-contains REGEX        Match only files that contain the specified regex pattern\n" +
            "  --file-not-contains REGEX    Exclude files that contain the specified regex pattern\n" +
            "  --exclude PATTERN            Exclude files that match the specified pattern\n\n" +
            "  --line-contains REGEX        Match only lines that contain the specified regex pattern\n" +
            "  --lines-before N             Include N lines before matching lines (default 0)\n" +
            "  --lines-after N              Include N lines after matching lines (default 0)\n" +
            "  --lines N                    Include N lines both before and after matching lines\n\n" +
            "  --line-numbers               Include line numbers in the output\n" +
            "  --remove-all-lines REGEX     Remove lines that contain the specified regex pattern\n\n" +
            "  --built-in-functions         Enable built-in functions in AI CLI (file system access)\n" +
            "  --file-instructions \"...\"    Apply the specified instructions to each file using AI CLI (e.g., @file)\n" +
            "  --instructions \"...\"         Apply the specified instructions to the entire output using AI CLI\n" +
            $"  --threads N                  Limit the number of concurrent file processing threads (default {processorCount})\n\n" +
            "  --save-file-output FILENAME  Save file output to the specified file (e.g. {filePath}/{fileBase}-output.md)\n" +
            "  --save-output FILENAME       Save the entire output to the specified file\n\n" +
            "  @ARGUMENTS\n\n" +
            "    Arguments starting with @ (e.g. @file) will use file content as argument.\n" +
            "    Arguments starting with @@ (e.g. @@file) will use file content as arguments line by line.\n\n" +
            "EXAMPLES\n\n" +
            "  mdcc file1.cs\n" +
            "  mdcc file1.md file2.md\n" +
            "  mdcc @@filelist.txt\n\n" +
            "  mdcc \"src/**/*.cs\" \"*.md\"\n" +
            "  mdcc \"src/**/*.js\" --contains \"export\"\n" +
            "  mdcc \"src/**\" --contains \"(?i)LLM\" --lines 2\n" +
            "  mdcc \"src/**\" --file-not-contains \"TODO\" --exclude \"drafts/*\"\n" +
            "  mdcc \"*.cs\" --remove-all-lines \"^\\s*//\"\n\n" +
            "  mdcc \"**/*.json\" --file-instructions \"convert the JSON to YAML\"\n" +
            "  mdcc \"**/*.json\" --file-instructions @instructions.md --threads 5\n" +
            "  mdcc \"**/*.cs\" --file-instructions @step1-instructions.md @step2-instructions.md\n" +
            "  mdcc \"**/*.py\" --file-instructions @instructions --save-file-output \"{filePath}/{fileBase}-{timeStamp}.md\"\n" +
            "  mdcc README.md \"**/*.cs\" --instructions \"Output only an updated README.md\""
        );
    }

    private static void PrintSavedOptionFiles(List<string> filesSaved)
    {
        var firstFileSaved = filesSaved.First();
        ConsoleHelpers.PrintLine($"Saved: {firstFileSaved}");

        foreach (var additionalFile in filesSaved.Skip(1))
        {
            ConsoleHelpers.PrintLine($"  and: {additionalFile}");
        }

        ConsoleHelpers.PrintLine("\nUSAGE: mdcc @@" + firstFileSaved);
    }

    private static Task<string> GetCheckSaveFileContentAsync(string fileName, SemaphoreSlim throttler, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<string> fileInstructionsList, bool useBuiltInFunctions, string saveFileOutput)
    {
        var getCheckSaveFileContent = new Func<string>(() =>
            GetCheckSaveFileContent(
                fileName,
                includeLineContainsPatternList,
                includeLineCountBefore,
                includeLineCountAfter,
                includeLineNumbers,
                removeAllLineContainsPatternList,
                fileInstructionsList,
                useBuiltInFunctions,
                saveFileOutput));

        if (!fileInstructionsList.Any())
        {
            var content = getCheckSaveFileContent();
            return Task.FromResult(content);
        }

        return Task.Run(async () => {
            await throttler.WaitAsync();
            try
            {
                return getCheckSaveFileContent();
            }
            finally
            {
                throttler.Release();
            }
        });
    }

    private static string GetCheckSaveFileContent(string fileName, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<string> fileInstructionsList, bool useBuiltInFunctions, string saveFileOutput)
    {
        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {fileName} ...");
            var finalContent = GetFinalFileContent(
                fileName,
                includeLineContainsPatternList,
                includeLineCountBefore,
                includeLineCountAfter,
                includeLineNumbers,
                removeAllLineContainsPatternList,
                fileInstructionsList, 
                useBuiltInFunctions);

            if (!string.IsNullOrEmpty(saveFileOutput))
            {
                var saveFileName = FileHelpers.GetFileNameFromTemplate(fileName, saveFileOutput);
                File.WriteAllText(saveFileName, finalContent);
                ConsoleHelpers.PrintStatus($"Saving to: {saveFileName} ... Done!");
            }

            return finalContent;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    private static string GetFinalFileContent(string fileName, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<string> fileInstructionsList, bool useBuiltInFunctions)
    {
        var formatted = GetFormattedFileContent(
            fileName,
            includeLineContainsPatternList,
            includeLineCountBefore,
            includeLineCountAfter,
            includeLineNumbers,
            removeAllLineContainsPatternList);

        var afterInstructions = fileInstructionsList.Any()
            ? AiInstructionProcessor.ApplyAllInstructions(fileInstructionsList, formatted, useBuiltInFunctions)
            : formatted;

        return afterInstructions;
    }

    private static string GetFormattedFileContent(string fileName, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList)
    {
        try
        {
            var isStdin = fileName == "-";
            var isBinary = !isStdin && File.ReadAllBytes(fileName).Any(x => x == 0);
            if (isBinary) return string.Empty;

            var content = FileHelpers.ReadAllText(fileName);

            var isMarkdown = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            var backticks = isMarkdown || isStdin
                ? new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(content))
                : "```";

            var filterContent = includeLineContainsPatternList.Any() || removeAllLineContainsPatternList.Any();
            if (filterContent)
            {
                content = GetContentFilteredAndFormatted(content, includeLineContainsPatternList, includeLineCountBefore, includeLineCountAfter, includeLineNumbers, removeAllLineContainsPatternList, backticks);
            }
            else if (includeLineNumbers)
            {
                content = GetContentFormattedWithLineNumbers(content);
            }

            return $"## {fileName}\n\n{backticks}\n{content}\n{backticks}\n";
        }
        catch (Exception ex)
        {
            return $"## {fileName} - Error reading file: {ex.Message}\n\n";
        }
    }

    private static string GetContentFormattedWithLineNumbers(string content)
    {
        var lines = content.Split('\n');
        content = string.Join('\n', lines.Select((line, index) => $"{index + 1}: {line}"));
        return content;
    }

    private static string GetContentFilteredAndFormatted(string content, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, string backticks)
    {
        // Find the matching lines/indices (line numbers are 1-based, indices are 0-based)
        var allLines = content.Split('\n');
        var matchedLineIndices = allLines.Select((line, index) => new { line, index })
            .Where(x => LineHelpers.IsLineMatch(x.line, includeLineContainsPatternList, removeAllLineContainsPatternList))
            .Select(x => x.index)
            .ToList();
        if (matchedLineIndices.Count == 0) return string.Empty;

        // Expand the range of lines, based on before and after counts
        var linesToInclude = new HashSet<int>(matchedLineIndices);
        foreach (var index in matchedLineIndices)
        {
            for (int b = 1; b <= includeLineCountBefore; b++)
            {
                var idxBefore = index - b;
                if (idxBefore >= 0) linesToInclude.Add(idxBefore);
            }

            for (int a = 1; a <= includeLineCountAfter; a++)
            {
                var idxAfter = index + a;
                if (idxAfter < allLines.Length) linesToInclude.Add(idxAfter);
            }
        }
        var expandedLineIndices = linesToInclude.OrderBy(i => i).ToList();

        var checkForLineNumberBreak = includeLineCountBefore + includeLineCountAfter > 0;
        int? previousLineIndex = null;

        // Loop through the lines to include and accumulate the output
        var output = new List<string>();
        foreach (var index in expandedLineIndices)
        {
            var addSeparatorForLineNumberBreak = checkForLineNumberBreak && previousLineIndex != null && index > previousLineIndex + 1;
            if (addSeparatorForLineNumberBreak)
            {
                output.Add($"{backticks}\n\n{backticks}");
            }

            var line = allLines[index];
            var shouldRemoveLine = removeAllLineContainsPatternList.Any(regex => regex.IsMatch(line));

            if (includeLineNumbers)
            {
                var lineNumber = index + 1;
                output.Add(shouldRemoveLine
                    ? $"{lineNumber}:"
                    : $"{lineNumber}: {line}");
            }
            else if (!shouldRemoveLine)
            {
                output.Add(line);
            }

            previousLineIndex = index;
        }

        return string.Join("\n", output);
    }
}
