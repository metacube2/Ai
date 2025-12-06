/**
 * GitHub Sync Dashboard - Frontend JavaScript
 */

// State
let repositories = [];
let logs = [];
let stats = {};
let currentRollbackRepoId = null;

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    loadDashboard();
    // Auto-refresh every 30 seconds
    setInterval(loadDashboard, 30000);
});

/**
 * Load complete dashboard
 */
async function loadDashboard() {
    await Promise.all([
        loadRepositories(),
        loadLogs()
    ]);
    updateStats();
}

/**
 * Load repositories
 */
async function loadRepositories() {
    try {
        const response = await fetch('api/repos.php');
        const data = await response.json();

        if (data.repositories) {
            repositories = data.repositories;
            renderRepositories();
        }
    } catch (error) {
        console.error('Error loading repositories:', error);
        showToast('Fehler beim Laden der Repositories', 'error');
    }
}

/**
 * Render repositories list
 */
function renderRepositories() {
    const container = document.getElementById('reposList');

    if (repositories.length === 0) {
        container.innerHTML = '<div class="empty-state">Keine Repositories konfiguriert. Füge dein erstes Repository hinzu!</div>';
        return;
    }

    container.innerHTML = repositories.map(repo => `
        <div class="repo-card status-${repo.status || 'pending'}">
            <div class="repo-header">
                <div>
                    <div class="repo-name">${escapeHtml(repo.name)}</div>
                    <div class="repo-url">${escapeHtml(repo.repo_url)}</div>
                </div>
                <span class="repo-status ${repo.status || 'pending'}">${getStatusText(repo.status)}</span>
            </div>

            <div class="repo-info">
                <div class="info-item">
                    <span class="info-label">Branch</span>
                    <span class="info-value">${escapeHtml(repo.branch)}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">Ziel-Pfad</span>
                    <span class="info-value">${escapeHtml(repo.target_path)}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">Letzte Sync</span>
                    <span class="info-value">${repo.last_sync || 'Noch nie'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">Auto-Sync</span>
                    <span class="info-value">${repo.auto_sync ? '✅ Aktiv' : '❌ Inaktiv'}</span>
                </div>
            </div>

            <div class="repo-actions">
                <button class="btn btn-success btn-sm" onclick="syncRepository('${repo.id}')">
                    🔄 Manueller Sync
                </button>
                <button class="btn btn-secondary btn-sm" onclick="showRollbackModal('${repo.id}')">
                    ⏪ Rollback
                </button>
                <button class="btn btn-secondary btn-sm" onclick="showWebhookInfo('${repo.id}')">
                    🔗 Webhook Info
                </button>
                <button class="btn btn-danger btn-sm" onclick="deleteRepository('${repo.id}', '${escapeHtml(repo.name)}')">
                    🗑️ Entfernen
                </button>
            </div>
        </div>
    `).join('');
}

/**
 * Load logs
 */
async function loadLogs() {
    try {
        const response = await fetch('api/log.php?limit=50');
        const data = await response.json();

        if (data.success) {
            logs = data.logs;
            stats = data.stats;
            renderLogs();
        }
    } catch (error) {
        console.error('Error loading logs:', error);
        showToast('Fehler beim Laden der Logs', 'error');
    }
}

/**
 * Render logs list
 */
function renderLogs() {
    const container = document.getElementById('logsList');

    if (logs.length === 0) {
        container.innerHTML = '<div class="empty-state">Noch keine Log-Einträge vorhanden.</div>';
        return;
    }

    container.innerHTML = logs.map(log => {
        const repo = repositories.find(r => r.id === log.repo_id);
        const repoName = repo ? repo.name : log.repo_id;

        return `
            <div class="log-entry ${log.type}">
                <div class="log-timestamp">${log.timestamp}</div>
                <div class="log-content">
                    <div class="log-message">
                        <strong>${escapeHtml(repoName)}</strong> - ${escapeHtml(log.message)}
                    </div>
                    ${log.details && Object.keys(log.details).length > 0 ? `
                        <div class="log-details">${formatLogDetails(log.details)}</div>
                    ` : ''}
                </div>
            </div>
        `;
    }).join('');
}

/**
 * Update statistics
 */
