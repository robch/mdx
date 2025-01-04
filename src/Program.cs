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
        if (!CommandLineOptions.Parse(args, out var commandLineOptions, out var ex))
        {
            PrintBanner();
            if (ex != null)
            {
                PrintException(ex);
                PrintUsage();
                return 2;
            }
            else
            {
                PrintUsage();
                return 1;
            }
        }

        ConsoleHelpers.Configure(commandLineOptions.Debug, commandLineOptions.Verbose);

        var shouldSaveOptions = !string.IsNullOrEmpty(commandLineOptions.SaveOptionsTemplate);
        if (shouldSaveOptions)
        {
            var filesSaved = commandLineOptions.SaveOptions(commandLineOptions.SaveOptionsTemplate);

            PrintBanner();
            PrintSavedOptionFiles(filesSaved);

            return 0;
        }

        var threadCountMax = commandLineOptions.Commands.OfType<FindFilesCommand>().Max(x => x.ThreadCount);
        var parallelism = threadCountMax > 0 ? threadCountMax : Environment.ProcessorCount;

        var tasks = new List<Task<string>>();
        var throttler = new SemaphoreSlim(parallelism);

        foreach (var command in commandLineOptions.Commands)
        {
            bool delayOutputToApplyInstructions;
            List<Task<string>> tasksThisCommand;

            var findFileCommand = command as FindFilesCommand;
            if (findFileCommand != null)
            {
                HandleFindFileCommand(commandLineOptions, findFileCommand, tasks, throttler, out delayOutputToApplyInstructions, out tasksThisCommand);
            }
            else
            {
                delayOutputToApplyInstructions = false;
                tasksThisCommand = new();
            }

            var shouldSaveOutput = !string.IsNullOrEmpty(findFileCommand.SaveOutput);
            if (shouldSaveOutput || delayOutputToApplyInstructions)
            {
                await Task.WhenAll(tasksThisCommand.ToArray());
                var commandOutput = string.Join("\n", tasksThisCommand.Select(t => t.Result));

                if (delayOutputToApplyInstructions)
                {
                    commandOutput = AiInstructionProcessor.ApplyAllInstructions(findFileCommand.InstructionsList, commandOutput, findFileCommand.UseBuiltInFunctions);
                    ConsoleHelpers.PrintLine(commandOutput);
                }

                if (shouldSaveOutput)
                {
                    var saveFileName = FileHelpers.GetFileNameFromTemplate("output.md", findFileCommand.SaveOutput);
                    File.WriteAllText(saveFileName, commandOutput);
                }
            }
        }

        await Task.WhenAll(tasks.ToArray());
        ConsoleHelpers.PrintStatusErase();

        return 0;
    }

    private static void HandleFindFileCommand(CommandLineOptions commandLineOptions, FindFilesCommand findFileCommand, List<Task<string>> tasks, SemaphoreSlim throttler, out bool delayOutputToApplyInstructions, out List<Task<string>> tasksThisCommand)
    {
        var files = FileHelpers.FindMatchingFiles(
            findFileCommand.Globs,
            findFileCommand.ExcludeGlobs,
            findFileCommand.ExcludeFileNamePatternList,
            findFileCommand.IncludeFileContainsPatternList,
            findFileCommand.ExcludeFileContainsPatternList)
            .ToList();

        delayOutputToApplyInstructions = findFileCommand.InstructionsList.Any();
        tasksThisCommand = new List<Task<string>>();
        foreach (var file in files)
        {
            var onlyOneFile = files.Count == 1 && commandLineOptions.Commands.Count == 1;
            var skipMarkdownWrapping = onlyOneFile && FileConverters.CanConvert(file);
            var wrapInMarkdown = !skipMarkdownWrapping; ;

            var getCheckSaveTask = GetCheckSaveFileContentAsync(
                file,
                throttler,
                wrapInMarkdown,
                findFileCommand.IncludeLineContainsPatternList,
                findFileCommand.IncludeLineCountBefore,
                findFileCommand.IncludeLineCountAfter,
                findFileCommand.IncludeLineNumbers,
                findFileCommand.RemoveAllLineContainsPatternList,
                findFileCommand.FileInstructionsList,
                findFileCommand.UseBuiltInFunctions,
                findFileCommand.SaveFileOutput);

            var taskToAdd = delayOutputToApplyInstructions
                ? getCheckSaveTask
                : getCheckSaveTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                });

            tasks.Add(taskToAdd);
            tasksThisCommand.Add(taskToAdd);
        }
    }

    private static void PrintBanner()
    {
        ConsoleHelpers.PrintLine(
            "MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    private static void PrintException(InputException ex)
    {
        var printMessage = !string.IsNullOrEmpty(ex.Message) && !(ex is HelpRequestedInputException);
        if (printMessage) ConsoleHelpers.PrintLine($"  {ex.Message}\n\n");
    }

    private static void PrintUsage()
    {
        var processorCount = Environment.ProcessorCount;
        ConsoleHelpers.PrintLine(
            "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]\n\n" +
            "OPTIONS\n\n" +
            "  --contains REGEX               Match only files and lines that contain the specified regex pattern\n\n" +
            "  --file-contains REGEX          Match only files that contain the specified regex pattern\n" +
            "  --file-not-contains REGEX      Exclude files that contain the specified regex pattern\n" +
            "  --exclude PATTERN              Exclude files that match the specified pattern\n\n" +
            "  --line-contains REGEX          Match only lines that contain the specified regex pattern\n" +
            "  --lines-before N               Include N lines before matching lines (default 0)\n" +
            "  --lines-after N                Include N lines after matching lines (default 0)\n" +
            "  --lines N                      Include N lines both before and after matching lines\n\n" +
            "  --line-numbers                 Include line numbers in the output\n" +
            "  --remove-all-lines REGEX       Remove lines that contain the specified regex pattern\n\n" +
            "  --built-in-functions           Enable built-in functions in AI CLI (file system access)\n" +
            "  --instructions \"...\"           Apply the specified instructions to all output using AI CLI\n" +
            "  --file-instructions \"...\"      Apply the specified instructions to each file using AI CLI\n" +
            "  --EXT-file-instructions \"...\"  Apply the specified instructions to each file with the specified extension\n\n" +
            $"  --threads N                    Limit the number of concurrent file processing threads (default {processorCount})\n\n" +
            "  --save-file-output FILENAME    Save file output to the specified file (e.g. " + CommandLineOptions.DefaultSaveFileOutputTemplate + ")\n" +
            "  --save-output FILENAME         Save the entire output to the specified file\n\n" +
            "  --save-options FILENAME        Save the current options to the specified file\n\n" +
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
            "  mdcc \"**/*\" --cs-file-instructions \"Only keep public methods\"\n" +
            "  mdcc README.md \"**/*.cs\" --instructions \"Output only an updated README.md\""
        );
    }

    private static void PrintSavedOptionFiles(List<string> filesSaved)
    {
        var firstFileSaved = filesSaved.First();
        var additionalFiles = filesSaved.Skip(1).ToList();

        var savedAsDefault = firstFileSaved == CommandLineOptions.DefaultOptionsFileName;
        ConsoleHelpers.PrintLine(savedAsDefault
            ? $"Saved: {firstFileSaved} (default options file)\n"
            : $"Saved: {firstFileSaved}\n");

        var hasAdditionalFiles = additionalFiles.Any();
        if (hasAdditionalFiles)
        {
            foreach (var additionalFile in additionalFiles)
            {
                ConsoleHelpers.PrintLine($"  and: {additionalFile}");
            }
         
            ConsoleHelpers.PrintLine();
        }

        if (savedAsDefault)
        {
            ConsoleHelpers.PrintLine("NOTE: These options will be used by default when invoking mdcc in this directory.");
            ConsoleHelpers.PrintLine("      To stop using these options by default, delete the file: " + firstFileSaved);
        }
        else
        {
            ConsoleHelpers.PrintLine("USAGE: mdcc @@" + firstFileSaved);
        }
    }

    private static Task<string> GetCheckSaveFileContentAsync(
        string fileName,
        SemaphoreSlim throttler,
        bool wrapInMarkdown,
        List<Regex> includeLineContainsPatternList,
        int includeLineCountBefore,
        int includeLineCountAfter,
        bool includeLineNumbers,
        List<Regex> removeAllLineContainsPatternList,
        List<Tuple<string, string>> fileInstructionsList,
        bool useBuiltInFunctions,
        string saveFileOutput)
    {
        var getCheckSaveFileContent = new Func<string>(() =>
            GetCheckSaveFileContent(
                fileName,
                wrapInMarkdown,
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

    private static string GetCheckSaveFileContent(
        string fileName,
        bool wrapInMarkdown,
        List<Regex> includeLineContainsPatternList,
        int includeLineCountBefore,
        int includeLineCountAfter,
        bool includeLineNumbers,
        List<Regex> removeAllLineContainsPatternList,
        List<Tuple<string, string>> fileInstructionsList,
        bool useBuiltInFunctions,
        string saveFileOutput)
    {
        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {fileName} ...");
            var finalContent = GetFinalFileContent(
                fileName,
                wrapInMarkdown,
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

    private static string GetFinalFileContent(
        string fileName,
        bool wrapInMarkdown,
        List<Regex> includeLineContainsPatternList,
        int includeLineCountBefore,
        int includeLineCountAfter,
        bool includeLineNumbers,
        List<Regex> removeAllLineContainsPatternList,
        List<Tuple<string, string>> fileInstructionsList,
        bool useBuiltInFunctions)
    {
        var formatted = GetFormattedFileContent(
            fileName,
            wrapInMarkdown,
            includeLineContainsPatternList,
            includeLineCountBefore,
            includeLineCountAfter,
            includeLineNumbers,
            removeAllLineContainsPatternList);

        var instructionsForThisFile = fileInstructionsList
            .Where(x => FileNameMatchesInstructionsCriteria(fileName, x.Item2))
            .Select(x => x.Item1)
            .ToList();

        var afterInstructions = instructionsForThisFile.Any()
            ? AiInstructionProcessor.ApplyAllInstructions(instructionsForThisFile, formatted, useBuiltInFunctions)
            : formatted;

        return afterInstructions;
    }

    private static bool FileNameMatchesInstructionsCriteria(string fileName, string fileNameCriteria)
    {
        return string.IsNullOrEmpty(fileNameCriteria) ||
            fileName.EndsWith($".{fileNameCriteria}") ||
            fileName == fileNameCriteria;
    }

    private static string GetFormattedFileContent(
        string fileName,
        bool wrapInMarkdown,
        List<Regex> includeLineContainsPatternList,
        int includeLineCountBefore,
        int includeLineCountAfter,
        bool includeLineNumbers,
        List<Regex> removeAllLineContainsPatternList)
    {
        try
        {
            var content = FileHelpers.ReadAllText(fileName, out var isMarkdown, out var isStdin, out var isBinary);
            if (content == null) return string.Empty;
            
            var backticks = isMarkdown || isStdin
                ? new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(content))
                : "```";

            var filterContent = includeLineContainsPatternList.Any() || removeAllLineContainsPatternList.Any();
            if (filterContent)
            {
                content = GetContentFilteredAndFormatted(content, includeLineContainsPatternList, includeLineCountBefore, includeLineCountAfter, includeLineNumbers, removeAllLineContainsPatternList, backticks);
                wrapInMarkdown = true;
            }
            else if (includeLineNumbers)
            {
                content = GetContentFormattedWithLineNumbers(content);
                wrapInMarkdown = true;
            }

            if (wrapInMarkdown)
            {
                if (fileName != "-")
                {
                    var fileInfo = new FileInfo(fileName);
                    var modified = FileHelpers.GetFriendlyLastModified(fileInfo);
                    var size = FileHelpers.GetFriendlySize(fileInfo);

                    content = $"## {fileName}\n\nModified: {modified}\nSize: {size}\n\n{backticks}\n{content}\n{backticks}\n";
                }
                else
                {
                    content = $"## (stdin)\n\n{backticks}\n{content}\n{backticks}\n";
                }
            }

            return content;
        }
        catch (Exception ex)
        {
            return $"## {fileName} - Error reading file: {ex.Message}\n\n{ex.StackTrace}";
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
