<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>GitHub Sync - Dashboard</title>
    <link rel="stylesheet" href="css/style.css">
</head>
<body>
    <div class="container">
        <header>
            <h1>🔄 GitHub Sync Dashboard</h1>
            <p class="subtitle">Automatische Repository-Synchronisation</p>
        </header>

        <div class="stats-grid" id="statsGrid">
            <div class="stat-card">
                <div class="stat-value" id="totalRepos">0</div>
                <div class="stat-label">Repositories</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="syncedRepos">0</div>
                <div class="stat-label">Synchronisiert</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="errorRepos">0</div>
                <div class="stat-label">Fehler</div>
            </div>
            <div class="stat-card">
                <div class="stat-value" id="totalLogs">0</div>
                <div class="stat-label">Log-Einträge (24h)</div>
            </div>
        </div>

        <section class="section">
            <div class="section-header">
                <h2>Repositories</h2>
                <button class="btn btn-primary" onclick="showAddRepoModal()">+ Repository hinzufügen</button>
            </div>

            <div id="reposList" class="repos-list">
                <div class="loading">Lade Repositories...</div>
            </div>
        </section>

        <section class="section">
            <div class="section-header">
                <h2>Letzte Ereignisse</h2>
                <button class="btn btn-secondary" onclick="refreshLogs()">🔄 Aktualisieren</button>
            </div>

            <div id="logsList" class="logs-list">
                <div class="loading">Lade Logs...</div>
            </div>
        </section>
    </div>

    <!-- Modal: Add Repository -->
    <div id="addRepoModal" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h2>Repository hinzufügen</h2>
                <button class="close-btn" onclick="closeModal('addRepoModal')">&times;</button>
            </div>
            <div class="modal-body">
                <form id="addRepoForm" onsubmit="addRepository(event)">
                    <div class="form-group">
                        <label for="repoName">Name</label>
                        <input type="text" id="repoName" name="name" required placeholder="Mein Projekt">
                    </div>

                    <div class="form-group">
                        <label for="repoUrl">GitHub Repository URL</label>
                        <input type="url" id="repoUrl" name="repo_url" required
                               placeholder="https://github.com/user/repo.git"
                               onblur="fetchBranches()">
                        <small>Die HTTPS Clone URL des Repositories</small>
                    </div>

                    <div class="form-group">
                        <label for="repoBranch">Branch</label>
                        <select id="repoBranch" name="branch" required>
                            <option value="main">main</option>
                            <option value="master">master</option>
                        </select>
                        <small id="branchLoading" style="display:none;">Lade Branches...</small>
                    </div>

                    <div class="form-group">
                        <label for="targetPath">Ziel-Pfad auf Server</label>
                        <input type="text" id="targetPath" name="target_path" required
                               placeholder="/var/www/mein-projekt">
                        <small>Absoluter Pfad, wo das Repository geklont werden soll</small>
                    </div>

                    <div class="form-group">
                        <label class="checkbox-label">
                            <input type="checkbox" name="auto_sync" checked>
                            Auto-Sync aktivieren (reagiert auf Webhooks)
                        </label>
                    </div>

                    <div class="form-actions">
                        <button type="button" class="btn btn-secondary" onclick="closeModal('addRepoModal')">Abbrechen</button>
                        <button type="submit" class="btn btn-primary">Repository hinzufügen</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Modal: Rollback -->
    <div id="rollbackModal" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h2>Rollback durchführen</h2>
                <button class="close-btn" onclick="closeModal('rollbackModal')">&times;</button>
            </div>
            <div class="modal-body">
                <p>Wähle einen Commit aus, zu dem du zurückkehren möchtest:</p>
                <div id="commitsList" class="commits-list">
                    <div class="loading">Lade Commits...</div>
                </div>
                <div class="form-actions">
                    <button type="button" class="btn btn-secondary" onclick="closeModal('rollbackModal')">Abbrechen</button>
                </div>
            </div>
        </div>
    </div>

    <!-- Modal: Webhook Info -->
    <div id="webhookModal" class="modal">
        <div class="modal-content">
            <div class="modal-header">
                <h2>Webhook-Konfiguration</h2>
                <button class="close-btn" onclick="closeModal('webhookModal')">&times;</button>
            </div>
            <div class="modal-body">
                <div class="form-group">
                    <label>Payload URL</label>
                    <div class="input-with-copy">
                        <input type="text" id="webhookUrl" readonly>
                        <button class="btn btn-secondary btn-sm" onclick="copyToClipboard('webhookUrl')">Kopieren</button>
                    </div>
                </div>

                <div class="form-group">
                    <label>Secret</label>
                    <div class="input-with-copy">
                        <input type="text" id="webhookSecret" readonly>
                        <button class="btn btn-secondary btn-sm" onclick="copyToClipboard('webhookSecret')">Kopieren</button>
                    </div>
                </div>

                <div class="form-group">
                    <label>Content type</label>
                    <input type="text" value="application/json" readonly>
                </div>

                <div class="alert alert-info">
                    <strong>Einrichtung:</strong>
                    <ol>
                        <li>Gehe zu deinem GitHub Repository</li>
                        <li>Settings → Webhooks → Add webhook</li>
                        <li>Füge die obige Payload URL ein</li>
                        <li>Füge das Secret ein</li>
                        <li>Wähle "application/json" als Content type</li>
                        <li>Wähle "Just the push event"</li>
                        <li>Klicke auf "Add webhook"</li>
                    </ol>
                </div>
            </div>
        </div>
    </div>

    <!-- Toast Notifications -->
    <div id="toastContainer"></div>

    <script src="js/app.js"></script>
</body>
</html>