function updateStats() {
    const totalRepos = repositories.length;
    const syncedRepos = repositories.filter(r => r.status === 'synced').length;
    const errorRepos = repositories.filter(r => r.status === 'error' || r.status === 'conflict').length;

    document.getElementById('totalRepos').textContent = totalRepos;
    document.getElementById('syncedRepos').textContent = syncedRepos;
    document.getElementById('errorRepos').textContent = errorRepos;
    document.getElementById('totalLogs').textContent = stats.last_24h || 0;
}

/**
 * Show add repository modal
 */
function showAddRepoModal() {
    document.getElementById('addRepoModal').classList.add('active');
}

/**
 * Add repository
 */
async function addRepository(event) {
    event.preventDefault();

    const form = event.target;
    const formData = new FormData(form);

    const data = {
        name: formData.get('name'),
        repo_url: formData.get('repo_url'),
        branch: formData.get('branch'),
        target_path: formData.get('target_path'),
        auto_sync: formData.get('auto_sync') === 'on'
    };

    try {
        const response = await fetch('api/repos.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.success) {
            showToast('Repository erfolgreich hinzugefügt!', 'success');
            closeModal('addRepoModal');
            form.reset();

            // Show webhook info
            if (result.repository.webhook_secret) {
                showWebhookInfoData(result.repository.webhook_url, result.repository.webhook_secret);
            }

            loadDashboard();
        } else {
            showToast(result.error || 'Fehler beim Hinzufügen des Repositories', 'error');
        }
    } catch (error) {
        console.error('Error adding repository:', error);
        showToast('Fehler beim Hinzufügen des Repositories', 'error');
    }
}

/**
 * Sync repository manually
 */
async function syncRepository(repoId) {
    const repo = repositories.find(r => r.id === repoId);

    if (!repo) return;

    showToast(`Synchronisiere ${repo.name}...`, 'info');

    try {
        const response = await fetch('api/sync.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ repo_id: repoId })
        });

        const result = await response.json();

        if (result.success) {
            showToast(`${repo.name} erfolgreich synchronisiert! ${result.files_changed} Datei(en) geändert.`, 'success');
            loadDashboard();
        } else {
            if (result.conflict) {
                showToast(`Merge-Konflikt in ${repo.name}! Manuelle Lösung erforderlich.`, 'warning');
            } else {
                showToast(result.message || 'Fehler beim Synchronisieren', 'error');
            }
            loadDashboard();
        }
    } catch (error) {
        console.error('Error syncing repository:', error);
        showToast('Fehler beim Synchronisieren', 'error');
    }
}

/**
 * Show rollback modal
 */
async function showRollbackModal(repoId) {
    currentRollbackRepoId = repoId;
    const modal = document.getElementById('rollbackModal');
    const commitsList = document.getElementById('commitsList');

    modal.classList.add('active');
    commitsList.innerHTML = '<div class="loading">Lade Commits...</div>';

    try {
        const response = await fetch(`api/rollback.php?repo_id=${repoId}&limit=20`);
        const result = await response.json();

        if (result.success && result.commits.length > 0) {
            commitsList.innerHTML = result.commits.map(commit => `
                <div class="commit-item" onclick="performRollback('${commit.hash}')">
                    <div class="commit-hash">${commit.hash_short}</div>
                    <div class="commit-message">${escapeHtml(commit.message)}</div>
                    <div class="commit-meta">
                        ${escapeHtml(commit.author_name)} - ${commit.timestamp}
                    </div>
                </div>
            `).join('');
        } else {
            commitsList.innerHTML = '<div class="empty-state">Keine Commits gefunden.</div>';
        }
    } catch (error) {
        console.error('Error loading commits:', error);
        commitsList.innerHTML = '<div class="empty-state">Fehler beim Laden der Commits.</div>';
    }
}

/**
 * Perform rollback
 */
async function performRollback(commitHash) {
    if (!currentRollbackRepoId) return;

    const repo = repositories.find(r => r.id === currentRollbackRepoId);

    if (!confirm(`Möchtest du wirklich einen Rollback zu Commit ${commitHash.substring(0, 7)} durchführen?\n\nDies erstellt einen neuen Revert-Commit.`)) {
        return;
    }

    showToast(`Führe Rollback durch...`, 'info');

    try {
        const response = await fetch('api/rollback.php', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                repo_id: currentRollbackRepoId,
                commit_hash: commitHash
            })
        });

        const result = await response.json();

        if (result.success) {
            showToast('Rollback erfolgreich durchgeführt!', 'success');
            closeModal('rollbackModal');
            loadDashboard();
        } else {
            showToast(result.message || 'Fehler beim Rollback', 'error');
        }
    } catch (error) {
        console.error('Error performing rollback:', error);
        showToast('Fehler beim Rollback', 'error');
    }
}

