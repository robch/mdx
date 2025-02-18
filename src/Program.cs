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
        var playwrightCommand = args.Length >= 1 && args[0] == "playwright";
        if (playwrightCommand) return PlaywrightHelpers.RunCli(args.Skip(1).ToArray());

        if (!CommandLineOptions.Parse(args, out var commandLineOptions, out var ex))
        {
            PrintBanner();
            if (ex != null)
            {
                PrintException(ex);
                HelpHelpers.PrintUsage(ex.GetCommand());
                return 2;
            }
            else
            {
                HelpHelpers.PrintUsage(commandLineOptions.HelpTopic);
                return 1;
            }
        }

        ConsoleHelpers.Configure(commandLineOptions.Debug, commandLineOptions.Verbose);
        BingApiWebSearchHelpers.ConfigureEndpoint(
            EnvironmentHelpers.FindEnvVar("BING_SEARCH_V7_ENDPOINT"),
            EnvironmentHelpers.FindEnvVar("BING_SEARCH_V7_KEY"));
        GoogleApiWebSearchHelpers.ConfigureEndpoint(
            EnvironmentHelpers.FindEnvVar("GOOGLE_SEARCH_ENDPOINT"),
            EnvironmentHelpers.FindEnvVar("GOOGLE_SEARCH_KEY"),
            EnvironmentHelpers.FindEnvVar("GOOGLE_SEARCH_ENGINE_ID"));
        OpenAIChatCompletionsClass.Configure(
            EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_ENDPOINT"),
            EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_API_KEY"),
            EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_CHAT_DEPLOYMENT"),
            EnvironmentHelpers.FindEnvVar("AZURE_OPENAI_SYSTEM_PROMPT"));

        var helpCommand = commandLineOptions.Commands.OfType<HelpCommand>().FirstOrDefault();
        if (helpCommand != null)
        {
            PrintBanner();
            HelpHelpers.PrintHelpTopic(commandLineOptions.HelpTopic, commandLineOptions.ExpandHelpTopics);
            return 0;
        }

        var shouldSaveAlias = !string.IsNullOrEmpty(commandLineOptions.SaveAliasName);
        if (shouldSaveAlias)
        {
            var filesSaved = commandLineOptions.SaveAlias(commandLineOptions.SaveAliasName);

            PrintBanner();
            PrintSavedAliasFiles(filesSaved);

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
                RunCommand runCommand => HandleRunCommand(commandLineOptions, runCommand, throttler, delayOutputToApplyInstructions),
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
                    commandOutput = AiInstructionProcessor.ApplyAllInstructions(command.InstructionsList, commandOutput, command.UseBuiltInFunctions, command.SaveChatHistory);
                    ConsoleHelpers.PrintLine(commandOutput);
                }

                if (shouldSaveOutput)
                {
                    var saveFileName = FileHelpers.GetFileNameFromTemplate("output.md", command.SaveOutput);
                    FileHelpers.WriteAllText(saveFileName, commandOutput);
                }
            }
        }

        await Task.WhenAll(allTasks.ToArray());
        ConsoleHelpers.PrintStatusErase();

        return 0;
    }

    private static void PrintBanner()
    {
        var programNameUppercase = Program.Name.ToUpper();
        ConsoleHelpers.PrintLine(
            $"{programNameUppercase} - The AI-Powered Markdown Generator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    private static void PrintException(CommandLineException ex)
    {
        var printMessage = !string.IsNullOrEmpty(ex.Message);
        if (printMessage) ConsoleHelpers.PrintLine($"  {ex.Message}\n\n");
    }

    private static void PrintSavedAliasFiles(List<string> filesSaved)
    {
        var firstFileSaved = filesSaved.First();
        var additionalFiles = filesSaved.Skip(1).ToList();

        ConsoleHelpers.PrintLine($"Saved: {firstFileSaved}\n");

        var hasAdditionalFiles = additionalFiles.Any();
        if (hasAdditionalFiles)
        {
            foreach (var additionalFile in additionalFiles)
            {
                ConsoleHelpers.PrintLine($"  and: {additionalFile}");
            }
         
            ConsoleHelpers.PrintLine();
        }

        var aliasName = Path.GetFileNameWithoutExtension(firstFileSaved);
        ConsoleHelpers.PrintLine($"USAGE: {Program.Name} [...] --" + aliasName);
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
                findFilesCommand.SaveChatHistory,
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
        var provider = command.SearchProvider;
        var query = string.Join(" ", command.Terms);
        var maxResults = command.MaxResults;
        var excludeURLContainsPatternList = command.ExcludeURLContainsPatternList;
        var getContent = command.GetContent;
        var stripHtml = command.StripHtml;
        var saveToFolder = command.SaveFolder;
        var browserType = command.Browser;
        var interactive = command.Interactive;
        var useBuiltInFunctions = command.UseBuiltInFunctions;
        var saveChatHistory = command.SaveChatHistory;

        var savePageOutput = command.SavePageOutput;
        var pageInstructionsList = command.PageInstructionsList
            .Select(x => Tuple.Create(
                x.Item1
                    .Replace("{searchTerms}", query)
                    .Replace("{query}", query)
                    .Replace("{terms}", query)
                    .Replace("{q}", query),
                x.Item2.ToLowerInvariant()))
            .ToList();

        var searchSectionHeader = $"## Web Search for '{query}' using {provider}";

        var urls = await WebSearchHelpers.GetWebSearchResultUrlsAsync(provider, query, maxResults, excludeURLContainsPatternList, browserType, interactive);
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
            var getCheckSaveTask = GetCheckSaveWebPageContentAsync(url, stripHtml, saveToFolder, browserType, interactive, pageInstructionsList, useBuiltInFunctions, saveChatHistory, savePageOutput);
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
        var browserType = command.Browser;
        var interactive = command.Interactive;
        var pageInstructionsList = command.PageInstructionsList;
        var useBuiltInFunctions = command.UseBuiltInFunctions;
        var saveChatHistory = command.SaveChatHistory;
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
            var getCheckSaveTask = GetCheckSaveWebPageContentAsync(url, stripHtml, saveToFolder, browserType, interactive, pageInstructionsList, useBuiltInFunctions, saveChatHistory, savePageOutput);
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

    private static List<Task<string>> HandleRunCommand(CommandLineOptions commandLineOptions, RunCommand command, SemaphoreSlim throttler, bool delayOutputToApplyInstructions)
    {
        var tasks = new List<Task<string>>();
        var getCheckSaveTask = GetCheckSaveRunCommandContentAsync(command);
        
        var taskToAdd = delayOutputToApplyInstructions
            ? getCheckSaveTask
            : getCheckSaveTask.ContinueWith(t =>
            {
                ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                return t.Result;
            });

        tasks.Add(taskToAdd);
        return tasks;
    }

    private static async Task<string> GetCheckSaveRunCommandContentAsync(RunCommand command)
    {
        try
        {
            ConsoleHelpers.PrintStatus($"Executing: {command.ScriptToRun} ...");
            var finalContent = await GetFinalRunCommandContentAsync(command);

            if (!string.IsNullOrEmpty(command.SaveOutput))
            {
                var saveFileName = FileHelpers.GetFileNameFromTemplate("command-output.md", command.SaveOutput);
                FileHelpers.WriteAllText(saveFileName, finalContent);
                ConsoleHelpers.PrintStatus($"Saving to: {saveFileName} ... Done!");
            }

            return finalContent;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    private static async Task<string> GetFinalRunCommandContentAsync(RunCommand command)
    {
        var formatted = await GetFormattedRunCommandContentAsync(command);

        // var afterInstructions = command.InstructionsList.Any()
        //     ? AiInstructionProcessor.ApplyAllInstructions(command.InstructionsList, formatted, command.UseBuiltInFunctions, command.SaveChatHistory)
        //     : formatted;

        // return afterInstructions;

        return formatted;
    }

    private static async Task<string> GetFormattedRunCommandContentAsync(RunCommand command)
    {
        try
        {
            var script = command.ScriptToRun;
            var shell = command.Type switch
            {
                RunCommand.ScriptType.Cmd => "cmd",
                RunCommand.ScriptType.Bash => "bash",
                RunCommand.ScriptType.PowerShell => "powershell",
                _ => null
            };

            var (output, exitCode) = await ProcessHelpers.RunShellCommandAsync(script, shell, command.EnvironmentVariables);
            var backticks = new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(output));

            var isMultiLine = script.Contains("\n");
            var header = isMultiLine ? "## Run\n\n" : $"## `{script}`\n\n";
            
            var sb = new StringBuilder();
            sb.Append(header);

            if (command.EnvironmentVariables.Any())
            {
                sb.Append("Environment:\n");
                foreach (var envVar in command.EnvironmentVariables)
                {
                    sb.AppendLine($"{envVar.Key}={envVar.Value}");
                }
                sb.AppendLine();
            }

            var scriptPart = isMultiLine ? $"Run:\n{backticks}\n{script.TrimEnd()}\n{backticks}\n\n" : string.Empty;
            var outputPart = $"Output:\n{backticks}\n{output.TrimEnd()}\n{backticks}\n\n";
            var exitCodePart = exitCode != 0 ? $"Exit code: {exitCode}\n\n" : string.Empty;

            sb.Append(scriptPart);
            sb.Append(outputPart);
            sb.Append(exitCodePart);

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"## Error executing command: {command.ScriptToRun}\n\n{ex.Message}\n{ex.StackTrace}";
        }
    }

    private static Task<string> GetCheckSaveFileContentAsync(string fileName, SemaphoreSlim throttler, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions, string saveChatHistory, string saveFileOutput)
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
                saveChatHistory,
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

    private static string GetCheckSaveFileContent(string fileName, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions, string saveChatHistory, string saveFileOutput)
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
                useBuiltInFunctions,
                saveChatHistory);

            if (!string.IsNullOrEmpty(saveFileOutput))
            {
                var saveFileName = FileHelpers.GetFileNameFromTemplate(fileName, saveFileOutput);
                FileHelpers.WriteAllText(saveFileName, finalContent);
                ConsoleHelpers.PrintStatus($"Saving to: {saveFileName} ... Done!");
            }

            return finalContent;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    private static string GetFinalFileContent(string fileName, bool wrapInMarkdown, List<Regex> includeLineContainsPatternList, int includeLineCountBefore, int includeLineCountAfter, bool includeLineNumbers, List<Regex> removeAllLineContainsPatternList, List<Tuple<string, string>> fileInstructionsList, bool useBuiltInFunctions, string saveChatHistory)
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
            ? AiInstructionProcessor.ApplyAllInstructions(instructionsForThisFile, formatted, useBuiltInFunctions, saveChatHistory)
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

    private static async Task<string> GetCheckSaveWebPageContentAsync(string url, bool stripHtml, string saveToFolder, BrowserType browserType, bool interactive, List<Tuple<string, string>> pageInstructionsList, bool useBuiltInFunctions, string saveChatHistory, string savePageOutput)
    {
        try
        {
            ConsoleHelpers.PrintStatus($"Processing: {url} ...");
            var finalContent = await GetFinalWebPageContentAsync(url, stripHtml, saveToFolder, browserType, interactive, pageInstructionsList, useBuiltInFunctions, saveChatHistory);

            if (!string.IsNullOrEmpty(savePageOutput))
            {
                var fileName = FileHelpers.GenerateUniqueFileNameFromUrl(url, saveToFolder ?? "web-pages");
                var saveFileName = FileHelpers.GetFileNameFromTemplate(fileName, savePageOutput);
                FileHelpers.WriteAllText(saveFileName, finalContent);
                ConsoleHelpers.PrintStatus($"Saving to: {saveFileName} ... Done!");
            }

            return finalContent;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    private static async Task<string> GetFinalWebPageContentAsync(string url, bool stripHtml, string saveToFolder, BrowserType browserType, bool interactive, List<Tuple<string, string>> pageInstructionsList, bool useBuiltInFunctions, string saveChatHistory)
    {
        var formatted = await GetFormattedWebPageContentAsync(url, stripHtml, saveToFolder, browserType, interactive);

        var instructionsForThisPage = pageInstructionsList
            .Where(x => WebPageMatchesInstructionsCriteria(url, x.Item2))
            .Select(x => x.Item1)
            .ToList();

        var afterInstructions = instructionsForThisPage.Any()
            ? AiInstructionProcessor.ApplyAllInstructions(instructionsForThisPage, formatted, useBuiltInFunctions, saveChatHistory)
            : formatted;

        return afterInstructions;
    }

    private static bool WebPageMatchesInstructionsCriteria(string url, string webPageCriteria)
    {
        return string.IsNullOrEmpty(webPageCriteria) ||
            url.Contains($".{webPageCriteria}") ||
            url == webPageCriteria;
    }

    private static async Task<string> GetFormattedWebPageContentAsync(string url, bool stripHtml, string saveToFolder, BrowserType browserType, bool interactive)
    {
        try
        {
            var (content, title) = await PlaywrightHelpers.GetPageAndTitle(url, stripHtml, saveToFolder, browserType, interactive);

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

    public const string Name = "mdx";
}
