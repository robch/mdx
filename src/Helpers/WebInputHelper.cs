using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace mdx.Helpers
{
    public class WebInputHelper
    {
        private IPage _page;

        public WebInputHelper(IPage page)
        {
            _page = page;
        }

        public async Task Click(string selector)
        {
            await _page.ClickAsync(selector);
        }

        public async Task MouseMove(int x, int y)
        {
            await _page.Mouse.MoveAsync(x, y);
        }

        public async Task MouseDown()
        {
            await _page.Mouse.DownAsync();
        }

        public async Task MouseUp()
        {
            await _page.Mouse.UpAsync();
        }

        public async Task DragAndDrop(string sourceSelector, string targetSelector)
        {
            await _page.DragAndDropAsync(sourceSelector, targetSelector);
        }

        public async Task Scroll(int x, int y)
        {
            await _page.Mouse.WheelAsync(x, y);
        }

        public async Task PressKey(string key)
        {
            await _page.Keyboard.PressAsync(key);
        }

        public async Task TypeText(string text)
        {
            await _page.Keyboard.TypeAsync(text);
        }

        public async Task SendShortcut(string shortcut)
        {
            await _page.Keyboard.PressAsync(shortcut);
        }
    }
}