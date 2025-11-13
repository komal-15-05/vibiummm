using Microsoft.AspNetCore.Mvc;
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

            // Use Gemini to refine the search query if available
            var searchQuery = await _geminiService.RefineSearchQueryAsync(
                request.Mood ?? "",
                request.Activity ?? "",
                request.Description ?? "",
                request.Language,
                request.Genres
            );

            _logger.LogInformation($"Search query: {searchQuery}");

            // Search Spotify with the refined query
            var tracks = await _spotifyService.SearchTracksAsync(searchQuery, request.ExplicitFilter, 20);

            if (tracks == null || !tracks.Any())
            {
                return Ok(new MusicResponse
                {
                    Success = true,
                    Message = "No tracks found. Try adjusting your search criteria.",
                    Tracks = new List<TrackInfo>()
                });
            }

            return Ok(new MusicResponse
            {
                Success = true,
                Message = $"Found {tracks.Count} tracks for you!",
                Tracks = tracks
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
}
