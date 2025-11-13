# Vibium - Music Suggester Web App

A modern web application that suggests music based on your mood, activity, and preferences using the Spotify API and Google Gemini AI.

## Features

- ?? Music suggestions based on mood, activity, and description
- ?? AI-powered query refinement using Google Gemini (optional)
- ?? User preferences saved in localStorage (language, explicit filter, genres)
- ?? Clean, responsive UI with smooth animations
- ?? Loading states and error handling
- ?? Direct links to listen on Spotify
- ?? Mobile-friendly design

## Setup Instructions

### 1. Get Spotify API Credentials

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Log in or create a Spotify account
3. Click "Create an App"
4. Fill in the app details and accept the terms
5. Copy your **Client ID** and **Client Secret**

### 2. Get Google Gemini API Key (Optional)

1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy your API key

Note: Gemini is optional. If not configured, the app will use basic query building.

### 3. Configure the Application

Open `appsettings.json` and update with your credentials:

```json
{
  "Spotify": {
    "ClientId": "YOUR_SPOTIFY_CLIENT_ID",
    "ClientSecret": "YOUR_SPOTIFY_CLIENT_SECRET"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

**Important:** For production, use User Secrets or environment variables instead of storing credentials in appsettings.json

### 4. Run the Application

```bash
dotnet restore
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`

## Usage

1. **Enter Your Preferences:**
   - Mood (e.g., Happy, Relaxed, Energetic)
   - Activity (e.g., Workout, Study, Party)
   - Description (describe the music you want)

2. **Set Preferences (Optional):**
   - Language preference
   - Preferred genres (comma-separated)
   - Filter explicit content checkbox

3. **Click "Find Music":**
   - The app will refine your query (using Gemini if configured)
   - Search Spotify for matching tracks
   - Display results with album art and artist information

4. **Explore Results:**
   - Click on album art or "Open in Spotify" to listen
   - Your preferences are automatically saved for next time

## Technologies Used

- **Backend:**
  - ASP.NET Core 8.0 (MVC)
  - C# 12
  - HttpClient for API calls

- **Frontend:**
  - Razor Pages
  - Vanilla JavaScript
  - CSS3 with animations
  - Bootstrap 5

- **APIs:**
  - Spotify Web API
  - Google Gemini AI API (optional)

- **Storage:**
  - localStorage for user preferences

## Project Structure

```
vibium/
??? Controllers/
?   ??? HomeController.cs
?   ??? MusicController.cs
??? Models/
?   ??? SpotifyModels.cs
?   ??? GeminiModels.cs
?   ??? MusicRequest.cs
??? Services/
?   ??? SpotifyService.cs
?   ??? GeminiService.cs
??? Views/
?   ??? Home/
?   ?   ??? Index.cshtml
?   ??? Shared/
?       ??? _Layout.cshtml
??? wwwroot/
?   ??? css/
?   ?   ??? site.css
?   ??? js/
?       ??? music-suggester.js
??? Program.cs
??? appsettings.json
```

## Features Explained

### State Management
- User preferences are stored in browser localStorage
- Automatically restored on page reload
- Includes language, genres, and explicit filter settings

### API Integration
- **Spotify:** OAuth2 client credentials flow for authentication
- **Gemini:** Query refinement for better search results
- Graceful fallback if Gemini is not configured

### UI/UX
- Responsive grid layout for track cards
- Smooth animations and transitions
- Loading spinners during API calls
- Error and success messages
- Hover effects on track cards
- Direct Spotify playback links

## Troubleshooting

### "Spotify credentials not configured"
- Make sure you've added your Client ID and Client Secret to appsettings.json

### "No tracks found"
- Try being more specific with your mood/activity/description
- Check if your Spotify credentials are valid
- Ensure you have internet connectivity

### Gemini not working
- Verify your API key is correct
- Check that you have API quota remaining
- The app will work without Gemini using basic query building

## Security Notes

- Never commit `appsettings.json` with real credentials to version control
- Use User Secrets for development: `dotnet user-secrets set "Spotify:ClientId" "your-value"`
- Use environment variables or Azure Key Vault for production
- The Spotify Client Credentials flow is used (no user authentication required)

## License

This is a demonstration project. Feel free to use and modify as needed.

## Future Enhancements

- [ ] Add user authentication
- [ ] Save favorite tracks
- [ ] Create and export playlists to Spotify
- [ ] More advanced filtering options
- [ ] Audio preview before opening in Spotify
- [ ] Social sharing features
