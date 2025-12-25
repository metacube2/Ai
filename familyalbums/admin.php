<?php
/**
 * FamilyAlbums - Admin Interface
 */

require_once __DIR__ . '/config.php';
session_start();

$pageTitle = SITE_TITLE . ' - Administration';
?>
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= e($pageTitle) ?></title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css">
    <style>
        .tag-input { display: flex; flex-wrap: wrap; gap: 0.5rem; padding: 0.5rem; border: 1px solid #d1d5db; border-radius: 0.5rem; }
        .tag-item { background: #dbeafe; color: #1d4ed8; padding: 0.25rem 0.5rem; border-radius: 9999px; display: flex; align-items: center; gap: 0.25rem; }
        .tag-item button { color: #1d4ed8; cursor: pointer; }
        .tag-input input { flex: 1; min-width: 100px; border: none; outline: none; }
        .suggestions { position: absolute; top: 100%; left: 0; right: 0; background: white; border: 1px solid #d1d5db; border-radius: 0.5rem; max-height: 200px; overflow-y: auto; z-index: 10; }
        .suggestions div { padding: 0.5rem 1rem; cursor: pointer; }
        .suggestions div:hover { background: #f3f4f6; }
    </style>
</head>
<body class="bg-gray-100 min-h-screen">
    <!-- Login-Bereich (wird per JS gesteuert) -->
    <div id="login-section" class="hidden min-h-screen flex items-center justify-center">
        <div class="bg-white p-8 rounded-xl shadow-lg w-full max-w-md">
            <h1 class="text-2xl font-bold text-center mb-6">
                <i class="fas fa-lock mr-2 text-blue-600"></i>Admin Login
            </h1>
            <form id="login-form">
                <div class="mb-4">
                    <label class="block text-gray-700 mb-2">Passwort</label>
                    <input type="password" id="login-password" required
                           class="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                           placeholder="Admin-Passwort eingeben">
                </div>
                <div id="login-error" class="hidden text-red-500 text-sm mb-4"></div>
                <button type="submit" class="w-full bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 transition">
                    <i class="fas fa-sign-in-alt mr-2"></i>Anmelden
                </button>
            </form>
            <p class="mt-4 text-center">
                <a href="index.php" class="text-blue-600 hover:underline">
                    <i class="fas fa-arrow-left mr-1"></i>Zurück zur Galerie
                </a>
            </p>
        </div>
    </div>

    <!-- Admin-Bereich -->
    <div id="admin-section" class="hidden">
        <!-- Header -->
        <header class="bg-gradient-to-r from-gray-800 to-gray-900 text-white shadow-lg">
            <div class="container mx-auto px-4 py-4">
                <div class="flex items-center justify-between">
                    <h1 class="text-xl font-bold">
                        <i class="fas fa-cog mr-2"></i><?= e($pageTitle) ?>
                    </h1>
                    <div class="flex items-center gap-4">
                        <a href="index.php" class="text-white/80 hover:text-white">
                            <i class="fas fa-eye mr-1"></i>Galerie
                        </a>
                        <button onclick="logout()" class="text-white/80 hover:text-white">
                            <i class="fas fa-sign-out-alt mr-1"></i>Logout
                        </button>
                    </div>
                </div>
            </div>
        </header>

        <!-- Tabs -->
        <div class="bg-white shadow">
            <div class="container mx-auto px-4">
                <nav class="flex gap-4">
                    <button onclick="showTab('albums')" id="tab-albums"
                            class="tab-btn py-4 px-2 border-b-2 border-blue-600 text-blue-600 font-medium">
                        <i class="fas fa-images mr-1"></i>Alben
                    </button>
                    <button onclick="showTab('comments')" id="tab-comments"
                            class="tab-btn py-4 px-2 border-b-2 border-transparent text-gray-500 hover:text-gray-700">
                        <i class="fas fa-comments mr-1"></i>Kommentare
                    </button>
                </nav>
            </div>
        </div>

        <!-- Content -->
        <main class="container mx-auto px-4 py-8">
            <!-- Alben-Tab -->
            <div id="content-albums">
                <!-- Album hinzufügen -->
                <div class="bg-white rounded-xl shadow-md p-6 mb-8">
                    <h2 class="text-xl font-semibold mb-4">
                        <i class="fas fa-plus-circle mr-2 text-green-600"></i>
                        <span id="form-title">Neues Album hinzufügen</span>
                    </h2>
                    <form id="album-form" class="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <input type="hidden" id="album-id">

                        <div>
                            <label class="block text-gray-700 mb-1">Titel *</label>
                            <input type="text" id="album-title" required
                                   class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                                   placeholder="z.B. Weihnachten bei Oma">
                        </div>

                        <div>
                            <label class="block text-gray-700 mb-1">Datum *</label>
                            <input type="date" id="album-date" required
                                   class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
                        </div>

                        <div class="md:col-span-2">
                            <label class="block text-gray-700 mb-1">Nextcloud-Link *</label>
                            <input type="url" id="album-url" required
                                   class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                                   placeholder="https://nextcloud.example.com/apps/photos/public/...">
                        </div>

                        <div class="md:col-span-2">
                            <label class="block text-gray-700 mb-1">Beschreibung</label>
                            <textarea id="album-description" rows="2"
                                      class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"
                                      placeholder="Kurze Beschreibung des Albums"></textarea>
                        </div>

                        <div class="md:col-span-2 relative">
                            <label class="block text-gray-700 mb-1">Tags</label>
                            <div class="tag-input" id="tags-container">
                                <input type="text" id="tag-input" placeholder="Tag eingeben und Enter drücken">
                            </div>
                            <div id="tag-suggestions" class="suggestions hidden"></div>
                            <input type="hidden" id="album-tags">
                        </div>

                        <div class="md:col-span-2">
                            <label class="block text-gray-700 mb-1">Vorschaubild (optional)</label>
                            <div class="flex gap-2">
                                <input type="file" id="thumbnail-file" accept="image/*"
                                       class="flex-1 px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
                                <button type="button" onclick="uploadThumbnail()" class="px-4 py-2 bg-gray-200 rounded-lg hover:bg-gray-300">
                                    <i class="fas fa-upload"></i>
                                </button>
                            </div>
                            <input type="hidden" id="album-thumbnail">
                            <div id="thumbnail-preview" class="mt-2"></div>
                        </div>

                        <div class="md:col-span-2 flex gap-2">
                            <button type="submit" class="bg-green-600 text-white px-6 py-2 rounded-lg hover:bg-green-700 transition">
                                <i class="fas fa-save mr-2"></i><span id="submit-text">Speichern</span>
                            </button>
                            <button type="button" onclick="resetForm()" class="bg-gray-200 px-6 py-2 rounded-lg hover:bg-gray-300 transition">
                                <i class="fas fa-times mr-2"></i>Abbrechen
                            </button>
                        </div>
                    </form>
                </div>

                <!-- Album-Liste -->
                <div class="bg-white rounded-xl shadow-md p-6">
                    <h2 class="text-xl font-semibold mb-4">
                        <i class="fas fa-list mr-2 text-blue-600"></i>Alle Alben
                    </h2>
                    <div id="albums-list" class="overflow-x-auto">
                        <table class="w-full">
                            <thead class="bg-gray-50">
                                <tr>
                                    <th class="px-4 py-3 text-left text-gray-600">Titel</th>
                                    <th class="px-4 py-3 text-left text-gray-600">Datum</th>
                                    <th class="px-4 py-3 text-left text-gray-600">Tags</th>
                                    <th class="px-4 py-3 text-right text-gray-600">Aktionen</th>
                                </tr>
                            </thead>
                            <tbody id="albums-table-body">
                                <!-- Wird per JS befüllt -->
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Kommentare-Tab -->
            <div id="content-comments" class="hidden">
                <div class="bg-white rounded-xl shadow-md p-6">
                    <h2 class="text-xl font-semibold mb-4">
                        <i class="fas fa-comments mr-2 text-blue-600"></i>Alle Kommentare
                    </h2>
                    <div id="comments-list" class="space-y-4">
                        <!-- Wird per JS befüllt -->
                    </div>
                </div>
            </div>
        </main>
    </div>

    <!-- Bestätigungs-Modal -->
    <div id="confirm-modal" class="hidden fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
        <div class="bg-white rounded-xl shadow-2xl max-w-md w-full p-6">
            <h3 class="text-lg font-semibold mb-4" id="confirm-title">Bestätigung</h3>
            <p id="confirm-message" class="text-gray-600 mb-6"></p>
            <div class="flex justify-end gap-2">
                <button onclick="closeConfirm()" class="px-4 py-2 bg-gray-200 rounded-lg hover:bg-gray-300">
                    Abbrechen
                </button>
                <button id="confirm-btn" class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700">
                    Löschen
                </button>
            </div>
        </div>
    </div>

    <script>
    // === State ===
    let csrfToken = '';
    let allTags = [];
    let currentTags = [];
    let editingAlbumId = null;
    let confirmCallback = null;

    // === Auth ===
    async function checkAuth() {
        const response = await fetch('api.php?action=check_auth');
        const data = await response.json();

        if (data.authenticated) {
            csrfToken = data.csrf;
            document.getElementById('login-section').classList.add('hidden');
            document.getElementById('admin-section').classList.remove('hidden');
            loadAlbums();
            loadAllTags();
        } else {
            document.getElementById('login-section').classList.remove('hidden');
            document.getElementById('admin-section').classList.add('hidden');
        }
    }

    document.getElementById('login-form').addEventListener('submit', async (e) => {
        e.preventDefault();
        const password = document.getElementById('login-password').value;
        const errorDiv = document.getElementById('login-error');

        try {
            const response = await fetch('api.php?action=login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ password })
            });
            const data = await response.json();

            if (data.success) {
                csrfToken = data.csrf;
                document.getElementById('login-section').classList.add('hidden');
                document.getElementById('admin-section').classList.remove('hidden');
                loadAlbums();
                loadAllTags();
            } else {
                errorDiv.textContent = data.error || 'Login fehlgeschlagen';
                errorDiv.classList.remove('hidden');
            }
        } catch (err) {
            errorDiv.textContent = 'Verbindungsfehler';
            errorDiv.classList.remove('hidden');
        }
    });

    async function logout() {
        await fetch('api.php?action=logout', { method: 'POST' });
        location.reload();
    }

    // === Tabs ===
    function showTab(tab) {
        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.classList.remove('border-blue-600', 'text-blue-600');
            btn.classList.add('border-transparent', 'text-gray-500');
        });
        document.getElementById(`tab-${tab}`).classList.add('border-blue-600', 'text-blue-600');
        document.getElementById(`tab-${tab}`).classList.remove('border-transparent', 'text-gray-500');

        document.getElementById('content-albums').classList.add('hidden');
        document.getElementById('content-comments').classList.add('hidden');
        document.getElementById(`content-${tab}`).classList.remove('hidden');

        if (tab === 'comments') {
            loadAllComments();
        }
    }

    // === Albums ===
    async function loadAlbums() {
        const response = await fetch('api.php?action=albums');
        const data = await response.json();
        renderAlbumsTable(data.albums || []);
    }

    function renderAlbumsTable(albums) {
        const tbody = document.getElementById('albums-table-body');

        if (albums.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center py-8 text-gray-500">Noch keine Alben vorhanden</td></tr>';
            return;
        }

        tbody.innerHTML = albums.map(album => `
            <tr class="border-t hover:bg-gray-50">
                <td class="px-4 py-3">
                    <div class="font-medium">${escapeHtml(album.title)}</div>
                    <div class="text-sm text-gray-500 truncate max-w-xs">${escapeHtml(album.url)}</div>
                </td>
                <td class="px-4 py-3 text-gray-600">${album.date}</td>
                <td class="px-4 py-3">
                    <div class="flex flex-wrap gap-1">
                        ${album.tags.slice(0, 3).map(tag =>
                            `<span class="bg-blue-100 text-blue-700 text-xs px-2 py-0.5 rounded-full">${escapeHtml(tag)}</span>`
                        ).join('')}
                        ${album.tags.length > 3 ? `<span class="text-gray-400 text-xs">+${album.tags.length - 3}</span>` : ''}
                    </div>
                </td>
                <td class="px-4 py-3 text-right">
                    <button onclick='editAlbum(${JSON.stringify(album).replace(/'/g, "&#39;")})' class="text-blue-600 hover:text-blue-800 mr-2">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button onclick="confirmDelete('album', '${album.id}', '${escapeHtml(album.title)}')" class="text-red-600 hover:text-red-800">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `).join('');
    }

    async function loadAllTags() {
        const response = await fetch('api.php?action=tags');
        const data = await response.json();
        allTags = data.tags || [];
    }

    // === Album Form ===
    document.getElementById('album-form').addEventListener('submit', async (e) => {
        e.preventDefault();

        const album = {
            csrf: csrfToken,
            title: document.getElementById('album-title').value,
            url: document.getElementById('album-url').value,
            date: document.getElementById('album-date').value,
            description: document.getElementById('album-description').value,
            tags: currentTags,
            thumbnail: document.getElementById('album-thumbnail').value
        };

        let url = 'api.php?action=album';
        let method = 'POST';

        if (editingAlbumId) {
            album.id = editingAlbumId;
            method = 'PUT';
        }

        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(album)
            });
            const data = await response.json();

            if (data.success) {
                resetForm();
                loadAlbums();
                loadAllTags();
            } else {
                alert(data.error || 'Fehler beim Speichern');
            }
        } catch (err) {
            alert('Verbindungsfehler');
        }
    });

    function editAlbum(album) {
        editingAlbumId = album.id;
        document.getElementById('album-id').value = album.id;
        document.getElementById('album-title').value = album.title;
        document.getElementById('album-url').value = album.url;
        document.getElementById('album-date').value = album.date;
        document.getElementById('album-description').value = album.description || '';
        document.getElementById('album-thumbnail').value = album.thumbnail || '';

        // Tags
        currentTags = [...album.tags];
        renderTags();

        // Thumbnail preview
        if (album.thumbnail) {
            document.getElementById('thumbnail-preview').innerHTML =
                `<img src="${escapeHtml(album.thumbnail)}" class="h-20 rounded">`;
        }

        document.getElementById('form-title').textContent = 'Album bearbeiten';
        document.getElementById('submit-text').textContent = 'Aktualisieren';

        // Scroll to form
        document.getElementById('album-form').scrollIntoView({ behavior: 'smooth' });
    }

    function resetForm() {
        editingAlbumId = null;
        document.getElementById('album-form').reset();
        document.getElementById('album-thumbnail').value = '';
        document.getElementById('thumbnail-preview').innerHTML = '';
        currentTags = [];
        renderTags();
        document.getElementById('form-title').textContent = 'Neues Album hinzufügen';
        document.getElementById('submit-text').textContent = 'Speichern';
    }

    async function deleteAlbum(id) {
        try {
            const response = await fetch('api.php?action=album', {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id, csrf: csrfToken })
            });
            const data = await response.json();

            if (data.success) {
                loadAlbums();
            } else {
                alert(data.error || 'Fehler beim Löschen');
            }
        } catch (err) {
            alert('Verbindungsfehler');
        }
    }

    // === Tags ===
    function renderTags() {
        const container = document.getElementById('tags-container');
        const input = document.getElementById('tag-input');

        // Remove existing tag items
        container.querySelectorAll('.tag-item').forEach(el => el.remove());

        // Add tag items before input
        currentTags.forEach((tag, index) => {
            const span = document.createElement('span');
            span.className = 'tag-item';
            span.innerHTML = `${escapeHtml(tag)}<button type="button" onclick="removeTag(${index})">&times;</button>`;
            container.insertBefore(span, input);
        });
    }

    function removeTag(index) {
        currentTags.splice(index, 1);
        renderTags();
    }

    document.getElementById('tag-input').addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ',') {
            e.preventDefault();
            const value = e.target.value.trim();
            if (value && !currentTags.includes(value)) {
                currentTags.push(value);
                renderTags();
            }
            e.target.value = '';
            document.getElementById('tag-suggestions').classList.add('hidden');
        }
    });

    document.getElementById('tag-input').addEventListener('input', (e) => {
        const value = e.target.value.toLowerCase();
        const suggestions = document.getElementById('tag-suggestions');

        if (value.length < 1) {
            suggestions.classList.add('hidden');
            return;
        }

        const matches = allTags.filter(tag =>
            tag.toLowerCase().includes(value) && !currentTags.includes(tag)
        ).slice(0, 5);

        if (matches.length === 0) {
            suggestions.classList.add('hidden');
            return;
        }

        suggestions.innerHTML = matches.map(tag =>
            `<div onclick="selectTag('${escapeHtml(tag)}')">${escapeHtml(tag)}</div>`
        ).join('');
        suggestions.classList.remove('hidden');
    });

    function selectTag(tag) {
        if (!currentTags.includes(tag)) {
            currentTags.push(tag);
            renderTags();
        }
        document.getElementById('tag-input').value = '';
        document.getElementById('tag-suggestions').classList.add('hidden');
    }

    // === Thumbnail Upload ===
    async function uploadThumbnail() {
        const fileInput = document.getElementById('thumbnail-file');
        if (!fileInput.files[0]) {
            alert('Bitte wähle zuerst ein Bild aus');
            return;
        }

        const formData = new FormData();
        formData.append('thumbnail', fileInput.files[0]);
        formData.append('csrf', csrfToken);

        try {
            const response = await fetch('api.php?action=upload_thumbnail', {
                method: 'POST',
                body: formData
            });
            const data = await response.json();

            if (data.success) {
                document.getElementById('album-thumbnail').value = data.path;
                document.getElementById('thumbnail-preview').innerHTML =
                    `<img src="${escapeHtml(data.path)}" class="h-20 rounded">`;
                fileInput.value = '';
            } else {
                alert(data.error || 'Upload fehlgeschlagen');
            }
        } catch (err) {
            alert('Verbindungsfehler');
        }
    }

    // === Comments ===
    async function loadAllComments() {
        const albumsResponse = await fetch('api.php?action=albums');
        const albumsData = await albumsResponse.json();
        const albums = albumsData.albums || [];

        const commentsContainer = document.getElementById('comments-list');
        commentsContainer.innerHTML = '<p class="text-center"><i class="fas fa-spinner fa-spin"></i> Lade Kommentare...</p>';

        // Kommentare für alle Alben laden
        const allComments = [];
        for (const album of albums) {
            const response = await fetch(`api.php?action=comments&album_id=${album.id}`);
            const data = await response.json();
            (data.comments || []).forEach(comment => {
                comment.albumTitle = album.title;
                allComments.push(comment);
            });
        }

        // Nach Datum sortieren
        allComments.sort((a, b) => b.created_at.localeCompare(a.created_at));

        if (allComments.length === 0) {
            commentsContainer.innerHTML = '<p class="text-center text-gray-500 py-8">Noch keine Kommentare vorhanden</p>';
            return;
        }

        commentsContainer.innerHTML = allComments.map(comment => `
            <div class="bg-gray-50 p-4 rounded-lg">
                <div class="flex justify-between items-start mb-2">
                    <div>
                        <span class="font-semibold">${escapeHtml(comment.author)}</span>
                        <span class="text-gray-400 text-sm ml-2">${formatDateTime(comment.created_at)}</span>
                    </div>
                    <button onclick="confirmDelete('comment', '${comment.id}', 'diesen Kommentar')" class="text-red-600 hover:text-red-800">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
                <p class="text-gray-700 mb-2">${escapeHtml(comment.text)}</p>
                <p class="text-sm text-gray-500">
                    <i class="fas fa-images mr-1"></i>${escapeHtml(comment.albumTitle)}
                </p>
            </div>
        `).join('');
    }

    async function deleteComment(id) {
        try {
            const response = await fetch('api.php?action=comment', {
                method: 'DELETE',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id, csrf: csrfToken })
            });
            const data = await response.json();

            if (data.success) {
                loadAllComments();
            } else {
                alert(data.error || 'Fehler beim Löschen');
            }
        } catch (err) {
            alert('Verbindungsfehler');
        }
    }

    // === Confirm Modal ===
    function confirmDelete(type, id, name) {
        document.getElementById('confirm-message').textContent =
            `Möchtest du "${name}" wirklich löschen?`;

        confirmCallback = () => {
            if (type === 'album') {
                deleteAlbum(id);
            } else if (type === 'comment') {
                deleteComment(id);
            }
        };

        document.getElementById('confirm-modal').classList.remove('hidden');
    }

    function closeConfirm() {
        document.getElementById('confirm-modal').classList.add('hidden');
        confirmCallback = null;
    }

    document.getElementById('confirm-btn').addEventListener('click', () => {
        if (confirmCallback) {
            confirmCallback();
        }
        closeConfirm();
    });

    // === Helpers ===
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatDateTime(isoStr) {
        const date = new Date(isoStr);
        return date.toLocaleDateString('de-CH', {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    // === Init ===
    checkAuth();
    </script>
</body>
</html>
