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

            var prompt = $@"Create a concise Spotify search query (max 5-7 words) based on:
Mood: {mood}
Activity: {activity}
Description: {description}
{(!string.IsNullOrEmpty(language) ? $"Language preference: {language}" : "")}
{(!string.IsNullOrEmpty(genres) ? $"Preferred genres: {genres}" : "")}

Return ONLY the search query, no explanation or extra text.";

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
