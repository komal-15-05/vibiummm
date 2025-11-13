// Music Suggester JavaScript
const STORAGE_KEY = 'musicSuggesterPreferences';

// Load preferences from localStorage on page load
document.addEventListener('DOMContentLoaded', function() {
    loadPreferences();
    checkConfiguration();
    
    // Add form submit handler
    const form = document.getElementById('musicForm');
    if (form) {
        form.addEventListener('submit', handleSubmit);
    }
});

// Check if API configuration is set up
async function checkConfiguration() {
    try {
        const response = await fetch('/api/music/config-check');
        const data = await response.json();
        
        const statusEl = document.getElementById('configStatus');
        
        if (data.spotifyConfigured) {
            statusEl.innerHTML = `
                <div class="status-badge status-success">
                    ? Spotify Connected${data.geminiConfigured ? ' | ? Gemini AI Enabled' : ' | ? Gemini AI Not Configured (Optional)'}
                </div>
            `;
        } else {
            statusEl.innerHTML = `
                <div class="status-badge status-error">
                    ? Configuration Required - Check SETUP.md for instructions
                </div>
            `;
        }
    } catch (error) {
        console.error('Error checking configuration:', error);
    }
}

// Load saved preferences from localStorage
function loadPreferences() {
    const savedPrefs = localStorage.getItem(STORAGE_KEY);
    
    if (savedPrefs) {
        try {
            const prefs = JSON.parse(savedPrefs);
            
            if (prefs.language) document.getElementById('language').value = prefs.language;
            if (prefs.genres) document.getElementById('genres').value = prefs.genres;
            if (prefs.explicitFilter !== undefined) {
                document.getElementById('explicitFilter').checked = prefs.explicitFilter;
            }
        } catch (e) {
            console.error('Error loading preferences:', e);
        }
    }
}

// Save preferences to localStorage
function savePreferences() {
    const prefs = {
        language: document.getElementById('language').value,
        genres: document.getElementById('genres').value,
        explicitFilter: document.getElementById('explicitFilter').checked
    };
    
    localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
}

// Handle form submission
async function handleSubmit(e) {
    e.preventDefault();
    
    // Get form values
    const mood = document.getElementById('mood').value.trim();
    const activity = document.getElementById('activity').value.trim();
    const description = document.getElementById('description').value.trim();
    const language = document.getElementById('language').value.trim();
    const genres = document.getElementById('genres').value.trim();
    const explicitFilter = document.getElementById('explicitFilter').checked;
    
    // Validate input
    if (!mood && !activity && !description) {
        showError('Please provide at least mood, activity, or description.');
        return;
    }
    
    // Save preferences
    savePreferences();
    
    // Show loading state
    setLoadingState(true);
    hideMessages();
    hideResults();
    
    // Prepare request
    const requestData = {
        mood,
        activity,
        description,
        language: language || null,
        genres: genres || null,
        explicitFilter
    };
    
    try {
        const response = await fetch('/api/music/suggest', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });
        
        const data = await response.json();
        
        if (data.success) {
            if (data.tracks && data.tracks.length > 0) {
                displayTracks(data.tracks);
                showSuccess(data.message);
            } else {
                showError('No tracks found. Try adjusting your search criteria.');
            }
        } else {
            showError(data.message || 'An error occurred while searching for music.');
        }
    } catch (error) {
        console.error('Error:', error);
        showError('Failed to connect to the server. Please try again.');
    } finally {
        setLoadingState(false);
    }
}

// Display tracks in the results section
function displayTracks(tracks) {
    const tracksList = document.getElementById('tracksList');
    const resultsContainer = document.getElementById('resultsContainer');
    
    tracksList.innerHTML = '';
    
    tracks.forEach(track => {
        const trackCard = createTrackCard(track);
        tracksList.appendChild(trackCard);
    });
    
    resultsContainer.style.display = 'block';
    
    // Scroll to results
    setTimeout(() => {
        resultsContainer.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 100);
}

// Create a track card element
function createTrackCard(track) {
    const card = document.createElement('div');
    card.className = 'track-card';
    
    const albumArt = track.albumArt || 'https://via.placeholder.com/300x300?text=No+Image';
    
    card.innerHTML = `
        <div class="track-image">
            <img src="${albumArt}" alt="${escapeHtml(track.name)}" onerror="this.src='https://via.placeholder.com/300x300?text=No+Image'">
            <div class="play-overlay">
                <a href="${track.spotifyUrl}" target="_blank" rel="noopener noreferrer" class="play-button">
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="white">
                        <path d="M8 5v14l11-7z"/>
                    </svg>
                </a>
            </div>
        </div>
        <div class="track-info">
            <h3 class="track-name" title="${escapeHtml(track.name)}">${escapeHtml(track.name)}</h3>
            <p class="track-artist" title="${escapeHtml(track.artist)}">${escapeHtml(track.artist)}</p>
            <p class="track-album" title="${escapeHtml(track.albumName)}">${escapeHtml(track.albumName)}</p>
        </div>
        <div class="track-actions">
            <a href="${track.spotifyUrl}" target="_blank" rel="noopener noreferrer" class="spotify-link">
                Open in Spotify
            </a>
        </div>
    `;
    
    return card;
}

// Utility function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Show error message
function showError(message) {
    const errorEl = document.getElementById('errorMessage');
    errorEl.textContent = message;
    errorEl.style.display = 'block';
    
    // Scroll to error
    errorEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
}

// Show success message
function showSuccess(message) {
    const successEl = document.getElementById('successMessage');
    successEl.textContent = message;
    successEl.style.display = 'block';
}

// Hide messages
function hideMessages() {
    document.getElementById('errorMessage').style.display = 'none';
    document.getElementById('successMessage').style.display = 'none';
}

// Hide results
function hideResults() {
    document.getElementById('resultsContainer').style.display = 'none';
}

// Set loading state
function setLoadingState(isLoading) {
    const submitBtn = document.getElementById('submitBtn');
    const btnText = document.getElementById('btnText');
    const btnLoader = document.getElementById('btnLoader');
    
    if (isLoading) {
        submitBtn.disabled = true;
        btnText.style.display = 'none';
        btnLoader.style.display = 'inline-block';
    } else {
        submitBtn.disabled = false;
        btnText.style.display = 'inline';
        btnLoader.style.display = 'none';
    }
}
