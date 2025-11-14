namespace vibium.Models;

public class MusicRequest
{
    public string? Mood { get; set; }
    public string? Activity { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
    public bool ExplicitFilter { get; set; }
    public string? Genres { get; set; }
}

public class MusicResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; } // indicates which path produced results
    public List<TrackInfo>? Tracks { get; set; }
}

public class TrackInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Artist { get; set; }
    public string? AlbumName { get; set; }
    public string? AlbumArt { get; set; }
    public string? SpotifyUrl { get; set; }
    public bool IsExplicit { get; set; } // map explicit flag from Spotify
}
