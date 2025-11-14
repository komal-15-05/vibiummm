using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using vibium.Models;
using vibium.Services;

namespace vibium.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MusicController : ControllerBase
{
    private readonly SpotifyService _spotifyService;
    private readonly GeminiService _geminiService;
    private readonly ILogger<MusicController> _logger;
    private readonly IConfiguration _configuration;

    public MusicController(
        SpotifyService spotifyService,
        GeminiService geminiService,
        ILogger<MusicController> logger,
        IConfiguration configuration)
    {
        _spotifyService = spotifyService;
        _geminiService = geminiService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet("config-check")]
    public ActionResult<object> CheckConfiguration()
    {
        var spotifyClientId = _configuration["Spotify:ClientId"];
        var spotifyClientSecret = _configuration["Spotify:ClientSecret"];
        var geminiApiKey = _configuration["Gemini:ApiKey"];

        var isSpotifyConfigured = !string.IsNullOrEmpty(spotifyClientId) && 
                                  !string.IsNullOrEmpty(spotifyClientSecret) &&
                                  spotifyClientId != "YOUR_SPOTIFY_CLIENT_ID" &&
                                  spotifyClientSecret != "YOUR_SPOTIFY_CLIENT_SECRET";

        var isGeminiConfigured = !string.IsNullOrEmpty(geminiApiKey) && 
                                 geminiApiKey != "YOUR_GEMINI_API_KEY";

        return Ok(new
        {
            spotifyConfigured = isSpotifyConfigured,
            geminiConfigured = isGeminiConfigured,
            message = isSpotifyConfigured 
                ? "Configuration looks good!" 
                : "Spotify credentials need to be configured. Check SETUP.md for instructions."
        });
    }

