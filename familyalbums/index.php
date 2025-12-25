<?php
/**
 * FamilyAlbums - Öffentliche Ansicht
 */

require_once __DIR__ . '/config.php';

$pageTitle = SITE_TITLE;
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
        .album-card:hover { transform: translateY(-4px); }
        .tag { transition: all 0.2s; }
        .tag:hover { transform: scale(1.05); }
        .modal { transition: opacity 0.3s; }
        .modal.hidden { opacity: 0; pointer-events: none; }
        .gradient-placeholder {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
    </style>
</head>
<body class="bg-gray-100 min-h-screen">
    <!-- Header -->
    <header class="bg-gradient-to-r from-blue-600 to-purple-600 text-white shadow-lg">
        <div class="container mx-auto px-4 py-6">
            <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                <h1 class="text-2xl md:text-3xl font-bold">
                    <i class="fas fa-images mr-2"></i><?= e($pageTitle) ?>
                </h1>
                <a href="admin.php" class="text-white/80 hover:text-white text-sm">
                    <i class="fas fa-lock mr-1"></i>Admin
                </a>
            </div>
        </div>
    </header>

    <!-- Filter-Bereich -->
    <div class="bg-white shadow-md sticky top-0 z-10">
        <div class="container mx-auto px-4 py-4">
            <div class="flex flex-col md:flex-row gap-4">
                <!-- Suche -->
                <div class="flex-1">
                    <div class="relative">
                        <input type="text" id="search" placeholder="Album suchen..."
                               class="w-full pl-10 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500">
                        <i class="fas fa-search absolute left-3 top-3 text-gray-400"></i>
                    </div>
                </div>

                <!-- Jahr -->
                <select id="filter-year" class="px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
                    <option value="">Alle Jahre</option>
                </select>

                <!-- Monat -->
                <select id="filter-month" class="px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500" disabled>
                    <option value="">Alle Monate</option>
                </select>

                <!-- Sortierung -->
                <select id="sort" class="px-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
                    <option value="newest">Neueste zuerst</option>
                    <option value="oldest">Älteste zuerst</option>
                </select>
            </div>
        </div>
    </div>

    <!-- Album-Grid -->
    <main class="container mx-auto px-4 py-8">
        <div id="albums-container" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            <!-- Alben werden per JS geladen -->
        </div>

        <div id="no-results" class="hidden text-center py-12 text-gray-500">
            <i class="fas fa-search text-4xl mb-4"></i>
            <p class="text-xl">Keine Alben gefunden</p>
        </div>

        <div id="loading" class="text-center py-12">
            <i class="fas fa-spinner fa-spin text-4xl text-blue-500"></i>
        </div>
    </main>

    <!-- Album-Detail Modal -->
    <div id="album-modal" class="modal hidden fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
        <div class="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div class="p-6">
                <div class="flex justify-between items-start mb-4">
                    <h2 id="modal-title" class="text-2xl font-bold text-gray-800"></h2>
                    <button onclick="closeModal()" class="text-gray-400 hover:text-gray-600">
                        <i class="fas fa-times text-xl"></i>
                    </button>
                </div>

                <div id="modal-thumbnail" class="mb-4 rounded-lg overflow-hidden"></div>

                <p id="modal-date" class="text-gray-500 mb-2"></p>
                <p id="modal-description" class="text-gray-700 mb-4"></p>

                <div id="modal-tags" class="flex flex-wrap gap-2 mb-6"></div>

                <a id="modal-link" href="#" target="_blank"
                   class="inline-block bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition mb-6">
                    <i class="fas fa-external-link-alt mr-2"></i>Album öffnen
                </a>

                <!-- Kommentare -->
                <div class="border-t pt-6">
                    <h3 class="text-lg font-semibold mb-4">
                        <i class="fas fa-comments mr-2"></i>Kommentare
                    </h3>

                    <div id="comments-list" class="space-y-4 mb-6"></div>

                    <!-- Kommentar-Formular -->
                    <form id="comment-form" class="bg-gray-50 p-4 rounded-lg">
                        <input type="hidden" id="comment-album-id">
                        <!-- Honeypot -->
                        <input type="text" name="website" id="comment-website" class="hidden" tabindex="-1" autocomplete="off">

                        <div class="mb-3">
                            <input type="text" id="comment-author" placeholder="Dein Name" required
                                   class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500">
                        </div>
                        <div class="mb-3">
                            <textarea id="comment-text" placeholder="Dein Kommentar..." required rows="3"
                                      class="w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500"></textarea>
                        </div>
                        <button type="submit" class="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition">
                            <i class="fas fa-paper-plane mr-2"></i>Absenden
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>

    <!-- Footer -->
    <footer class="bg-gray-800 text-white py-6 mt-12">
        <div class="container mx-auto px-4 text-center">
            <p>&copy; <?= date('Y') ?> <?= e($pageTitle) ?></p>
        </div>
    </footer>

    <script>
    // === State ===
    let allDates = {};
    let currentAlbumId = null;
    let debounceTimer = null;

    // === Monatsnamen ===
    const monthNames = {
        '01': 'Januar', '02': 'Februar', '03': 'März', '04': 'April',
        '05': 'Mai', '06': 'Juni', '07': 'Juli', '08': 'August',
        '09': 'September', '10': 'Oktober', '11': 'November', '12': 'Dezember'
    };

    // === Helpers ===
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatDate(dateStr) {
        const [year, month, day] = dateStr.split('-');
        return `${parseInt(day)}. ${monthNames[month]} ${year}`;
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

    // === API Calls ===
    async function fetchAlbums() {
        const params = new URLSearchParams();

        const year = document.getElementById('filter-year').value;
        const month = document.getElementById('filter-month').value;
        const search = document.getElementById('search').value;
        const sort = document.getElementById('sort').value;

        if (year) params.append('year', year);
        if (month) params.append('month', month);
        if (search) params.append('search', search);
        params.append('sort', sort);

        const response = await fetch(`api.php?action=albums&${params}`);
        return response.json();
    }

    async function fetchDates() {
        const response = await fetch('api.php?action=dates');
        return response.json();
    }

    async function fetchComments(albumId) {
        const response = await fetch(`api.php?action=comments&album_id=${encodeURIComponent(albumId)}`);
        return response.json();
    }

    async function postComment(albumId, author, text, website) {
        const response = await fetch('api.php?action=comment', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ album_id: albumId, author, text, website })
        });
        return response.json();
    }

    // === Rendering ===
    function renderAlbums(albums) {
        const container = document.getElementById('albums-container');
        const noResults = document.getElementById('no-results');
        const loading = document.getElementById('loading');

        loading.classList.add('hidden');

        if (albums.length === 0) {
            container.innerHTML = '';
            noResults.classList.remove('hidden');
            return;
        }

        noResults.classList.add('hidden');

        container.innerHTML = albums.map(album => `
            <div class="album-card bg-white rounded-xl shadow-md overflow-hidden cursor-pointer transition-all duration-300 hover:shadow-xl"
                 data-album='${JSON.stringify(album).replace(/'/g, "&#39;")}'
                 onclick="openModalFromCard(this)">
                <div class="aspect-video gradient-placeholder flex items-center justify-center">
                    ${album.thumbnail
                        ? `<img src="${escapeHtml(album.thumbnail)}" alt="${escapeHtml(album.title)}" class="w-full h-full object-cover" onerror="this.parentElement.innerHTML='<i class=\\'fas fa-images text-4xl text-white/50\\'></i>'">`
                        : `<i class="fas fa-images text-4xl text-white/50"></i>`
                    }
                </div>
                <div class="p-4">
                    <h3 class="font-semibold text-lg text-gray-800 mb-1 line-clamp-2">${escapeHtml(album.title)}</h3>
                    <p class="text-gray-500 text-sm mb-3">
                        <i class="fas fa-calendar mr-1"></i>${formatDate(album.date)}
                    </p>
                    <div class="flex flex-wrap gap-1">
                        ${album.tags.slice(0, 3).map(tag => `
                            <span class="tag bg-blue-100 text-blue-700 text-xs px-2 py-1 rounded-full">${escapeHtml(tag)}</span>
                        `).join('')}
                        ${album.tags.length > 3 ? `<span class="text-gray-400 text-xs">+${album.tags.length - 3}</span>` : ''}
                    </div>
                </div>
            </div>
        `).join('');
    }

    function renderDateFilters(dates) {
        allDates = dates;
        const yearSelect = document.getElementById('filter-year');

        yearSelect.innerHTML = '<option value="">Alle Jahre</option>' +
            Object.keys(dates).map(year => `<option value="${year}">${year}</option>`).join('');
    }

    function updateMonthFilter() {
        const year = document.getElementById('filter-year').value;
        const monthSelect = document.getElementById('filter-month');

        if (!year || !allDates[year]) {
            monthSelect.innerHTML = '<option value="">Alle Monate</option>';
            monthSelect.disabled = true;
            return;
        }

        monthSelect.disabled = false;
        monthSelect.innerHTML = '<option value="">Alle Monate</option>' +
            allDates[year].map(month => `<option value="${month}">${monthNames[month]}</option>`).join('');
    }

    function renderComments(comments) {
        const container = document.getElementById('comments-list');

        if (comments.length === 0) {
            container.innerHTML = '<p class="text-gray-500 text-center italic">Noch keine Kommentare. Sei der Erste!</p>';
            return;
        }

        container.innerHTML = comments.map(comment => `
            <div class="bg-white p-3 rounded-lg border">
                <div class="flex justify-between items-start mb-1">
                    <span class="font-semibold text-gray-800">${escapeHtml(comment.author)}</span>
                    <span class="text-gray-400 text-xs">${formatDateTime(comment.created_at)}</span>
                </div>
                <p class="text-gray-700">${escapeHtml(comment.text)}</p>
            </div>
        `).join('');
    }

    // === Modal ===
    function openModalFromCard(element) {
        const album = JSON.parse(element.dataset.album);
        openModal(album.id, album);
    }

    function openModal(id, album) {
        currentAlbumId = id;

        document.getElementById('modal-title').textContent = album.title;
        document.getElementById('modal-date').innerHTML = `<i class="fas fa-calendar mr-1"></i>${formatDate(album.date)}`;
        document.getElementById('modal-description').textContent = album.description || 'Keine Beschreibung';
        document.getElementById('modal-link').href = album.url;
        document.getElementById('comment-album-id').value = id;

        // Thumbnail
        const thumbnailContainer = document.getElementById('modal-thumbnail');
        if (album.thumbnail) {
            thumbnailContainer.innerHTML = `<img src="${escapeHtml(album.thumbnail)}" alt="${escapeHtml(album.title)}" class="w-full max-h-64 object-cover">`;
        } else {
            thumbnailContainer.innerHTML = '';
        }

        // Tags
        document.getElementById('modal-tags').innerHTML = album.tags.map(tag =>
            `<span class="bg-blue-100 text-blue-700 text-sm px-3 py-1 rounded-full">${escapeHtml(tag)}</span>`
        ).join('');

        // Modal anzeigen
        document.getElementById('album-modal').classList.remove('hidden');
        document.body.style.overflow = 'hidden';

        // Kommentare laden
        loadComments(id);
    }

    function closeModal() {
        document.getElementById('album-modal').classList.add('hidden');
        document.body.style.overflow = '';
        currentAlbumId = null;
    }

    async function loadComments(albumId) {
        document.getElementById('comments-list').innerHTML = '<p class="text-center"><i class="fas fa-spinner fa-spin"></i></p>';
        const data = await fetchComments(albumId);
        renderComments(data.comments || []);
    }

    // === Event Listeners ===
    document.getElementById('search').addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(async () => {
            const data = await fetchAlbums();
            renderAlbums(data.albums || []);
        }, 300);
    });

    document.getElementById('filter-year').addEventListener('change', async () => {
        updateMonthFilter();
        document.getElementById('filter-month').value = '';
        const data = await fetchAlbums();
        renderAlbums(data.albums || []);
    });

    document.getElementById('filter-month').addEventListener('change', async () => {
        const data = await fetchAlbums();
        renderAlbums(data.albums || []);
    });

    document.getElementById('sort').addEventListener('change', async () => {
        const data = await fetchAlbums();
        renderAlbums(data.albums || []);
    });

    document.getElementById('comment-form').addEventListener('submit', async (e) => {
        e.preventDefault();

        const albumId = document.getElementById('comment-album-id').value;
        const author = document.getElementById('comment-author').value.trim();
        const text = document.getElementById('comment-text').value.trim();
        const website = document.getElementById('comment-website').value;

        if (!author || !text) return;

        const btn = e.target.querySelector('button[type="submit"]');
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin mr-2"></i>Senden...';

        try {
            const result = await postComment(albumId, author, text, website);

            if (result.error) {
                alert(result.error);
            } else {
                document.getElementById('comment-text').value = '';
                await loadComments(albumId);
            }
        } catch (err) {
            alert('Fehler beim Senden des Kommentars');
        }

        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-paper-plane mr-2"></i>Absenden';
    });

    // Modal schliessen bei Klick ausserhalb
    document.getElementById('album-modal').addEventListener('click', (e) => {
        if (e.target.id === 'album-modal') {
            closeModal();
        }
    });

    // Modal schliessen mit Escape
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            closeModal();
        }
    });

    // === Init ===
    async function init() {
        try {
            const [albumsData, datesData] = await Promise.all([
                fetchAlbums(),
                fetchDates()
            ]);

            renderAlbums(albumsData.albums || []);
            renderDateFilters(datesData.dates || {});
        } catch (err) {
            console.error('Fehler beim Laden:', err);
            document.getElementById('loading').innerHTML =
                '<p class="text-red-500"><i class="fas fa-exclamation-triangle mr-2"></i>Fehler beim Laden der Alben</p>';
        }
    }

    init();
    </script>
</body>
</html>
