using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

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
                PrintUsage(ex.GetCommand());
                return 2;
            }
            else
            {
                PrintUsage(commandLineOptions.HelpRequested);
                return 1;
            }
        }

        ConsoleHelpers.Configure(commandLineOptions.Debug, commandLineOptions.Verbose);

        var helpCommand = commandLineOptions.Commands.OfType<HelpCommand>().FirstOrDefault();
        if (helpCommand != null)
        {
            PrintBanner();
            PrintHelpTopic(commandLineOptions.HelpRequested);
            return 0;
        }

        var shouldSaveOptions = !string.IsNullOrEmpty(commandLineOptions.SaveOptionsTemplate);
        if (shouldSaveOptions)
        {
            var filesSaved = commandLineOptions.SaveOptions(commandLineOptions.SaveOptionsTemplate);

            PrintBanner();
            PrintSavedOptionFiles(filesSaved);

            return 0;
        }

        var threadCountMax = commandLineOptions.Commands.Max(x => x.ThreadCount);
        var parallelism = threadCountMax > 0 ? threadCountMax : Environment.ProcessorCount;

        var allTasks = new List<Task<string>>();
        var throttler = new SemaphoreSlim(parallelism);

        foreach (var command in commandLineOptions.Commands)
        {
            bool delayOutputToApplyInstructions = command.InstructionsList.Any();

            var tasksThisCommand = command switch
            {
                FindFilesCommand findFilesCommand => HandleFindFileCommand(commandLineOptions, findFilesCommand, throttler, delayOutputToApplyInstructions),
                WebSearchCommand webSearchCommand => await HandleWebSearchCommandAsync(commandLineOptions, webSearchCommand, throttler, delayOutputToApplyInstructions),
                WebGetCommand webGetCommand => HandleWebGetCommand(commandLineOptions, webGetCommand, throttler, delayOutputToApplyInstructions),
                _ => new List<Task<string>>()
            };

            allTasks.AddRange(tasksThisCommand);

            var shouldSaveOutput = !string.IsNullOrEmpty(command.SaveOutput);
            if (shouldSaveOutput || delayOutputToApplyInstructions)
            {
                await Task.WhenAll(tasksThisCommand.ToArray());
                var commandOutput = string.Join("\n", tasksThisCommand.Select(t => t.Result));

                if (delayOutputToApplyInstructions)
                {
                    commandOutput = AiInstructionProcessor.ApplyAllInstructions(command.InstructionsList, commandOutput, command.UseBuiltInFunctions);
                    ConsoleHelpers.PrintLine(commandOutput);
                }

                if (shouldSaveOutput)
                {
                    var saveFileName = FileHelpers.GetFileNameFromTemplate("output.md", command.SaveOutput);
                    File.WriteAllText(saveFileName, commandOutput);
                }
            }
        }

        await Task.WhenAll(allTasks.ToArray());
        ConsoleHelpers.PrintStatusErase();

        return 0;
    }

    private static void PrintBanner()
    {
        var programNameUppercase = ProgramName.ToUpper();
        ConsoleHelpers.PrintLine(
            $"{programNameUppercase} - Markdown Context Creator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    private static void PrintException(CommandLineException ex)
    {
        var printMessage = !string.IsNullOrEmpty(ex.Message);
        if (printMessage) ConsoleHelpers.PrintLine($"  {ex.Message}\n\n");
    }

    private static void PrintUsage(string command)
    {
        var validTopic = !string.IsNullOrEmpty(command) && FileHelpers.FindHelpTopic(command);
        var helpContent = validTopic
            ? FileHelpers.GetHelpTopicText(command)
            : FileHelpers.GetHelpTopicText(UsageHelpTopic);

        helpContent ??=
            $"USAGE: {ProgramName} [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]\n" +
            $"   OR: {ProgramName} web search \"TERMS\" [...]\n" +
            $"   OR: {ProgramName} web get \"URL\" [...]";

        ConsoleHelpers.PrintLine(helpContent.TrimEnd());
    }

    private static void PrintHelpTopic(string topic)
    {
        topic ??= UsageHelpTopic;

        var findHelpTopic = FileHelpers.FindHelpTopic(topic);
        if (!findHelpTopic)
        {
            ConsoleHelpers.PrintLine(
                $"  WARNING: No help topic found for '{topic}'\n\n" +
                $"      TRY: {ProgramName} help\n");
            return;
        }

        var helpContent = FileHelpers.GetHelpTopicText(topic);
        ConsoleHelpers.PrintLine(helpContent.TrimEnd());
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
            ConsoleHelpers.PrintLine($"NOTE: These options will be used by default when invoking {ProgramName} in this directory.");
            ConsoleHelpers.PrintLine($"      To stop using these options by default, delete the file: {firstFileSaved}");
        }
        else
        {
            ConsoleHelpers.PrintLine($"USAGE: {ProgramName} @@" + firstFileSaved);
        }
    }

    private static List<Task<string>> HandleFindFileCommand(CommandLineOptions commandLineOptions, FindFilesCommand findFilesCommand, SemaphoreSlim throttler, bool delayOutputToApplyInstructions)
    {
        var files = FileHelpers.FindMatchingFiles(
            findFilesCommand.Globs,
            findFilesCommand.ExcludeGlobs,
            findFilesCommand.ExcludeFileNamePatternList,
            findFilesCommand.IncludeFileContainsPatternList,
            findFilesCommand.ExcludeFileContainsPatternList)
            .ToList();

        var tasks = new List<Task<string>>();
        foreach (var file in files)
        {
            var onlyOneFile = files.Count == 1 && commandLineOptions.Commands.Count == 1;
            var skipMarkdownWrapping = onlyOneFile && FileConverters.CanConvert(file);
            var wrapInMarkdown = !skipMarkdownWrapping; ;

            var getCheckSaveTask = GetCheckSaveFileContentAsync(
                file,
                throttler,
                wrapInMarkdown,
                findFilesCommand.IncludeLineContainsPatternList,
                findFilesCommand.IncludeLineCountBefore,
                findFilesCommand.IncludeLineCountAfter,
                findFilesCommand.IncludeLineNumbers,
                findFilesCommand.RemoveAllLineContainsPatternList,
                findFilesCommand.FileInstructionsList,
                findFilesCommand.UseBuiltInFunctions,
                findFilesCommand.SaveFileOutput);

            var taskToAdd = delayOutputToApplyInstructions
                ? getCheckSaveTask
                : getCheckSaveTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                });

            tasks.Add(taskToAdd);
        }

        return tasks;
    }

    private static async Task<List<Task<string>>> HandleWebSearchCommandAsync(CommandLineOptions commandLineOptions, WebSearchCommand command, SemaphoreSlim throttler, bool delayOutputToApplyInstructions)
    {
        var searchEngine = command.UseBing ? "bing" : "google";
        var query = string.Join(" ", command.Terms);
        var maxResults = command.MaxResults;
        var getContent = command.GetContent;
        var stripHtml = command.StripHtml;
        var saveToFolder = command.SaveFolder;
        var headless = command.Headless;
        var pageInstructionsList = command.PageInstructionsList;
        var useBuiltInFunctions = command.UseBuiltInFunctions;
        var savePageOutput = command.SavePageOutput;

        var searchSectionHeader = $"## Web Search for '{query}' using {searchEngine}";

        var urls = await PlaywrightHelpers.GetWebSearchResultUrlsAsync(searchEngine, query, maxResults, headless);
        var searchSection = urls.Count == 0
            ? $"{searchSectionHeader}\n\nNo results found\n"
            : $"{searchSectionHeader}\n\n" + string.Join("\n", urls) + "\n";

        if (!delayOutputToApplyInstructions) ConsoleHelpers.PrintLine(searchSection);

        if (urls.Count == 0 || !getContent)
        {
            return new List<Task<string>>() { Task.FromResult(searchSection) };
        }

        var tasks = new List<Task<string>>();
        tasks.Add(Task.FromResult(searchSection));

        foreach (var url in urls)
        {
            var getCheckSaveTask = GetCheckSaveWebPageContentAsync(url, stripHtml, saveToFolder, headless, pageInstructionsList, useBuiltInFunctions, savePageOutput);
            var taskToAdd = delayOutputToApplyInstructions
                ? getCheckSaveTask
                : getCheckSaveTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                });

            tasks.Add(taskToAdd);
        }

        return tasks;
    }

    private static List<Task<string>> HandleWebGetCommand(CommandLineOptions commandLineOptions, WebGetCommand command, SemaphoreSlim throttler, bool delayOutputToApplyInstructions)
    {
        var urls = command.Urls;
        var stripHtml = command.StripHtml;
        var saveToFolder = command.SaveFolder;
        var headless = command.Headless;
        var pageInstructionsList = command.PageInstructionsList;
        var useBuiltInFunctions = command.UseBuiltInFunctions;
        var savePageOutput = command.SavePageOutput;

        var badUrls = command.Urls.Where(l => !l.StartsWith("http")).ToList();
        if (badUrls.Any())
        {
            var message = (badUrls.Count == 1)
                ? $"Invalid URL: {badUrls[0]}"
                : "Invalid URLs:\n" + string.Join(Environment.NewLine, badUrls.Select(url => "  " + url));
            return new List<Task<string>>() { Task.FromResult(message) };
        }

        var tasks = new List<Task<string>>();
        foreach (var url in urls)
        {
            var getCheckSaveTask = GetCheckSaveWebPageContentAsync(url, stripHtml, saveToFolder, headless, pageInstructionsList, useBuiltInFunctions, savePageOutput);
            var taskToAdd = delayOutputToApplyInstructions
                ? getCheckSaveTask
                : getCheckSaveTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                });

            tasks.Add(taskToAdd);
        }

        return tasks;
    }

    private static Task<string> GetCheckSaveFileContentAsync(string fileName, SemaphoreSlim throttler, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions, string saveFileOutput)
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

    private static string GetCheckSaveFileContent(string fileName, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions, string saveFileOutput)
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

    private static string GetFinalFileContent(string fileName, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions)
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

    private static string GetFormattedFileContent(string fileName, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList)
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
                content = GetContentFilteredAndFormatted(
                    content,
                    includeLineContainsPatternList,
                    includeLineCountBefore,
                    includeLineCountAfter,
                    includeLineNumbers,
                    removeAllLineContainsPatternList,
                    backticks);
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
                    var lang = FileHelpers.GetMarkdownLanguage(fileInfo.Extension);

                    content = $"## {fileName}\n\nModified: {modified}\nSize: {size}\n\n{backticks}{lang}\n{content}\n{backticks}\n";
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
        return string.Join('\n', lines.Select((line, index) => $"{index + 1}: {line}"));
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

        var checkForLineNumberBreak = (includeLineCountBefore + includeLineCountAfter) > 0;
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

    private static async Task<string> GetCheckSaveWebPageContentAsync(string url, bool stripHtml, string saveToFolder, bool headless, List<Tuple<string, string>> pageInstructionsList, bool useBuiltInFunctions, string savePageOutput)
    {
        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {url} ...");
            var finalContent = await GetFinalWebPageContentAsync(url, stripHtml, saveToFolder, headless, pageInstructionsList, useBuiltInFunctions);

            if (!string.IsNullOrEmpty(savePageOutput))
            {
                var fileName = FileHelpers.GenerateUniqueFileNameFromUrl(url, saveToFolder ?? "web-pages");
                var saveFileName = FileHelpers.GetFileNameFromTemplate(fileName, savePageOutput);
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

    private static async Task<string> GetFinalWebPageContentAsync(string url, bool stripHtml, string saveToFolder, bool headless, List<Tuple<string, string>> pageInstructionsList, bool useBuiltInFunctions)
    {
        var formatted = await GetFormattedWebPageContentAsync(url, stripHtml, saveToFolder, headless);

        var instructionsForThisPage = pageInstructionsList
            .Where(x => WebPageMatchesInstructionsCriteria(url, x.Item2))
            .Select(x => x.Item1)
            .ToList();

        var afterInstructions = instructionsForThisPage.Any()
            ? AiInstructionProcessor.ApplyAllInstructions(instructionsForThisPage, formatted, false)
            : formatted;

        return afterInstructions;
    }

    private static bool WebPageMatchesInstructionsCriteria(string url, string webPageCriteria)
    {
        return string.IsNullOrEmpty(webPageCriteria) ||
            url.Contains($".{webPageCriteria}") ||
            url == webPageCriteria;
    }

    private static async Task<string> GetFormattedWebPageContentAsync(string url, bool stripHtml, string saveToFolder, bool headless)
    {
        try
        {
            var (content, title) = await PlaywrightHelpers.GetPageAndTitle(url, stripHtml, saveToFolder, headless);

            var sb = new StringBuilder();
            sb.AppendLine($"## {title}\n");
            sb.AppendLine($"url: {url}\n");
            sb.AppendLine("```");
            sb.AppendLine(content);
            sb.AppendLine("```\n");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"## Error fetching {url}\n\n{ex.Message}\n{ex.StackTrace}";
        }
    }

    private const string ProgramName = "mdx";
    private const string UsageHelpTopic = "usage";
}
