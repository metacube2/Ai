/**
 * Admin Settings Manager - AJAX ohne Reload
 */
const AdminSettings = {
    settings: {},

    init: function() {
        this.loadSettings();
        this.setupEventListeners();
    },

    loadSettings: function() {
        fetch(window.location.href, {
            method: 'POST',
            headers: {'Content-Type': 'application/x-www-form-urlencoded'},
            body: 'settings_action=get'
        })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                this.settings = data.settings;
                this.updateUI();
            }
        })
        .catch(err => console.error('Settings load error:', err));
    },

    updateSetting: function(key, value) {
        fetch(window.location.href, {
            method: 'POST',
            headers: {'Content-Type': 'application/x-www-form-urlencoded'},
            body: `settings_action=update&key=${encodeURIComponent(key)}&value=${encodeURIComponent(value)}`
        })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                this.showNotification('✓ Einstellung gespeichert', 'success');
                // Sofort UI aktualisieren
                this.applySettingImmediately(key, value);
            } else {
                this.showNotification('✗ Fehler beim Speichern', 'error');
            }
        })
        .catch(err => {
            console.error('Settings update error:', err);
            this.showNotification('✗ Netzwerkfehler', 'error');
        });
    },

    applySettingImmediately: function(key, value) {
        // Sofortige Anwendung ohne Reload
        switch(key) {
            case 'viewer_display.enabled':
                const viewerEl = document.querySelector('.viewer-stat');
                if (viewerEl) {
                    viewerEl.style.display = value === true || value === 'true' ? 'inline-flex' : 'none';
                }
                break;

            case 'viewer_display.min_viewers':
                // Wird beim nächsten Heartbeat angewendet
                window.minViewersToShow = parseInt(value);
                break;
        }
    },

    updateUI: function() {
        // Checkbox für Zuschauer-Anzeige
        const viewerEnabled = document.getElementById('setting-viewer-enabled');
        if (viewerEnabled) {
            viewerEnabled.checked = this.settings.viewer_display?.enabled ?? true;
        }

        // Mindestanzahl
        const minViewers = document.getElementById('setting-min-viewers');
        if (minViewers) {
            minViewers.value = this.settings.viewer_display?.min_viewers ?? 1;
        }

        // Video-Modus
        const playInPlayer = document.getElementById('setting-play-in-player');
        if (playInPlayer) {
            playInPlayer.checked = this.settings.video_mode?.play_in_player ?? true;
        }

        const allowDownload = document.getElementById('setting-allow-download');
        if (allowDownload) {
            allowDownload.checked = this.settings.video_mode?.allow_download ?? true;
        }
    },

    setupEventListeners: function() {
        // Zuschauer-Anzeige Toggle
        document.getElementById('setting-viewer-enabled')?.addEventListener('change', (e) => {
            this.updateSetting('viewer_display.enabled', e.target.checked);
        });

        // Mindestanzahl Zuschauer
        document.getElementById('setting-min-viewers')?.addEventListener('change', (e) => {
            this.updateSetting('viewer_display.min_viewers', e.target.value);
        });

        // Video im Player abspielen
        document.getElementById('setting-play-in-player')?.addEventListener('change', (e) => {
            this.updateSetting('video_mode.play_in_player', e.target.checked);
        });

        // Download erlauben
        document.getElementById('setting-allow-download')?.addEventListener('change', (e) => {
            this.updateSetting('video_mode.allow_download', e.target.checked);
        });
    },

    showNotification: function(message, type) {
        const notification = document.createElement('div');
        notification.className = `admin-notification ${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 25px;
            border-radius: 8px;
            background: ${type === 'success' ? '#4CAF50' : '#f44336'};
            color: white;
            font-weight: bold;
            z-index: 10000;
            animation: slideIn 0.3s ease;
        `;
        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 3000);
    }
};

// Initialisierung nur im Admin-Bereich
document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('admin-settings-panel')) {
        AdminSettings.init();
    }
});
