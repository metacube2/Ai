<?php
// Gym-Logbuch: speichert pro Profil ein JSON-Dokument (Offline-first, Client merged).
// Zugang nur ueber das zufaellige Profil-Token (32 Hex), kein Login.
declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store');
header('X-Content-Type-Options: nosniff');

const DATA_DIR = '/var/lib/gym';
const MAX_BYTES = 2_000_000;
const KEEP_REVISIONS = 30;
const CREATE_PER_HOUR = 20;

function antwort(int $code, array $body): never {
    http_response_code($code);
    echo json_encode($body, JSON_UNESCAPED_UNICODE);
    exit;
}

function db(): PDO {
    $pdo = new PDO('sqlite:' . DATA_DIR . '/gym.db');
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    $pdo->exec('PRAGMA journal_mode=WAL; PRAGMA busy_timeout=3000;');
    $pdo->exec('CREATE TABLE IF NOT EXISTS profil (token TEXT PRIMARY KEY, erstellt INTEGER, updated INTEGER, data TEXT)');
    $pdo->exec('CREATE TABLE IF NOT EXISTS revision (token TEXT, updated INTEGER, data TEXT)');
    $pdo->exec('CREATE INDEX IF NOT EXISTS rev_token ON revision(token, updated)');
    $pdo->exec('CREATE TABLE IF NOT EXISTS anlage (ip TEXT, ts INTEGER)');
    return $pdo;
}

function jetzt_ms(): int {
    return (int) floor(microtime(true) * 1000);
}

function koerper(): string {
    $roh = file_get_contents('php://input', false, null, 0, MAX_BYTES + 1);
    if ($roh === false || strlen($roh) > MAX_BYTES) antwort(413, ['fehler' => 'zu gross']);
    $json = json_decode($roh, true);
    if (!is_array($json) || !isset($json['profile'])) antwort(400, ['fehler' => 'ungueltig']);
    return $roh;
}

function token(): string {
    $k = $_GET['k'] ?? '';
    if (!is_string($k) || !preg_match('/^[a-f0-9]{32}$/', $k)) antwort(400, ['fehler' => 'token']);
    return $k;
}

$a = $_GET['a'] ?? '';
$methode = $_SERVER['REQUEST_METHOD'] ?? 'GET';

try {
    $pdo = db();

    if ($a === 'create' && $methode === 'POST') {
        $daten = koerper();
        $ip = $_SERVER['REMOTE_ADDR'] ?? '';
        $stunde = time() - 3600;
        $pdo->prepare('DELETE FROM anlage WHERE ts < ?')->execute([$stunde]);
        $z = $pdo->prepare('SELECT COUNT(*) FROM anlage WHERE ip = ?');
        $z->execute([$ip]);
        if ((int) $z->fetchColumn() >= CREATE_PER_HOUR) antwort(429, ['fehler' => 'zu viele Profile']);
        $pdo->prepare('INSERT INTO anlage (ip, ts) VALUES (?, ?)')->execute([$ip, time()]);

        $t = bin2hex(random_bytes(16));
        $u = jetzt_ms();
        $pdo->prepare('INSERT INTO profil (token, erstellt, updated, data) VALUES (?, ?, ?, ?)')
            ->execute([$t, $u, $u, $daten]);
        antwort(200, ['token' => $t, 'updated' => $u]);
    }

    if ($a === 'get' && $methode === 'GET') {
        $t = token();
        $s = $pdo->prepare('SELECT updated, data FROM profil WHERE token = ?');
        $s->execute([$t]);
        $r = $s->fetch(PDO::FETCH_ASSOC);
        if (!$r) antwort(404, ['fehler' => 'unbekannt']);
        echo '{"updated":' . (int) $r['updated'] . ',"data":' . $r['data'] . '}';
        exit;
    }

    if ($a === 'put' && $methode === 'POST') {
        $t = token();
        $basis = (int) ($_GET['base'] ?? 0);
        $daten = koerper();
        $pdo->beginTransaction();
        $s = $pdo->prepare('SELECT updated, data FROM profil WHERE token = ?');
        $s->execute([$t]);
        $r = $s->fetch(PDO::FETCH_ASSOC);
        if (!$r) { $pdo->rollBack(); antwort(404, ['fehler' => 'unbekannt']); }
        if ((int) $r['updated'] !== $basis) {
            $pdo->rollBack();
            http_response_code(409);
            echo '{"updated":' . (int) $r['updated'] . ',"data":' . $r['data'] . '}';
            exit;
        }
        $u = max(jetzt_ms(), (int) $r['updated'] + 1);
        $pdo->prepare('INSERT INTO revision (token, updated, data) VALUES (?, ?, ?)')
            ->execute([$t, (int) $r['updated'], $r['data']]);
        $pdo->prepare('DELETE FROM revision WHERE token = ? AND updated NOT IN
            (SELECT updated FROM revision WHERE token = ? ORDER BY updated DESC LIMIT ' . KEEP_REVISIONS . ')')
            ->execute([$t, $t]);
        $pdo->prepare('UPDATE profil SET updated = ?, data = ? WHERE token = ?')->execute([$u, $daten, $t]);
        $pdo->commit();
        antwort(200, ['updated' => $u]);
    }

    antwort(404, ['fehler' => 'aktion']);
} catch (Throwable $e) {
    error_log('gym api: ' . $e->getMessage());
    antwort(500, ['fehler' => 'server']);
}
