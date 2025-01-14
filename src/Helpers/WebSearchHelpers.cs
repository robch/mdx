using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class WebSearchHelpers
{
    public static async Task<List<string>> GetWebSearchResultUrlsAsync(WebSearchProvider webSearchProvider, string query, int maxResults, List<Regex> excludeURLContainsPatternList, BrowserType browserType, bool headless)
    {
        var providerString = webSearchProvider.ToString().ToLower();
        return webSearchProvider switch
        {
            WebSearchProvider.BingAPI => await BingApiWebSearchHelpers.GetWebSearchResultUrlsAsync(query, maxResults, excludeURLContainsPatternList, browserType, headless),
            WebSearchProvider.GoogleAPI => await GoogleApiWebSearchHelpers.GetWebSearchResultUrlsAsync(query, maxResults, excludeURLContainsPatternList, browserType, headless),
            _ => await PlaywrightHelpers.GetWebSearchResultUrlsAsync(providerString, query, maxResults, excludeURLContainsPatternList, browserType, headless)
        };
    }
}