/**
 * Show webhook info modal
 */
async function showWebhookInfo(repoId) {
    const repo = repositories.find(r => r.id === repoId);

    if (!repo) return;

    // Fetch webhook secret from config
    try {
        const response = await fetch(`api/repos.php?id=${repoId}`);
        const data = await response.json();

        const webhookUrl = window.location.origin + window.location.pathname.replace('index.php', '') + 'webhook.php';
        const webhookSecret = '(Secret gespeichert auf Server)';

        showWebhookInfoData(webhookUrl, webhookSecret);
    } catch (error) {
        console.error('Error loading webhook info:', error);
    }
}

/**
 * Show webhook info with data
 */
function showWebhookInfoData(url, secret) {
    document.getElementById('webhookUrl').value = url;
    document.getElementById('webhookSecret').value = secret;
    document.getElementById('webhookModal').classList.add('active');
}

/**
 * Delete repository
 */
async function deleteRepository(repoId, repoName) {
    const deleteFiles = confirm(`Repository "${repoName}" aus Konfiguration entfernen?\n\nKlicke OK, um auch die Dateien vom Server zu löschen.\nKlicke Abbrechen, um nur die Konfiguration zu entfernen.`);

    if (deleteFiles === null) return; // User cancelled

    try {
        const response = await fetch('api/repos.php', {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                id: repoId,
                delete_files: deleteFiles
            })
        });

        const result = await response.json();

        if (result.success) {
            showToast(`Repository "${repoName}" wurde entfernt.`, 'success');
            loadDashboard();
        } else {
            showToast(result.error || 'Fehler beim Löschen', 'error');
        }
    } catch (error) {
        console.error('Error deleting repository:', error);
        showToast('Fehler beim Löschen', 'error');
    }
}

/**
 * Fetch branches from GitHub
 */
async function fetchBranches() {
    const repoUrl = document.getElementById('repoUrl').value;

    if (!repoUrl) return;

    const branchSelect = document.getElementById('repoBranch');
    const branchLoading = document.getElementById('branchLoading');

    branchLoading.style.display = 'block';

    try {
        // This would need to be implemented in the backend
        // For now, keep default branches
        branchLoading.style.display = 'none';
    } catch (error) {
        console.error('Error fetching branches:', error);
        branchLoading.style.display = 'none';
    }
}

/**
 * Refresh logs
 */
function refreshLogs() {
    loadLogs();
    showToast('Logs aktualisiert', 'info');
}

/**
 * Close modal
 */
function closeModal(modalId) {
    document.getElementById(modalId).classList.remove('active');
}

/**
 * Show toast notification
 */
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');

    toast.className = `toast ${type}`;
    toast.textContent = message;

    container.appendChild(toast);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

/**
 * Copy text to clipboard
 */
function copyToClipboard(elementId) {
    const element = document.getElementById(elementId);
    element.select();
    document.execCommand('copy');
    showToast('In Zwischenablage kopiert!', 'success');
}

/**
 * Helper: Escape HTML
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Helper: Get status text
 */
function getStatusText(status) {
    const statusMap = {
        'synced': '✅ Synchronisiert',
        'cloning': '⏳ Wird geklont...',
        'error': '❌ Fehler',
        'conflict': '⚠️ Konflikt',
        'pending': '⏸️ Ausstehend'
    };

    return statusMap[status] || status;
}

/**
 * Helper: Format log details
 */
function formatLogDetails(details) {
    return Object.entries(details)
        .map(([key, value]) => `${key}: ${value}`)
        .join(' | ');
}

// Close modal when clicking outside
window.addEventListener('click', function(event) {
    if (event.target.classList.contains('modal')) {
        event.target.classList.remove('active');
    }
});

// Close modal with Escape key
window.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        document.querySelectorAll('.modal.active').forEach(modal => {
            modal.classList.remove('active');
        });
    }
});
