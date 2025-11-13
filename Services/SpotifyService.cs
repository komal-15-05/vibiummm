using System.Text;
using System.Text.Json;
using vibium.Models;

namespace vibium.Services;

public class SpotifyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiration = DateTime.MinValue;

    public SpotifyService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
        {
            return _accessToken;
        }

        var clientId = _configuration["Spotify:ClientId"];
        var clientSecret = _configuration["Spotify:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new Exception("Spotify credentials not configured");
        }

        var client = _httpClientFactory.CreateClient();
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get Spotify access token");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);

        if (tokenResponse?.access_token == null)
        {
            throw new Exception("Invalid token response");
        }

        _accessToken = tokenResponse.access_token;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in - 60);

        return _accessToken;
    }

    public async Task<List<TrackInfo>> SearchTracksAsync(string query, bool explicitFilter = false, int limit = 10)
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit={limit}";

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Spotify search failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var searchResponse = JsonSerializer.Deserialize<SpotifySearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var tracks = searchResponse?.tracks?.items?.Select(track => new TrackInfo
        {
            Id = track.id,
            Name = track.name,
            Artist = string.Join(", ", track.artists?.Select(a => a.name) ?? new List<string>()),
            AlbumName = track.album?.name,
            AlbumArt = track.album?.images?.FirstOrDefault()?.url,
            SpotifyUrl = track.external_urls?.spotify
        }).ToList() ?? new List<TrackInfo>();

        if (explicitFilter)
        {
            // Note: The Spotify API doesn't always provide explicit content flag in search results
            // For a production app, you might want to use the track details endpoint to check this
        }

        return tracks;
    }

    public async Task<List<TrackInfo>> GetRecommendationsAsync(string seedGenres, int limit = 10)
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var encodedGenres = Uri.EscapeDataString(seedGenres);
        var url = $"https://api.spotify.com/v1/recommendations?seed_genres={encodedGenres}&limit={limit}";

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Spotify recommendations failed: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var recommendationsResponse = JsonSerializer.Deserialize<SpotifyRecommendationsResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return recommendationsResponse?.tracks?.Select(track => new TrackInfo
        {
            Id = track.id,
            Name = track.name,
            Artist = string.Join(", ", track.artists?.Select(a => a.name) ?? new List<string>()),
            AlbumName = track.album?.name,
            AlbumArt = track.album?.images?.FirstOrDefault()?.url,
            SpotifyUrl = track.external_urls?.spotify
        }).ToList() ?? new List<TrackInfo>();
    }
}
