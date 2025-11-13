# Quick Setup Guide for Vibium Music Suggester

## Step 1: Get Your Spotify Credentials

1. Visit: https://developer.spotify.com/dashboard
2. Log in with your Spotify account (create one if needed)
3. Click **"Create an App"**
4. Fill in:
   - App name: `Vibium Music Suggester`
   - App description: `A music suggestion web app`
   - Redirect URI: Leave empty (not needed for this flow)
5. Accept the terms and click **Create**
6. You'll see your **Client ID** immediately
7. Click **"Show Client Secret"** to reveal your **Client Secret**
8. Copy both values - you'll need them in Step 3

## Step 2: Get Your Gemini API Key (Optional)

The app works without Gemini, but it makes search queries smarter!

1. Visit: https://makersuite.google.com/app/apikey
2. Sign in with your Google account
3. Click **"Create API Key"**
4. Copy the API key - you'll need it in Step 3

## Step 3: Configure Your Application

1. Open `appsettings.json` in your project
2. Replace the placeholder values:

```json
{
  "Spotify": {
    "ClientId": "paste-your-spotify-client-id-here",
    "ClientSecret": "paste-your-spotify-client-secret-here"
  },
  "Gemini": {
    "ApiKey": "paste-your-gemini-api-key-here-or-leave-empty"
  }
}
```

## Step 4: Run the Application

Open a terminal in the project folder and run:

```bash
dotnet restore
dotnet run
```

The app will start at:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## Step 5: Use the App!

1. Open your browser and go to `https://localhost:5001`
2. Fill in your mood, activity, and description
3. Optionally set your preferences (language, genres, explicit filter)
4. Click **"Find Music"**
5. Enjoy your personalized music suggestions!

## Troubleshooting

### "Spotify credentials not configured"
- Double-check that you copied the Client ID and Client Secret correctly
- Make sure there are no extra spaces in the values
- Verify you saved the `appsettings.json` file

### "Failed to connect to the server"
- Make sure the app is running (`dotnet run`)
- Check that your browser is pointing to the correct URL
- Try refreshing the page

### No results found
- Try being more descriptive with your mood/activity
- Remove the explicit filter if it's enabled
- Try different genre combinations

### Gemini not working
- The app will still work without Gemini using basic query building
- Verify your API key is correct and has quota remaining
- Leave the Gemini ApiKey empty if you don't want to use it

## Security Warning ??

**Never commit your `appsettings.json` file with real credentials to version control!**

For production deployment:
- Use environment variables
- Use Azure Key Vault
- Use .NET User Secrets for development

## Need Help?

Check the full README.md for more detailed information and advanced configuration options.
