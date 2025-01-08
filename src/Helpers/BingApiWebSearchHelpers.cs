using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

public static class BingApiWebSearchHelpers
{
    private static string endpoint = null;
    private static string apiKey = null;

    public static void ConfigureEndpoint(string apiEndpoint, string api_Key)
    {
        endpoint = apiEndpoint;
        apiKey = api_Key;
    }

    public static async Task<List<string>> GetWebSearchResultUrlsAsync(string query, int maxResults, List<Regex> excludeURLContainsPatternList, bool headless)
    {
        var fallbackToPlaywright = string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey);
        if (fallbackToPlaywright) return await PlaywrightHelpers.GetWebSearchResultUrlsAsync("bing", query, maxResults, excludeURLContainsPatternList, headless);

        using var httpClient = new HttpClient();
        var requestUri = $"{endpoint}?q={Uri.EscapeDataString(query)}&count={maxResults * 5}";
        ConsoleHelpers.PrintDebugLine($"Sending request to Bing API: {requestUri}");

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        var searchResults = doc.RootElement.GetProperty("webPages").GetProperty("value").EnumerateArray();

        var urls = new List<string>();

        foreach (var result in searchResults)
        {
            var url = result.GetProperty("url").GetString();
            if (!excludeURLContainsPatternList.Any(pattern => pattern.IsMatch(url)))
            {
                urls.Add(url);
            }
        }

        if (urls.Count == 0)
        {
            ConsoleHelpers.PrintDebugLine($"No search results found, json response: {jsonResponse}");
        }

        return urls.Take(maxResults).ToList();
    }
}