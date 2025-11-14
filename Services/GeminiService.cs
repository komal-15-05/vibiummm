using System.Text;
using System.Text.Json;
using vibium.Models;

namespace vibium.Services;

public class GeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> RefineSearchQueryAsync(string mood, string activity, string description, string? language = null, string? genres = null)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            // Fallback to basic query if Gemini is not configured
            return BuildBasicQuery(mood, activity, description, genres);
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

            var prompt = $"Create a concise Spotify search query (max5-7 words) based on:\n" +
                         $"Mood: {mood}\n" +
                         $"Activity: {activity}\n" +
                         $"Description: {description}\n" +
                         (!string.IsNullOrEmpty(language) ? $"Language preference: {language}\n" : "") +
                         (!string.IsNullOrEmpty(genres) ? $"Preferred genres: {genres}\n" : "") +
                         "\nReturn ONLY the search query, no explanation or extra text.";

            var request = new GeminiRequest
            {
                contents = new List<Content>
                {
                    new Content
                    {
                        parts = new List<Part>
                        {
                            new Part { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                return BuildBasicQuery(mood, activity, description, genres);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var refinedQuery = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text?.Trim();

            return string.IsNullOrEmpty(refinedQuery) ? BuildBasicQuery(mood, activity, description, genres) : refinedQuery;
        }
        catch
        {
            return BuildBasicQuery(mood, activity, description, genres);
        }
    }

    // New: ask Gemini for multiple alternative queries to broaden/rephrase user input
    public async Task<List<string>> ExpandSearchQueriesAsync(string mood, string activity, string description, string? language = null, string? genres = null, int maxAlternatives = 3)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return new List<string>();
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

            var prompt = $"Provide up to {maxAlternatives} concise Spotify search queries (3-7 words each) that broaden or rephrase this search to increase chances of finding relevant tracks.\n" +
                         $"Respond in the same language as the user's preference if provided ({language ?? "any"}).\n\n" +
                         $"Mood: {mood}\n" +
                         $"Activity: {activity}\n" +
                         $"Description: {description}\n" +
                         (!string.IsNullOrEmpty(genres) ? $"Preferred genres: {genres}\n" : "") +
                         "\nReturn ONLY the queries, each on its own line. No extra text.";

            var request = new GeminiRequest
            {
                contents = new List<Content>
                {
                    new Content
                    {
                        parts = new List<Part>
                        {
                            new Part { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return new List<string>();

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var responseText = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;
            if (string.IsNullOrEmpty(responseText)) return new List<string>();

            // Split into lines and take up to maxAlternatives
            var lines = responseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct()
                .Take(maxAlternatives)
                .ToList();

            return lines;
        }
        catch
        {
            return new List<string>();
        }
    }

    // New: ask Gemini to suggest specific track titles and artists as JSON
    public async Task<List<(string Title, string Artist)>> SuggestTrackCandidatesAsync(string mood, string activity, string description, string? language = null, string? genres = null, int maxTracks = 5)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var result = new List<(string Title, string Artist)>();

        if (string.IsNullOrEmpty(apiKey)) return result;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

            var prompt = "Based on the user's inputs, suggest up to " + maxTracks + " specific song titles and their artists that match the mood, activity, description, language, and genres.\n" +
                         "Respond in " + (string.IsNullOrEmpty(language) ? "any language" : language) + ".\n\n" +
                         "Return STRICTLY a JSON array of objects with the structure: [{\"title\":\"...\",\"artist\":\"...\"}, ...].\n" +
                         "Do not return any extra text or commentary. If you cannot produce suggestions, return an empty JSON array [].\n\n" +
                         "Mood: " + mood + "\n" +
                         "Activity: " + activity + "\n" +
                         "Description: " + description + "\n" +
                         (!string.IsNullOrEmpty(genres) ? "Preferred genres: " + genres + "\n" : "");

            var request = new GeminiRequest
            {
                contents = new List<Content>
                {
                    new Content
                    {
                        parts = new List<Part>
                        {
                            new Part { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return result;

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var responseText2 = geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text;
            if (string.IsNullOrWhiteSpace(responseText2)) return result;

            // Try to extract JSON array from the response text
            var start = responseText2.IndexOf('[');
            var end = responseText2.LastIndexOf(']');
            if (start >=0 && end > start)
            {
                var jsonArray = responseText2.Substring(start, end - start +1);
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var parsed = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(jsonArray, options);
                    if (parsed != null)
                    {
                        foreach (var item in parsed.Take(maxTracks))
                        {
                            item.TryGetValue("title", out var title);
                            item.TryGetValue("artist", out var artist);
                            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(artist))
                            {
                                result.Add((title.Trim(), artist.Trim()));
                            }
                        }
                    }
                }
                catch
                {
                    // ignore parse errors
                }
            }

            return result;
        }
        catch
        {
            return result;
        }
    }

    private string BuildBasicQuery(string mood, string activity, string description, string? genres)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(mood)) queryParts.Add(mood);
        if (!string.IsNullOrEmpty(activity)) queryParts.Add(activity);
        if (!string.IsNullOrEmpty(genres)) queryParts.Add(genres);
        if (!string.IsNullOrEmpty(description)) queryParts.Add(description);

        return string.Join(" ", queryParts);
    }
}
