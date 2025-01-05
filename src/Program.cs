using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
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

    private static void PrintBanner()
    {
        ConsoleHelpers.PrintLine(
            "MDCC - Markdown Context Creator CLI, Version 1.0.0\n" +
            "Copyright(c) 2024, Rob Chambers. All rights reserved.\n");
    }

    private static void PrintException(CommandLineException ex)
    {
        var printMessage = !string.IsNullOrEmpty(ex.Message) && !(ex is CommandLineHelpRequestedException);
        if (printMessage) ConsoleHelpers.PrintLine($"  {ex.Message}\n\n");
    }

    private static void PrintUsage(string command = "")
    {
        var processorCount = Environment.ProcessorCount;

        if (command == "web search")
        {
            ConsoleHelpers.PrintLine(
                "USAGE: mdcc web search \"TERMS\" [...]\n\n" +
                "OPTIONS:\n\n" +
                "  --headless       Run in headless mode (default: false)\n" +
                "  --strip          Strip HTML tags from downloaded content (default: false)\n" +
                "  --save [FOLDER]  Save downloaded content to disk\n" +
                "\nSEARCH OPTIONS:\n\n" +
                "  --bing           Use Bing search engine\n" +
                "  --google         Use Google search engine (default)\n" +
                "  --get            Download content from search results (default: false)\n" +
                "  --max NUMBER     Maximum number of search results (default: 10)"
                );
        }
        else if (command == "web get")
        {
            ConsoleHelpers.PrintLine(
                "USAGE: mdcc web get \"URL\" [...]\n\n" +
                "OPTIONS:\n\n" +
                "  --headless       Run in headless mode (default: false)\n" +
                "  --strip          Strip HTML tags from downloaded content (default: false)\n" +
                "  --save [FOLDER]  Save downloaded content to disk"
                );
        }
        else
        {
            ConsoleHelpers.PrintLine(
                "USAGE: mdcc [file1 [file2 [pattern1 [pattern2 [...]]]]] [...]\n\n" +
                "OPTIONS:\n\n" +
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
        return string.Join('\n', lines.Select((line, index) => $"{index + 1}: {line}"));
    }

    private static string GetContentFilteredAndFormatted(
        string content,
        List<Regex> includeLineContainsPatternList,
        int includeLineCountBefore,
        int includeLineCountAfter,
        bool includeLineNumbers,
        List<Regex> removeAllLineContainsPatternList,
        string backticks)
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

    private static async Task<List<Task<string>>> HandleWebSearchCommandAsync(
        CommandLineOptions commandLineOptions,
        WebSearchCommand command,
        SemaphoreSlim throttler,
        bool delayOutputToApplyInstructions)
    {
        var searchEngine = command.UseBing ? "bing" : "google";
        var query = string.Join(" ", command.Terms);
        var maxResults = command.MaxResults;
        var getContent = command.GetContent;
        var stripHtml = command.StripHtml;
        var saveToFolder = command.SaveFolder;
        var headless = command.Headless;

        var searchSectionHeader = $"## Web Search for '{query}' using {searchEngine}";

        var urls = await GetWebSearchResultUrlsAsync(searchEngine, query, maxResults, headless);
        if (urls.Count == 0)
        {
            var message = $"{searchSectionHeader}\n\nNo results found\n";
            return new List<Task<string>>() { Task.FromResult(message) };
        }

        var searchSection = $"{searchSectionHeader}\n\n" + string.Join("\n", urls) + "\n\n";
        if (!getContent)
        {
            return new List<Task<string>>() { Task.FromResult(searchSection) };
        }

        var tasks = new List<Task<string>>();
        tasks.Add(Task.FromResult(searchSection));

        foreach (var url in urls)
        {
            var getFormattedPageContentTask = GetFormattedPageContentFromUrlAsync(url, stripHtml, saveToFolder, headless);
            tasks.Add(delayOutputToApplyInstructions
                ? getFormattedPageContentTask
                : getFormattedPageContentTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                }));
        }

        return tasks;
    }

    private static async Task<List<string>> GetWebSearchResultUrlsAsync(string searchEngine, string query, int maxResults, bool headless)
    {
        // Initialize Playwright
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Determine the search URL based on the chosen search engine
        var searchUrl = searchEngine == "bing"
            ? $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}"
            : $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";

        // Navigate to the search engine
        await page.GotoAsync(searchUrl);

        // Extract search result URLs
        var urls = (searchEngine == "google")
            ? await ExtractGoogleSearchResults(page, maxResults)
            : await ExtractBingSearchResults(page, maxResults);

        return urls;
    }

    private static async Task<string> GetFormattedPageContentFromUrlAsync(string url, bool stripHtml, string saveToFolder, bool headless)
    {
        try
        {
            // Initialize Playwright
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            // Navigate to the URL
            await page.GotoAsync(url);

            // Fetch the page content and title
            var content = await FetchPageContent(page, url, stripHtml, saveToFolder);
            var title = await page.TitleAsync();

            // Wrap the content in markdown
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

    private static List<Task<string>> HandleWebGetCommand(
        CommandLineOptions commandLineOptions,
        WebGetCommand command,
        SemaphoreSlim throttler,
        bool delayOutputToApplyInstructions)
    {
        var urls = command.Urls;
        var stripHtml = command.StripHtml;
        var saveToFolder = command.SaveFolder;
        var headless = command.Headless;

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
            var getFormattedPageContentTask = GetFormattedPageContentFromUrlAsync(url, stripHtml, saveToFolder, headless);
            tasks.Add(delayOutputToApplyInstructions
                ? getFormattedPageContentTask
                : getFormattedPageContentTask.ContinueWith(t =>
                {
                    ConsoleHelpers.PrintLineIfNotEmpty(t.Result);
                    return t.Result;
                }));
        }

        return tasks;
    }

    private static async Task<List<string>> ExtractGoogleSearchResults(IPage page, int maxResults)
    {
        var urls = new List<string>();
        while (urls.Count < maxResults)
        {
            var elements = await page.QuerySelectorAllAsync("div#search a[href]");
            foreach (var element in elements)
            {
                var href = await element.GetAttributeAsync("href");
                if (href != null && href.StartsWith("http") && !href.Contains("google"))
                {
                    if (!urls.Contains(href))
                    {
                        urls.Add(href);
                    }
                }
                if (urls.Count >= maxResults) break;
            }

            if (urls.Count >= maxResults) break;

            var nextButton = await page.QuerySelectorAsync("a#pnnext");
            if (nextButton != null)
            {
                await nextButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            else
            {
                break;
            }
        }
        return urls.Take(maxResults).ToList();
    }

    private static async Task<List<string>> ExtractBingSearchResults(IPage page, int maxResults)
    {
        var urls = new List<string>();
        while (urls.Count < maxResults)
        {
            var elements = await page.QuerySelectorAllAsync("li.b_algo a[href]");
            foreach (var element in elements)
            {
                var href = await element.GetAttributeAsync("href");
                if (href != null && href.StartsWith("http"))
                {
                    if (!urls.Contains(href))
                    {
                        urls.Add(href);
                    }
                }
                if (urls.Count >= maxResults) break;
            }

            if (urls.Count >= maxResults) break;

            var nextButton = await page.QuerySelectorAsync("a.sb_pagN");
            if (nextButton != null)
            {
                await nextButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            else
            {
                break;
            }
        }
        return urls.Take(maxResults).ToList();
    }

    private static async Task<string> FetchPageContent(IPage page, string url, bool stripHtml, string saveToFolder)
    {
        try
        {
            // Navigate to the URL
            await page.GotoAsync(url);

            // Get the main content text
            var content = await FetchPageContentWithRetries(page);

            if (content.Contains("Rate limit is exceeded. Try again in"))
            {
                // Rate limit exceeded, wait and try again
                var seconds = int.Parse(content.Split("Try again in ")[1].Split(" seconds.")[0]);
                await Task.Delay(seconds * 1000);
                return await FetchPageContent(page, url, stripHtml, saveToFolder);
            }

            if (stripHtml)
            {
                content = StripHtmlContent(content);
            }

            if (!string.IsNullOrEmpty(saveToFolder))
            {
                var fileName = GenerateUniqueFileNameFromUrl(url, saveToFolder);
                File.WriteAllText(fileName, content);
            }

            return content;
        }
        catch (Exception ex)
        {
            return $"Error fetching content from {url}: {ex.Message}\n{ex.StackTrace}";
        }
    }

    private static async Task<string> FetchPageContentWithRetries(IPage page, int retries = 3)
    {
        var tryCount = retries + 1;
        while (true)
        {
            try
            {
                return await page.ContentAsync();
            }
            catch (Exception ex)
            {
                var rethrow = --tryCount == 0 || !ex.Message.Contains("navigating");
                if (rethrow) throw;
                await Task.Delay(1000);
            }
        }
    }

    private static string StripHtmlContent(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var innerText = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        var innerTextLines = innerText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var lines = new List<string>();
        var blanks = new List<string>();
        foreach (var line in innerTextLines)
        {
            var trimmed = line.Trim(new[] { ' ', '\t', '\r', '\n', '\v', '\f', '\u00A0', '\u200B' });
            if (string.IsNullOrEmpty(trimmed))
            {
                blanks.Add(line);
                continue;
            }

            // Only insert one blank line at a time
            var addBlanks = Math.Min(blanks.Count, 1);
            while (addBlanks-- > 0) lines.Add(string.Empty);
            blanks.Clear();

            lines.Add(line);
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string GenerateUniqueFileNameFromUrl(string url, string saveToFolder)
    {
        EnsureDirectoryExists(saveToFolder);

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

    private static void EnsureDirectoryExists(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
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
