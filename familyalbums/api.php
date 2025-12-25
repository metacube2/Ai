<?php
/**
 * FamilyAlbums - API Endpunkte
 */

require_once __DIR__ . '/config.php';

session_start();

header('Content-Type: application/json; charset=utf-8');

$method = $_SERVER['REQUEST_METHOD'];
$action = $_GET['action'] ?? '';

// Hilfsfunktion: JSON Response
function json_response(array $data, int $code = 200): void {
    http_response_code($code);
    echo json_encode($data, JSON_UNESCAPED_UNICODE);
    exit;
}

// Hilfsfunktion: Admin-Check
function require_admin(): void {
    if (empty($_SESSION['admin_logged_in'])) {
        json_response(['error' => 'Nicht autorisiert'], 401);
    }
}

// === ALBEN ===

if ($action === 'albums' && $method === 'GET') {
    // Alle Alben abrufen (öffentlich)
    $data = read_json(ALBUMS_FILE);
    $albums = $data['albums'] ?? [];

    // Filter: Jahr
    if (!empty($_GET['year'])) {
        $year = $_GET['year'];
        $albums = array_filter($albums, fn($a) => substr($a['date'], 0, 4) === $year);
    }

    // Filter: Monat
    if (!empty($_GET['month'])) {
        $month = $_GET['month'];
        $albums = array_filter($albums, fn($a) => substr($a['date'], 5, 2) === $month);
    }

    // Filter: Suche
    if (!empty($_GET['search'])) {
        $search = mb_strtolower($_GET['search']);
        $albums = array_filter($albums, function($a) use ($search) {
            $haystack = mb_strtolower($a['title'] . ' ' . $a['description'] . ' ' . implode(' ', $a['tags']));
            return str_contains($haystack, $search);
        });
    }

    // Sortierung
    $sort = $_GET['sort'] ?? 'newest';
    usort($albums, function($a, $b) use ($sort) {
        if ($sort === 'oldest') {
            return strcmp($a['date'], $b['date']);
        }
        return strcmp($b['date'], $a['date']); // newest first
    });

    json_response(['albums' => array_values($albums)]);
}

if ($action === 'album' && $method === 'POST') {
    // Album erstellen (Admin)
    require_admin();

    $input = json_decode(file_get_contents('php://input'), true);

    if (!csrf_validate($input['csrf'] ?? '')) {
        json_response(['error' => 'Ungültiges CSRF-Token'], 403);
    }

    if (empty($input['title']) || empty($input['url']) || empty($input['date'])) {
        json_response(['error' => 'Titel, URL und Datum sind Pflichtfelder'], 400);
    }

    $album = [
        'id' => generate_uuid(),
        'title' => trim($input['title']),
        'url' => trim($input['url']),
        'date' => $input['date'],
        'tags' => array_map('trim', $input['tags'] ?? []),
        'description' => trim($input['description'] ?? ''),
        'thumbnail' => $input['thumbnail'] ?? '',
        'created_at' => date('c')
    ];

    $data = read_json(ALBUMS_FILE);
    $data['albums'][] = $album;
    write_json(ALBUMS_FILE, $data);

    json_response(['success' => true, 'album' => $album]);
}

if ($action === 'album' && $method === 'PUT') {
    // Album bearbeiten (Admin)
    require_admin();

    $input = json_decode(file_get_contents('php://input'), true);

    if (!csrf_validate($input['csrf'] ?? '')) {
        json_response(['error' => 'Ungültiges CSRF-Token'], 403);
    }

    $id = $input['id'] ?? '';

    $data = read_json(ALBUMS_FILE);
    $found = false;

    foreach ($data['albums'] as &$album) {
        if ($album['id'] === $id) {
            $album['title'] = trim($input['title'] ?? $album['title']);
            $album['url'] = trim($input['url'] ?? $album['url']);
            $album['date'] = $input['date'] ?? $album['date'];
            $album['tags'] = array_map('trim', $input['tags'] ?? $album['tags']);
            $album['description'] = trim($input['description'] ?? $album['description']);
            $album['thumbnail'] = $input['thumbnail'] ?? $album['thumbnail'];
            $found = true;
            break;
        }
    }

    if (!$found) {
        json_response(['error' => 'Album nicht gefunden'], 404);
    }

    write_json(ALBUMS_FILE, $data);
    json_response(['success' => true]);
}

