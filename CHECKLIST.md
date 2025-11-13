# Setup Checklist for Vibium Music Suggester

Use this checklist to ensure everything is configured correctly before running the app.

## ? Prerequisites

- [ ] .NET 8.0 SDK installed (check with `dotnet --version`)
- [ ] Visual Studio 2022, VS Code, or JetBrains Rider (optional but recommended)
- [ ] Modern web browser (Chrome, Firefox, Edge, Safari)
- [ ] Internet connection for API calls

## ? Spotify API Setup

- [ ] Created a Spotify Developer account at https://developer.spotify.com/dashboard
- [ ] Created a new app in the Spotify Dashboard
- [ ] Copied the Client ID
- [ ] Revealed and copied the Client Secret
- [ ] Added credentials to `appsettings.json` or User Secrets

## ? Gemini AI Setup (Optional)

- [ ] Visited https://makersuite.google.com/app/apikey
- [ ] Created a Google AI API key
- [ ] Added API key to `appsettings.json` or User Secrets
- [ ] **OR** Left empty if not using Gemini (app will still work)

## ? Configuration

Choose ONE of the following methods:

### Option A: appsettings.json (Simple, less secure)
- [ ] Opened `appsettings.json`
- [ ] Replaced `YOUR_SPOTIFY_CLIENT_ID` with actual Client ID
- [ ] Replaced `YOUR_SPOTIFY_CLIENT_SECRET` with actual Client Secret
- [ ] Replaced `YOUR_GEMINI_API_KEY` with actual API key (or left as is)
- [ ] Saved the file
- [ ] **Added `appsettings.json` to `.gitignore` if using version control**

### Option B: User Secrets (Recommended, more secure)
- [ ] Ran `dotnet user-secrets init` in project directory
- [ ] Ran `dotnet user-secrets set "Spotify:ClientId" "your-client-id"`
- [ ] Ran `dotnet user-secrets set "Spotify:ClientSecret" "your-client-secret"`
- [ ] Ran `dotnet user-secrets set "Gemini:ApiKey" "your-api-key"` (optional)
- [ ] Verified with `dotnet user-secrets list`

## ? Build and Run

- [ ] Opened terminal in project directory
- [ ] Ran `dotnet restore` (restores NuGet packages)
- [ ] Ran `dotnet build` (builds the project)
- [ ] No build errors displayed
- [ ] Ran `dotnet run` or used `start.bat` / `start.sh`
- [ ] Application started without errors

## ? Access the Application

- [ ] Opened web browser
- [ ] Navigated to `https://localhost:5001` or `http://localhost:5000`
- [ ] Page loaded successfully
- [ ] Configuration status shows "? Spotify Connected"

## ? Test the Application

- [ ] Filled in "Mood" field (e.g., "Happy")
- [ ] Filled in "Activity" field (e.g., "Workout")
- [ ] Added optional description
- [ ] Set preferences (language, genres, explicit filter)
- [ ] Clicked "Find Music" button
- [ ] Loading spinner appeared
- [ ] Results displayed with album art
- [ ] Clicked "Open in Spotify" link (opens in new tab)
- [ ] Spotify plays the track

## ? Verify Preferences Persistence

- [ ] Set language and genres
- [ ] Checked "Filter explicit content"
- [ ] Refreshed the page (F5)
- [ ] Preferences are still filled in (loaded from localStorage)

## ? Troubleshooting Completed

If you encountered issues, verify:
- [ ] API credentials are correct (no extra spaces)
- [ ] Internet connection is working
- [ ] Firewall/antivirus isn't blocking the app
- [ ] Ports 5000/5001 aren't in use by another app
- [ ] Browser allows HTTPS with self-signed certificate
- [ ] Checked browser console for JavaScript errors (F12)

## ? Security Best Practices

- [ ] Using User Secrets for development (recommended)
- [ ] `appsettings.json` is in `.gitignore` if it contains credentials
- [ ] Planning to use environment variables or Key Vault for production
- [ ] Not sharing credentials in screenshots or public repositories

## ? Documentation Review

- [ ] Read `README.md` for full documentation
- [ ] Read `SETUP.md` for detailed setup instructions
- [ ] Reviewed `PROJECT-SUMMARY.md` for project overview
- [ ] Checked `USER-SECRETS.md` for secure credential management

## ?? Success!

If all boxes are checked, your Vibium Music Suggester is fully set up and working!

## ?? Notes

Write any issues or observations here:

______________________________________________________________________________

______________________________________________________________________________

______________________________________________________________________________

## ?? Need Help?

Common issues:

**"Spotify credentials not configured"**
? Check that credentials in appsettings.json or User Secrets are correct

**No results found**
? Try different mood/activity combinations, check internet connection

**Configuration check fails**
? Restart the application after updating credentials

**Port already in use**
? Change port in `Properties/launchSettings.json` or stop other apps

**Browser won't connect**
? Try HTTP (port 5000) instead of HTTPS, or accept the self-signed certificate

---

Last updated: [Date]
Configuration verified by: [Your Name]