    [HttpPost("suggest")]
    public async Task<ActionResult<MusicResponse>> SuggestMusic([FromBody] MusicRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Mood) && string.IsNullOrEmpty(request.Activity) && string.IsNullOrEmpty(request.Description))
            {
                return BadRequest(new MusicResponse
                {
                    Success = false,
                    Message = "Please provide at least mood, activity, or description."
                });
            }
            
            _logger.LogInformation("Music request - Mood: {Mood}, Activity: {Activity}, Genres: {Genres}", 
                request.Mood, request.Activity, request.Genres);

            // Detect artist mention but still send all inputs to Gemini
            string? artistName = null;
            if (!string.IsNullOrEmpty(request.Description))
            {
                artistName = ExtractArtistFromDescription(request.Description);
                if (!string.IsNullOrEmpty(artistName))
                {
                    _logger.LogInformation("Artist explicitly mentioned in description: {Artist}", artistName);
                }
            }

            // Always call Gemini (or fallback) with all inputs to refine the query
            var refinedQuery = await _geminiService.RefineSearchQueryAsync(
                request.Mood ?? string.Empty,
                request.Activity ?? string.Empty,
                request.Description ?? string.Empty,
                request.Language,
                request.Genres
            );

            // If Gemini returns an empty query, build a basic query from inputs
            if (string.IsNullOrWhiteSpace(refinedQuery))
            {
                var fallbackParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(request.Mood)) fallbackParts.Add(request.Mood);
                if (!string.IsNullOrWhiteSpace(request.Activity)) fallbackParts.Add(request.Activity);
                if (!string.IsNullOrWhiteSpace(request.Genres)) fallbackParts.Add(request.Genres);
                if (!string.IsNullOrWhiteSpace(request.Description)) fallbackParts.Add(request.Description);

                refinedQuery = string.Join(" ", fallbackParts).Trim();
                _logger.LogInformation("Gemini returned empty; using fallback query: {Query}", refinedQuery);
            }

            // FIRST: Ask Gemini to suggest specific tracks (title + artist) directly
            var geminiTrackCandidates = await _geminiService.SuggestTrackCandidatesAsync(
                request.Mood ?? string.Empty,
                request.Activity ?? string.Empty,
                request.Description ?? string.Empty,
                request.Language,
                request.Genres,
                maxTracks:8
            );

            var tracks = new List<TrackInfo>();
            if (geminiTrackCandidates != null && geminiTrackCandidates.Any())
            {
                _logger.LogInformation("Gemini returned {Count} track candidates; resolving on Spotify.", geminiTrackCandidates.Count);
                foreach (var (Title, Artist) in geminiTrackCandidates)
                {
                    try
                    {
                        var titleSan = SanitizeQuery(Title);
                        var artistSan = SanitizeQuery(Artist);
                        if (string.IsNullOrWhiteSpace(titleSan) || string.IsNullOrWhiteSpace(artistSan)) continue;

                        var candidateQuery = $"track:{titleSan} artist:{artistSan}";
                        var found = await _spotifyService.SearchTracksAsync(candidateQuery, request.ExplicitFilter,1);
                        if (found != null && found.Any())
                        {
                            tracks.AddRange(found);
                        }
                        // stop early if we have enough
                        if (tracks.Count >=5) break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error resolving GeminI candidate {Title} - {Artist}", Title, Artist);
                    }
                }

                if (tracks.Any())
                {
                    // dedupe and return top5
                    tracks = tracks.GroupBy(t => string.IsNullOrEmpty(t.Id) ? ((t.Name ?? string.Empty) + "|" + (t.Artist ?? string.Empty)) : t.Id).Select(g => g.First()).Take(5).ToList();
                    var wittyQuotesLocal = new[]
                    {
                        "Good music is the shorthand of emotion.",
                        "Putting the right track on is like hitting the mood's sweet spot.",
                        "Here's something to make your playlist proud.",
                        "Soundtrack incoming. Emotions not included (but likely).",
                        "Trust the vibes. These know the way."
                    };
                    var randLocal = new Random();
                    var quoteLocal = wittyQuotesLocal[randLocal.Next(wittyQuotesLocal.Length)];

                    return Ok(new MusicResponse
                    {
                        Success = true,
                        Message = $"Found {tracks.Count} tracks for you! — {quoteLocal}",
                        Tracks = tracks
                    });
                }
            }

            // Prepare the searchQuery from refinedQuery and optionally append artist filter
            var searchQuery = SanitizeQuery(refinedQuery);

            // If an artist was detected, attempt to fetch top tracks by artist id for accuracy
            if (!string.IsNullOrEmpty(artistName))
            {
                var artistId = await _spotifyService.SearchArtistIdAsync(artistName);
                if (!string.IsNullOrEmpty(artistId))
                {
                    _logger.LogInformation("Found artist id {ArtistId} for {Artist}", artistId, artistName);
                    var topTracks = await _spotifyService.GetArtistTopTracksAsync(artistId, limit:10);
                    if (topTracks != null && topTracks.Any())
                    {
                        tracks = topTracks.ToList();
                    }
                }
                // Also append artist filter to the searchQuery so any subsequent search is artist-specific
                var artistFilter = SanitizeQuery(artistName);
                if (!string.IsNullOrWhiteSpace(artistFilter)) searchQuery = $"{searchQuery} artist:{artistFilter}";
            }

            // If tracks still empty, run the standard refined search
            if (tracks == null || !tracks.Any())
            {
                tracks = await _spotifyService.SearchTracksAsync(searchQuery, request.ExplicitFilter,50);
            }
            
            // If no results, ask Gemini for alternative queries (in user's language if provided) and retry
            if (tracks == null || !tracks.Any())
            {
                _logger.LogInformation("No tracks found for initial query, requesting alternative queries from Gemini.");

                var alternatives = await _geminiService.ExpandSearchQueriesAsync(
                    request.Mood ?? string.Empty,
                    request.Activity ?? string.Empty,
                    request.Description ?? string.Empty,
                    request.Language,
                    request.Genres,
                    maxAlternatives:3
                );

                foreach (var alt in alternatives)
                {
                    var altQuery = SanitizeQuery(alt);
                    if (!string.IsNullOrEmpty(artistName))
                    {
                        var artistFilter = SanitizeQuery(artistName);
                        if (!string.IsNullOrWhiteSpace(artistFilter)) altQuery = $"{altQuery} artist:{artistFilter}";
                    }

                    _logger.LogInformation("Trying alternative query: {Alt}", altQuery);

                    tracks = await _spotifyService.SearchTracksAsync(altQuery, request.ExplicitFilter,50);
                    if (tracks != null && tracks.Any())
                    {
                        _logger.LogInformation("Found tracks using alternative query.");
                        break;
                    }
                }

                // Try Spotify recommendations as a last resort (use provided genres or fallback to 'pop')
                if (tracks == null || !tracks.Any())
                {
                    var seed = string.IsNullOrWhiteSpace(request.Genres) ? "pop" : request.Genres.Split(',').First().Trim();
                    _logger.LogInformation("No results from searches. Falling back to recommendations using seed: {Seed}", seed);
                    var recs = await _spotifyService.GetRecommendationsAsync(seed, limit:10);
                    if (recs != null && recs.Any())
                    {
                        tracks = recs.ToList();
                      }
                }

                if (tracks == null || !tracks.Any())
                {
                    return Ok(new MusicResponse
                    {
                        Success = true,
                        Message = "No tracks found. Gemini attempted to rephrase your request but returned no matches.",
                        Tracks = new List<TrackInfo>()
                    });
                }
            }

            // Deduplicate tracks by Id if available, otherwise by Name+Artist
            tracks = tracks
                .GroupBy(t => string.IsNullOrEmpty(t.Id) ? ((t.Name ?? string.Empty) + "|" + (t.Artist ?? string.Empty)) : t.Id)
                .Select(g => g.First())
                .ToList();

            // If artistName detected, filter strictly by artist to be safe
            if (!string.IsNullOrEmpty(artistName))
            {
                tracks = tracks.Where(t => !string.IsNullOrEmpty(t.Artist) && t.Artist.IndexOf(artistName, StringComparison.OrdinalIgnoreCase) >=0).ToList();
            }

            // Limit returned tracks to5 for concise suggestions
            var limited = tracks.Take(5).ToList();

            // Improve diversity: if results are dominated by a single artist or album, fetch recommendations and merge
            var uniqueArtists = limited.Select(t => t.Artist ?? string.Empty).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var uniqueAlbums = limited.Select(t => t.AlbumName ?? string.Empty).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            if ((uniqueArtists <=1 || uniqueAlbums <=1) && (tracks.Count <10))
            {
                _logger.LogInformation("Low diversity detected (artists: {Artists}, albums: {Albums}). Fetching recommendations to improve variety.", uniqueArtists, uniqueAlbums);
                var seed = string.IsNullOrWhiteSpace(request.Genres) ? "pop" : request.Genres.Split(',').First().Trim();
                try
                {
                    var recs = await _spotifyService.GetRecommendationsAsync(seed, limit:20);
                    if (recs != null && recs.Any())
                    {
                        // apply explicit filter if set
                        if (request.ExplicitFilter)
                        {
                            recs = recs.Where(r => !r.IsExplicit).ToList();
                        }

                        // merge recommendations that are not already present
                        var existingKeys = new HashSet<string>(tracks.Select(t => string.IsNullOrEmpty(t.Id) ? ((t.Name ?? string.Empty) + "|" + (t.Artist ?? string.Empty)) : t.Id));
                        foreach (var r in recs)
                        {
                            var key = string.IsNullOrEmpty(r.Id) ? ((r.Name ?? string.Empty) + "|" + (r.Artist ?? string.Empty)) : r.Id;
                            if (!existingKeys.Contains(key))
                            {
                                tracks.Add(r);
                                existingKeys.Add(key);
                            }
                        }

                        // re-evaluate limited selection prioritizing artist variety
                        limited = tracks
                          .GroupBy(t => string.IsNullOrEmpty(t.Id) ? ((t.Name ?? string.Empty) + "|" + (t.Artist ?? string.Empty)) : t.Id)
                          .Select(g => g.First())
                          .OrderByDescending(t => t.Artist) // stable ordering
                          .ThenBy(t => t.Name)
                          .ToList()
                          .Take(5)
                          .ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch recommendations for diversity fallback.");
                }
            }

            if (!limited.Any())
            {
                return Ok(new MusicResponse
                {
                    Success = true,
                    Message = "No tracks found for the specified artist or query.",
                    Tracks = new List<TrackInfo>()
                });
            }

            var wittyQuotes = new[]
            {
                "Good music is the shorthand of emotion.",
                "Putting the right track on is like hitting the mood's sweet spot.",
                "Here's something to make your playlist proud.",
                "Soundtrack incoming. Emotions not included (but likely).",
                "Trust the vibes. These know the way."
            };

            var rand = new Random();
            var quote = wittyQuotes[rand.Next(wittyQuotes.Length)];

            return Ok(new MusicResponse
            {
                Success = true,
                Message = $"Found {limited.Count} tracks for you! — {quote}",
                Tracks = limited
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting music");
            return StatusCode(500, new MusicResponse
            {
                Success = false,
                Message = "An error occurred while searching for music. Please check your API configuration."
            });
        }
    }

    [HttpGet("genres")]
    public ActionResult<List<string>> GetAvailableGenres()
    {
        // Common Spotify seed genres
        var genres = new List<string>
        {
            "acoustic", "afrobeat", "alt-rock", "alternative", "ambient", "anime",
            "black-metal", "bluegrass", "blues", "bossanova", "brazil", "breakbeat",
            "british", "cantopop", "chicago-house", "children", "chill", "classical",
            "club", "comedy", "country", "dance", "dancehall", "death-metal", "deep-house",
            "detroit-techno", "disco", "disney", "drum-and-bass", "dub", "dubstep",
            "edm", "electro", "electronic", "emo", "folk", "forro", "french", "funk",
            "garage", "german", "gospel", "goth", "grindcore", "groove", "grunge",
            "guitar", "happy", "hard-rock", "hardcore", "hardstyle", "heavy-metal",
            "hip-hop", "holidays", "honky-tonk", "house", "idm", "indian", "indie",
            "indie-pop", "industrial", "iranian", "j-dance", "j-idol", "j-pop", "j-rock",
            "jazz", "k-pop", "kids", "latin", "latino", "malay", "mandopop", "metal",
            "metal-misc", "metalcore", "minimal-techno", "movies", "mpb", "new-age",
            "new-release", "opera", "pagode", "party", "philippines-opm", "piano",
            "pop", "pop-film", "post-dubstep", "power-pop", "progressive-house", "psych-rock",
            "punk", "punk-rock", "r-n-b", "rainy-day", "reggae", "reggaeton", "road-trip",
            "rock", "rock-n-roll", "rockabilly", "romance", "sad", "salsa", "samba",
            "sertanejo", "show-tunes", "singer-songwriter", "ska", "sleep", "songwriter",
            "soul", "soundtracks", "spanish", "study", "summer", "swedish", "synth-pop",
            "tango", "techno", "trance", "trip-hop", "turkish", "work-out", "world-music"
        };

        return Ok(genres);
    }

    // Helper to detect artist mentions in a free-form description
    private static string? ExtractArtistFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var desc = description.Trim();

        // Common patterns to match explicit user inputs like:
        // "by Adele", "songs by Adele", "songs from Adele", "from Adele", "Adele - Live" etc.
        var patterns = new[]
        {
            @"\bby[:\s]+(?<artist>[\w&\.\- '\\s]{1,80})\b",
            @"\bsongs?\s+by\s+(?<artist>[\w&\.\- '\\s]{1,80})\b",
            @"\bsongs?\s+from\s+(?<artist>[\w&\.\- '\\s]{1,80})\b",
            @"\bfrom\s+(?<artist>[\w&\.\- '\\s]{1,80})\b",
            @"^(?<artist>[\w&\.\- '\\s]{1,80})\s*(?:—|-|:)\s*",
            @"^(?<artist>[\w&\.\- '\\s]{1,80})\s+songs?\b",
            @"\b(?<artist>[\w&\.\- '\\s]{1,80})\s+songs?\s+from\b"
        };

        foreach (var pattern in patterns)
        {
            var m = Regex.Match(desc, pattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var artist = m.Groups["artist"].Value.Trim();
                if (!string.IsNullOrEmpty(artist))
                {
                    // Clean up quotes and trailing punctuation
                    artist = artist.Trim().Trim('"', '\'', ',', '.', '-', ':' ).Trim();
                    return artist;
                }
            }
        }

        // Do NOT treat short free-form descriptions as artist names anymore.
        return null;
    }

    // Helper: sanitize queries for Spotify (remove problematic characters and collapse whitespace)
    static string SanitizeQuery(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return string.Empty;
        // Replace smart quotes and other unicode quotes
        var s = q.Replace('\u201C', ' ').Replace('\u201D', ' ').Replace('\u2018', ' ').Replace('\u2019', ' ');
        // Remove control characters
        s = System.Text.RegularExpressions.Regex.Replace(s, "[\u0000-\u001F]", " ");
        // Remove unusual punctuation that can break queries, keep basic punctuation
        s = System.Text.RegularExpressions.Regex.Replace(s, "[\\\"#@\\$%&()=+{}\\[\\]|<>\\^~`]", " ");
        // Collapse whitespace
        s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", " ").Trim();
        // Truncate to reasonable length
        if (s.Length >200) s = s.Substring(0,200);
        return s;
    }
}
