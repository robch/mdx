using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class AiInstructionProcessor
{
    public const string DefaultSaveChatHistoryTemplate = "chat-history-{time}.jsonl";

    public static string ApplyAllInstructions(List<string> instructionsList, string content, bool useBuiltInFunctions, string saveChatHistory, int retries = 1)
    {
        try
        {
            ConsoleHelpers.PrintStatus("Applying instructions ...");
            return instructionsList.Aggregate(content, (current, instruction) => ApplyInstructions(instruction, current, useBuiltInFunctions, saveChatHistory, retries));
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    public static string ApplyInstructions(string instructions, string content, bool useBuiltInFunctions, string saveChatHistory, int retries = 1)
    {
        while (true)
        {
            ApplyInstructions(instructions, content, useBuiltInFunctions, saveChatHistory, out var returnCode, out var stdOut, out var stdErr, out var exception);

            var retryable = retries-- > 0;
            var tryAgain = retryable && (returnCode != 0 || exception != null);
            if (tryAgain) continue;

            return exception != null
                ? $"{stdOut}\n\n## Error Applying Instructions\n\nEXIT CODE: {returnCode}\n\nERROR: {exception.Message}\n\nSTDERR: {stdErr}"
                : returnCode != 0
                    ? $"{stdOut}\n\n## Error Applying Instructions\n\nEXIT CODE: {returnCode}\n\nSTDERR: {stdErr}"
                    : stdOut;
        }
    }

    private static void ApplyInstructions(string instructions, string content, bool useBuiltInFunctions, string saveChatHistory, out int returnCode, out string stdOut, out string stdErr, out Exception exception)
    {
        returnCode = 0;
        stdOut = null;
        stdErr = null;
        exception = null;

        var userPromptFileName = Path.GetTempFileName();
        var systemPromptFileName = Path.GetTempFileName();
        var instructionsFileName = Path.GetTempFileName();
        var contentFileName = Path.GetTempFileName();
        try
        {
            var backticks = new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(content) + 3);
            File.WriteAllText(systemPromptFileName, GetSystemPrompt());
            File.WriteAllText(userPromptFileName, GetUserPrompt(backticks, contentFileName, instructionsFileName));
            File.WriteAllText(instructionsFileName, instructions);
            File.WriteAllText(contentFileName, content);

            ConsoleHelpers.PrintDebugLine($"user:\n{File.ReadAllText(userPromptFileName)}\n\n");
            ConsoleHelpers.PrintDebugLine($"system:\n{File.ReadAllText(systemPromptFileName)}\n\n");
            ConsoleHelpers.PrintDebugLine($"instructions:\n{File.ReadAllText(instructionsFileName)}\n\n");

            var arguments = $"chat --user \"@{userPromptFileName}\" --system \"@{systemPromptFileName}\" --quiet true";
            if (useBuiltInFunctions) arguments += " --built-in-functions";

            if (!string.IsNullOrEmpty(saveChatHistory))
            {
                var fileName = FileHelpers.GetFileNameFromTemplate(DefaultSaveChatHistoryTemplate, saveChatHistory);
                arguments += $" --output-chat-history \"{fileName}\"";
            }

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ai";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            ConsoleHelpers.PrintDebugLine(process.StartInfo.Arguments);
            ConsoleHelpers.PrintStatus("Applying instructions ...");

            stdOut = process.StandardOutput.ReadToEnd();
            stdErr = process.StandardError.ReadToEnd();

            process.WaitForExit();
            returnCode = process.ExitCode;
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
            if (File.Exists(userPromptFileName)) File.Delete(userPromptFileName);
            if (File.Exists(systemPromptFileName)) File.Delete(systemPromptFileName);
            if (File.Exists(instructionsFileName)) File.Delete(instructionsFileName);
            if (File.Exists(contentFileName)) File.Delete(contentFileName);
        }
    }

    private static string GetSystemPrompt()
    {
        return FileHelpers.ReadEmbeddedStream("prompts.system.md");
    }

    private static string GetUserPrompt(string backticks, string contentFile, string instructionsFile)
    {
        return FileHelpers.ReadEmbeddedStream("prompts.user.md")
            .Replace("{instructionsFile}", instructionsFile)
            .Replace("{contentFile}", contentFile)
            .Replace("{backticks}", backticks);
    }
}
