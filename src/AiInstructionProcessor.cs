using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

class AiInstructionProcessor
{
    public static string ApplyAllInstructions(List<string> instructionsList, string content, bool useBuiltInFunctions = false)
    {
        try
        {
            ConsoleHelpers.PrintStatus("Applying file instructions ...");
            return instructionsList.Aggregate(content, (current, instruction) => ApplyInstructions(instruction, current, useBuiltInFunctions));
        }
        finally
        {
            ConsoleHelpers.PrintStatusErase();
        }
    }

    public static string ApplyInstructions(string instructions, string content, bool useBuiltInFunctions = false)
    {
        var userPromptFileName = Path.GetTempFileName();
        var systemPromptFileName = Path.GetTempFileName();
        var instructionsFileName = Path.GetTempFileName();
        var contentFileName = Path.GetTempFileName();
        try
        {
            var backticks = new string('`', MarkdownHelpers.GetCodeBlockBacktickCharCountRequired(content) + 3);
            File.WriteAllText(userPromptFileName, GetUserPrompt(backticks, contentFileName, instructionsFileName));
            File.WriteAllText(systemPromptFileName, GetSystemPrompt());
            File.WriteAllText(instructionsFileName, instructions);
            File.WriteAllText(contentFileName, content);

            ConsoleHelpers.PrintDebugLine($"user:\n{File.ReadAllText(userPromptFileName)}\n\n");
            ConsoleHelpers.PrintDebugLine($"system:\n{File.ReadAllText(systemPromptFileName)}\n\n");
            ConsoleHelpers.PrintDebugLine($"instructions:\n{File.ReadAllText(instructionsFileName)}\n\n");

            var arguments = $"chat --user \"@{userPromptFileName}\" --system \"@{systemPromptFileName}\" --quiet true";
            if (useBuiltInFunctions) arguments += " --built-in-functions";

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "ai";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            ConsoleHelpers.PrintDebugLine(process.StartInfo.Arguments);
            ConsoleHelpers.PrintStatus("Applying file instructions ...");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return process.ExitCode != 0
                ? $"{output}\n\nEXIT CODE: {process.ExitCode}\n\nERROR: {error}"
                : output;
        }
        catch (Exception ex)
        {
            return $"## {ex.Message}\n\n";
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
        return "You are a helpful AI assistant.\n\n" +
            "You will be provided with a set of instructions and a markdown file.\n\n" +
            "Your task is to apply the instructions to the text and return the modified text.";
    }

    private static string GetUserPrompt(string backticks, string contentFile, string instructionsFile)
    {
        return 
            "Instructions:\n" + backticks + "\n{@" + instructionsFile + "}\n" + backticks + "\n\n" +
            "Markdown:\n" + backticks + "\n{@" + contentFile + "}\n" + backticks + "\n\n" +
            "Modified markdown (do not enclose in backticks):\n\n";
    }
}