if ($action === 'album' && $method === 'DELETE') {
    // Album löschen (Admin)
    require_admin();

    $input = json_decode(file_get_contents('php://input'), true);

    if (!csrf_validate($input['csrf'] ?? '')) {
        json_response(['error' => 'Ungültiges CSRF-Token'], 403);
    }

    $id = $input['id'] ?? '';

    $data = read_json(ALBUMS_FILE);
    $data['albums'] = array_filter($data['albums'], fn($a) => $a['id'] !== $id);
    $data['albums'] = array_values($data['albums']);
    write_json(ALBUMS_FILE, $data);

    // Zugehörige Kommentare löschen
    $comments = read_json(COMMENTS_FILE);
    $comments['comments'] = array_filter($comments['comments'], fn($c) => $c['album_id'] !== $id);
    $comments['comments'] = array_values($comments['comments']);
    write_json(COMMENTS_FILE, $comments);

    json_response(['success' => true]);
}

// === KOMMENTARE ===

if ($action === 'comments' && $method === 'GET') {
    // Kommentare für Album abrufen (öffentlich)
    $album_id = $_GET['album_id'] ?? '';

    $data = read_json(COMMENTS_FILE);
    $comments = array_filter($data['comments'] ?? [], fn($c) => $c['album_id'] === $album_id);

    // Nach Datum sortieren (neueste zuerst)
    usort($comments, fn($a, $b) => strcmp($b['created_at'], $a['created_at']));

    json_response(['comments' => array_values($comments)]);
}

if ($action === 'comment' && $method === 'POST') {
    // Kommentar erstellen (öffentlich)
    $input = json_decode(file_get_contents('php://input'), true);

    if (empty($input['album_id']) || empty($input['author']) || empty($input['text'])) {
        json_response(['error' => 'Album-ID, Name und Text sind Pflichtfelder'], 400);
    }

    // Honeypot-Check (Spam-Schutz)
    if (!empty($input['website'])) {
        json_response(['success' => true]); // Fake-Erfolg für Bots
    }

    // Rate-Limiting: Max 5 Kommentare pro Minute pro IP
    $ip = $_SERVER['REMOTE_ADDR'];
    $rate_file = DATA_PATH . 'rate_' . md5($ip) . '.json';
    $rate_data = read_json($rate_file);
    $now = time();
    $rate_data['times'] = array_filter($rate_data['times'] ?? [], fn($t) => $t > $now - 60);

    if (count($rate_data['times']) >= 5) {
        json_response(['error' => 'Zu viele Kommentare. Bitte warte eine Minute.'], 429);
    }

    $rate_data['times'][] = $now;
    write_json($rate_file, $rate_data);

    $comment = [
        'id' => generate_uuid(),
        'album_id' => $input['album_id'],
        'author' => trim($input['author']),
        'text' => trim($input['text']),
        'created_at' => date('c')
    ];

    $data = read_json(COMMENTS_FILE);
    $data['comments'][] = $comment;
    write_json(COMMENTS_FILE, $data);

    json_response(['success' => true, 'comment' => $comment]);
}

