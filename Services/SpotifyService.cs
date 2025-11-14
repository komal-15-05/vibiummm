using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
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
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var respBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get Spotify access token: {response.StatusCode} - {respBody}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<SpotifyTokenResponse>(json);

        if (tokenResponse?.access_token == null)
        {
            throw new Exception("Invalid token response");
        }

        _accessToken = tokenResponse.access_token;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in -60);

        return _accessToken;
    }

    public async Task<List<TrackInfo>> SearchTracksAsync(string query, bool explicitFilter = false, int limit =10)
    {
        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://api.spotify.com/v1/search?q={encodedQuery}&type=track&limit={limit}";

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Spotify search failed: {response.StatusCode} - {body}");
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
            SpotifyUrl = track.external_urls?.spotify,
            IsExplicit = track.Explicit
        }).ToList() ?? new List<TrackInfo>();

        if (explicitFilter)
        {
            tracks = tracks.Where(t => !t.IsExplicit).ToList();
        }

        return tracks;
    }

    public async Task<List<TrackInfo>> GetRecommendationsAsync(string seedGenres, int limit =10)
    {
        // Validate inputs early: Spotify requires at least one seed parameter.
        if (string.IsNullOrWhiteSpace(seedGenres))
        {
            // Return empty list instead of making an invalid request that yields 404.
            return new List<TrackInfo>();
        }

        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var encodedGenres = Uri.EscapeDataString(seedGenres);
        var url = $"https://api.spotify.com/v1/recommendations?seed_genres={encodedGenres}&limit={limit}";

        var response = await client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            // Read response body to aid debugging, but do not throw for 404/400.
            var body = await response.Content.ReadAsStringAsync();

            // For common client errors return empty list so caller can continue with fallback logic.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new List<TrackInfo>();
            }

            throw new Exception($"Spotify recommendations failed: {response.StatusCode} - {body}");
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
            SpotifyUrl = track.external_urls?.spotify,
            IsExplicit = track.Explicit
        }).ToList() ?? new List<TrackInfo>();
    }

    // New: search artist by name, return artist id or null
    public async Task<string?> SearchArtistIdAsync(string artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName)) return null;

        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var encoded = Uri.EscapeDataString(artistName);
        var url = $"https://api.spotify.com/v1/search?q={encoded}&type=artist&limit=1";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("artists", out var artists) && artists.TryGetProperty("items", out var items) && items.GetArrayLength() >0)
        {
            var id = items[0].GetProperty("id").GetString();
            return id;
        }

        return null;
    }

    // New: get artist top tracks by artist id
    public async Task<List<TrackInfo>> GetArtistTopTracksAsync(string artistId, int limit =10, string market = "US")
    {
        if (string.IsNullOrWhiteSpace(artistId)) return new List<TrackInfo>();

        var token = await GetAccessTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = $"https://api.spotify.com/v1/artists/{artistId}/top-tracks?market={market}";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<TrackInfo>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("tracks", out var tracksElem)) return new List<TrackInfo>();

        var results = new List<TrackInfo>();
        foreach (var t in tracksElem.EnumerateArray())
        {
            if (results.Count >= limit) break;
            var id = t.GetProperty("id").GetString();
            var name = t.GetProperty("name").GetString();
            var artists = t.GetProperty("artists").EnumerateArray().Select(a => a.GetProperty("name").GetString()).Where(s => s != null).Cast<string>().ToList();
            var albumName = t.GetProperty("album").GetProperty("name").GetString();
            string? albumArt = null;
            if (t.GetProperty("album").TryGetProperty("images", out var images) && images.GetArrayLength() >0)
            {
                albumArt = images[0].GetProperty("url").GetString();
            }
            string? spotifyUrl = null;
            if (t.TryGetProperty("external_urls", out var ex) && ex.TryGetProperty("spotify", out var spUrl))
            {
                spotifyUrl = spUrl.GetString();
            }

            results.Add(new TrackInfo
            {
                Id = id,
                Name = name,
                Artist = string.Join(", ", artists),
                AlbumName = albumName,
                AlbumArt = albumArt,
                SpotifyUrl = spotifyUrl
            });
        }

        return results;
    }
}
