#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace vibium.Models;

public class SpotifyTokenResponse
{
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    public int expires_in { get; set; }
}

public class SpotifySearchResponse
{
    public TracksResponse? tracks { get; set; }
}

public class TracksResponse
{
    public List<SpotifyTrack>? items { get; set; }
}

public class SpotifyTrack
{
    public string? id { get; set; }
    public string? name { get; set; }
    public List<SpotifyArtist>? artists { get; set; }
    public SpotifyAlbum? album { get; set; }
    public ExternalUrls? external_urls { get; set; }
    [JsonPropertyName("explicit")]
    public bool Explicit { get; set; }
}

public class ExternalUrls
{
    public string? spotify { get; set; }
}

public class SpotifyArtist
{
    public string? name { get; set; }
}

public class SpotifyAlbum
{
    public string? name { get; set; }
    public List<SpotifyImage>? images { get; set; }
}

public class SpotifyImage
{
    public string? url { get; set; }
    public int height { get; set; }
    public int width { get; set; }
}

public class SpotifyRecommendationsResponse
{
    public List<SpotifyTrack>? tracks { get; set; }
}
