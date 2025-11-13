@echo off
echo ====================================
echo    Vibium Music Suggester
echo ====================================
echo.

REM Check if appsettings.json is configured
findstr /C:"YOUR_SPOTIFY_CLIENT_ID" appsettings.json >nul
if %errorlevel% == 0 (
    echo WARNING: Spotify credentials not configured!
    echo Please edit appsettings.json with your Spotify API credentials.
    echo See SETUP.md for instructions.
    echo.
    echo Press any key to continue anyway, or Ctrl+C to exit...
    pause >nul
)

echo Starting application...
echo.
echo Once started, open your browser to:
echo   - HTTPS: https://localhost:5001
echo   - HTTP:  http://localhost:5000
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run
