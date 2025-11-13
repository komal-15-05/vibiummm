#!/bin/bash

echo "===================================="
echo "   Vibium Music Suggester"
echo "===================================="
echo ""

# Check if appsettings.json is configured
if grep -q "YOUR_SPOTIFY_CLIENT_ID" appsettings.json; then
    echo "? WARNING: Spotify credentials not configured!"
    echo "Please edit appsettings.json with your Spotify API credentials."
    echo "See SETUP.md for instructions."
    echo ""
    echo "Press Enter to continue anyway, or Ctrl+C to exit..."
    read
fi

echo "Starting application..."
echo ""
echo "Once started, open your browser to:"
echo "  - HTTPS: https://localhost:5001"
echo "  - HTTP:  http://localhost:5000"
echo ""
echo "Press Ctrl+C to stop the application"
echo ""

dotnet run