if ($action === 'comment' && $method === 'DELETE') {
    // Kommentar löschen (Admin)
    require_admin();

    $input = json_decode(file_get_contents('php://input'), true);

    if (!csrf_validate($input['csrf'] ?? '')) {
        json_response(['error' => 'Ungültiges CSRF-Token'], 403);
    }

    $id = $input['id'] ?? '';

    $data = read_json(COMMENTS_FILE);
    $data['comments'] = array_filter($data['comments'], fn($c) => $c['id'] !== $id);
    $data['comments'] = array_values($data['comments']);
    write_json(COMMENTS_FILE, $data);

    json_response(['success' => true]);
}

// === TAGS ===

if ($action === 'tags' && $method === 'GET') {
    // Alle verwendeten Tags abrufen (für Vorschläge)
    $data = read_json(ALBUMS_FILE);
    $tags = [];

    foreach ($data['albums'] ?? [] as $album) {
        foreach ($album['tags'] ?? [] as $tag) {
            $tags[$tag] = ($tags[$tag] ?? 0) + 1;
        }
    }

    arsort($tags);
    json_response(['tags' => array_keys($tags)]);
}

// === JAHRE/MONATE ===

if ($action === 'dates' && $method === 'GET') {
    // Verfügbare Jahre und Monate
    $data = read_json(ALBUMS_FILE);
    $years = [];

    foreach ($data['albums'] ?? [] as $album) {
        $year = substr($album['date'], 0, 4);
        $month = substr($album['date'], 5, 2);

        if (!isset($years[$year])) {
            $years[$year] = [];
        }
        if (!in_array($month, $years[$year])) {
            $years[$year][] = $month;
        }
    }

    // Sortieren
    krsort($years);
    foreach ($years as &$months) {
        sort($months);
    }

    json_response(['dates' => $years]);
}

// === AUTH ===

if ($action === 'login' && $method === 'POST') {
    $input = json_decode(file_get_contents('php://input'), true);
    $password = $input['password'] ?? '';

    if (password_verify($password, ADMIN_PASSWORD_HASH)) {
        $_SESSION['admin_logged_in'] = true;
        $_SESSION['csrf_token'] = bin2hex(random_bytes(32));
        json_response(['success' => true, 'csrf' => $_SESSION['csrf_token']]);
    }

    // Verzögerung gegen Brute-Force
    sleep(1);
    json_response(['error' => 'Falsches Passwort'], 401);
}

if ($action === 'logout' && $method === 'POST') {
    session_destroy();
    json_response(['success' => true]);
}

if ($action === 'check_auth' && $method === 'GET') {
    json_response([
        'authenticated' => !empty($_SESSION['admin_logged_in']),
        'csrf' => $_SESSION['csrf_token'] ?? ''
    ]);
}

// === THUMBNAIL UPLOAD ===

if ($action === 'upload_thumbnail' && $method === 'POST') {
    require_admin();

    if (empty($_POST['csrf']) || !csrf_validate($_POST['csrf'])) {
        json_response(['error' => 'Ungültiges CSRF-Token'], 403);
    }

    if (empty($_FILES['thumbnail']) || $_FILES['thumbnail']['error'] !== UPLOAD_ERR_OK) {
        json_response(['error' => 'Kein Bild hochgeladen'], 400);
    }

    $file = $_FILES['thumbnail'];
    $allowed = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    if (!in_array($file['type'], $allowed)) {
        json_response(['error' => 'Nur JPG, PNG, GIF und WebP erlaubt'], 400);
    }

    if ($file['size'] > 5 * 1024 * 1024) {
        json_response(['error' => 'Maximale Dateigrösse: 5MB'], 400);
    }

    $ext = pathinfo($file['name'], PATHINFO_EXTENSION);
    $filename = generate_uuid() . '.' . $ext;
    $path = THUMBNAIL_PATH . $filename;

    if (!move_uploaded_file($file['tmp_name'], $path)) {
        json_response(['error' => 'Upload fehlgeschlagen'], 500);
    }

    json_response(['success' => true, 'path' => THUMBNAIL_URL . $filename]);
}

// Unbekannte Aktion
json_response(['error' => 'Unbekannte Aktion'], 404);
