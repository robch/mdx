using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Collections.Generic;

public static class InteractiveHelpers
{
    public static readonly Dictionary<string, string> KeyboardShortcuts = new()
    {
        { "ESC", "Exit interactive mode" },
        { "n", "Go to next page" },
        { "p", "Go to previous page" },
        { "j", "Scroll down" },
        { "k", "Scroll up" },
        { "y", "Copy text under cursor to clipboard" },
        { "h", "Show this help" },
        { "q", "Close browser" }
    };

    public static void ShowKeyboardShortcuts()
    {
        Console.WriteLine("\nKeyboard shortcuts in interactive mode:");
        Console.WriteLine("------------------------------------");
        foreach (var (key, description) in KeyboardShortcuts)
        {
            Console.WriteLine($"{key,-4} : {description}");
        }
        Console.WriteLine("\nMouse controls:");
        Console.WriteLine("- Click and drag to select text");
        Console.WriteLine("- Double click to select word");
        Console.WriteLine("- Triple click to select line");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }

    public static async Task HandleKeyboardAndMouseEvents(IPage page)
    {
        // Monitor keyboard events
        await page.Keyboard.DownAsync("Control");
        await page.Keyboard.PressAsync("Home"); // Scroll to top
        await page.Keyboard.UpAsync("Control");

        // Example keyboard shortcuts
        ConsoleKeyInfo key;
        bool exitRequested = false;
        
        while (!exitRequested)
        {
            if (Console.KeyAvailable)
            {
                key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case 'q':
                        exitRequested = true;
                        break;
                    case 'n':
                        await page.EvaluateAsync("window.scrollBy(0, window.innerHeight)");
                        break;
                    case 'p':
                        await page.EvaluateAsync("window.scrollBy(0, -window.innerHeight)");
                        break;
                    case 'j':
                        await page.EvaluateAsync("window.scrollBy(0, 100)");
                        break;
                    case 'k':
                        await page.EvaluateAsync("window.scrollBy(0, -100)");
                        break;
                    case 'y':
                        var selectedText = await page.EvaluateAsync<string>(@"() => {
                            const selection = window.getSelection();
                            return selection ? selection.toString() : '';
                        }");
                        if (!string.IsNullOrEmpty(selectedText))
                        {
                            Console.WriteLine($"\nCopied: {selectedText}");
                        }
                        break;
                    case 'h':
                        ShowKeyboardShortcuts();
                        break;
                    case (char)27: // ESC key
                        exitRequested = true;
                        break;
                }
            }

            // Short delay to prevent high CPU usage
            await Task.Delay(50);
        }
    }

    public static async Task<string> GetTextUnderCursor(IPage page, string selector)
    {
        var element = await page.QuerySelectorAsync(selector);
        if (element != null)
        {
            return await element.TextContentAsync() ?? string.Empty;
        }
        return string.Empty;
    }
}