<?php
// bier.ingok.ch – Funkerbier-Terminsuche, JSON-API. Quelle Ai/funkerbier/ (2026-10-15).
//
// GET  ?a=runde&id=..[&admin=..][&ich=..]   Runde mit Terminen und Eintraegen
// GET  ?a=ics&id=..                          Kalenderdatei des festgelegten Termins
// POST {a:"anlegen"|"eintragen"|"final"|"loeschen"|"link", ...}
//
// Kein Login: Die Runde hat eine oeffentliche ID, der Organisator einen geheimen
// Admin-Token, jeder Eintrag einen eigenen Bearbeiten-Token (kommen per Mail).
// Mailadressen sieht nur der Admin. Mail ueber Gmail (Zugang /etc/bier/mail.json).
declare(strict_types=1);

const DB_PFAD = '/var/lib/bier/bier.db';
const MAIL_ZUGANG = '/etc/bier/mail.json';
const AUTOLOAD = '/opt/bier/vendor/autoload.php';
const BASIS = 'https://bier.ingok.ch';
const ZONE = 'Europe/Zurich';
const NPM = '192.168.178.111';
const MAX_TERMINE = 30;
const MAX_FUNKER = 200;

header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store');
header('X-Content-Type-Options: nosniff');

function antwort(array $daten, int $code = 200): never
{
    http_response_code($code);
    echo json_encode($daten, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function fehler(string $text, int $code = 400): never
{
    antwort(['error' => $text], $code);
}

function db(): PDO
{
    static $db = null;
    if ($db) {
        return $db;
    }
    $db = new PDO('sqlite:' . DB_PFAD, null, null, [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);
    $db->exec('PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=4000;');
    $db->exec(<<<SQL
        CREATE TABLE IF NOT EXISTS runde (
            id TEXT PRIMARY KEY, admin TEXT NOT NULL, titel TEXT NOT NULL, ort TEXT NOT NULL DEFAULT '',
            notiz TEXT NOT NULL DEFAULT '', wer TEXT NOT NULL, mail TEXT NOT NULL,
            dauer INTEGER NOT NULL DEFAULT 180, final INTEGER, final_mail INTEGER NOT NULL DEFAULT 0,
            erstellt INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS termin (
            id INTEGER PRIMARY KEY, runde TEXT NOT NULL REFERENCES runde(id) ON DELETE CASCADE,
            beginn TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS funker (
            id INTEGER PRIMARY KEY, runde TEXT NOT NULL REFERENCES runde(id) ON DELETE CASCADE,
            name TEXT NOT NULL, rufzeichen TEXT NOT NULL DEFAULT '', mail TEXT NOT NULL,
            token TEXT NOT NULL UNIQUE, erstellt INTEGER NOT NULL, geaendert INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS stimme (
            funker INTEGER NOT NULL REFERENCES funker(id) ON DELETE CASCADE,
            termin INTEGER NOT NULL REFERENCES termin(id) ON DELETE CASCADE,
            wert INTEGER NOT NULL, PRIMARY KEY (funker, termin));
        CREATE TABLE IF NOT EXISTS zugriff (ip TEXT NOT NULL, art TEXT NOT NULL, t INTEGER NOT NULL);
        CREATE INDEX IF NOT EXISTS zugriff_ip ON zugriff (ip, art, t);
        SQL);
    return $db;
}

function client_ip(): string
{
    $ip = $_SERVER['REMOTE_ADDR'] ?? '0.0.0.0';
    $echt = $_SERVER['HTTP_X_REAL_IP'] ?? '';
    if ($ip === NPM && filter_var($echt, FILTER_VALIDATE_IP)) {
        return $echt;
    }
    return $ip;
}

// Bremse gegen Missbrauch als Mailschleuder: hoechstens $max Vorgaenge je Stunde und IP.
function bremse(string $art, int $max): void
{
    $db = db();
    $jetzt = time();
    if (random_int(0, 50) === 0) {
        $db->prepare('DELETE FROM zugriff WHERE t < ?')->execute([$jetzt - 86400]);
    }
    $q = $db->prepare('SELECT count(*) FROM zugriff WHERE ip = ? AND art = ? AND t > ?');
    $q->execute([client_ip(), $art, $jetzt - 3600]);
    if ((int) $q->fetchColumn() >= $max) {
        fehler('QRX – zu viele Anfragen von deiner Station. Bitte in einer Stunde nochmals.', 429);
    }
    $db->prepare('INSERT INTO zugriff (ip, art, t) VALUES (?, ?, ?)')->execute([client_ip(), $art, $jetzt]);
}

function token(int $bytes): string
{
    return bin2hex(random_bytes($bytes));
}

function runden_id(): string
{
    $zeichen = 'abcdefghjkmnpqrstuvwxyz23456789';
    $id = '';
    for ($i = 0; $i < 8; $i++) {
        $id .= $zeichen[random_int(0, strlen($zeichen) - 1)];
    }
    return $id;
}

function text(mixed $wert, int $max, bool $pflicht = false, string $feld = 'Feld'): string
{
    $s = is_string($wert) ? trim(preg_replace('/[\x00-\x09\x0B-\x1F\x7F]/u', '', $wert) ?? '') : '';
    if ($pflicht && $s === '') {
        fehler("$feld fehlt.");
    }
    if (mb_strlen($s) > $max) {
        fehler("$feld ist zu lang (höchstens $max Zeichen).");
    }
    return $s;
}

function zeile(string $s): string
{
    return trim(preg_replace('/\s+/u', ' ', $s) ?? '');
}

function mailadresse(mixed $wert): string
{
    $m = is_string($wert) ? trim($wert) : '';
    if ($m === '' || strlen($m) > 120 || preg_match('/[\r\n]/', $m) || !filter_var($m, FILTER_VALIDATE_EMAIL)) {
        fehler('Die Mailadresse stimmt nicht.');
    }
    return $m;
}

function rufzeichen(mixed $wert): string
{
    $r = strtoupper(text($wert, 12, false, 'Rufzeichen'));
    if ($r !== '' && !preg_match('#^[A-Z0-9/]{3,12}$#', $r)) {
        fehler('Das Rufzeichen darf nur A–Z, 0–9 und / enthalten.');
    }
    return $r;
}

function datum_de(string $beginn, bool $lang = true): string
{
    $d = new DateTimeImmutable($beginn, new DateTimeZone(ZONE));
    $tage = $lang ? ['Sonntag', 'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag']
        : ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
    return $tage[(int) $d->format('w')] . ', ' . $d->format('d.m.Y, H:i') . ' Uhr';
}

function runde_holen(string $id): array
{
    $q = db()->prepare('SELECT * FROM runde WHERE id = ?');
    $q->execute([$id]);
    $r = $q->fetch();
    if (!$r) {
        fehler('Diese Runde gibt es nicht (mehr).', 404);
    }
    return $r;
}

function ist_admin(array $runde, mixed $token): bool
{
    return is_string($token) && $token !== '' && hash_equals($runde['admin'], $token);
}

function termine(string $id): array
{
    $q = db()->prepare('SELECT id, beginn FROM termin WHERE runde = ? ORDER BY beginn');
    $q->execute([$id]);
    return array_map(fn($t) => ['id' => (int) $t['id'], 'beginn' => $t['beginn']], $q->fetchAll());
}

function funker_liste(string $id): array
{
    $q = db()->prepare('SELECT * FROM funker WHERE runde = ? ORDER BY erstellt, id');
    $q->execute([$id]);
    $liste = $q->fetchAll();
    $s = db()->prepare('SELECT s.funker, s.termin, s.wert FROM stimme s JOIN funker f ON f.id = s.funker WHERE f.runde = ?');
    $s->execute([$id]);
    $stimmen = [];
    foreach ($s->fetchAll() as $z) {
        $stimmen[(int) $z['funker']][(int) $z['termin']] = (int) $z['wert'];
    }
    foreach ($liste as &$f) {
        $f['id'] = (int) $f['id'];
        $f['stimmen'] = (object) ($stimmen[$f['id']] ?? []);
    }
    return $liste;
}

// --- Mail --------------------------------------------------------------------

function mailer(): ?PHPMailer\PHPMailer\PHPMailer
{
    static $m = false;
    if ($m !== false) {
        return $m;
    }
    $m = null;
    if (!is_readable(MAIL_ZUGANG) || !is_readable(AUTOLOAD)) {
        error_log('bier: Mailzugang oder PHPMailer fehlt');
        return null;
    }
    require_once AUTOLOAD;
    $z = json_decode((string) file_get_contents(MAIL_ZUGANG), true);
    if (empty($z['user']) || empty($z['app_password'])) {
        error_log('bier: mail.json unvollstaendig');
        return null;
    }
    $m = new PHPMailer\PHPMailer\PHPMailer(true);
    $m->isSMTP();
    $m->Host = 'smtp.gmail.com';
    $m->SMTPAuth = true;
    $m->Username = $z['user'];
    $m->Password = $z['app_password'];
    $m->SMTPSecure = PHPMailer\PHPMailer\PHPMailer::ENCRYPTION_STARTTLS;
    $m->Port = 587;
    $m->Timeout = 15;
    $m->SMTPKeepAlive = true;
    $m->CharSet = 'UTF-8';
    $m->setFrom($z['user'], 'Funkerbier QRV');
    return $m;
}

function senden(string $an, string $betreff, string $text, ?string $antwort_an = null, ?string $ics = null): bool
{
    if (str_ends_with(strtolower($an), '.invalid')) {
        return true; // reservierte Testdomain (RFC 2606), nie zustellbar: nicht senden
    }
    $m = mailer();
    if (!$m) {
        return false;
    }
    try {
        $m->clearAllRecipients();
        $m->clearReplyTos();
        $m->clearAttachments();
        $m->addAddress($an);
        if ($antwort_an) {
            $m->addReplyTo($antwort_an);
        }
        $m->Subject = zeile($betreff);
        $m->Body = $text . "\n\n73 de bier.ingok.ch\n";
        if ($ics) {
            $m->addStringAttachment($ics, 'funkerbier.ics', 'base64', 'text/calendar; charset=utf-8; method=PUBLISH');
        }
        $m->send();
        return true;
    } catch (Throwable $e) {
        error_log('bier: Mail an ' . substr(hash('sha256', $an), 0, 8) . ' fehlgeschlagen: ' . $e->getMessage());
        return false;
    }
}

function qsl(string $titel, array $zeilen): string
{
    $breite = 46;
    $rand = '+' . str_repeat('-', $breite) . '+';
    $kopf = 'QSL  *  ' . mb_strtoupper(mb_substr($titel, 0, 34));
    $out = [$rand, '| ' . $kopf . str_repeat(' ', max(0, $breite - 1 - mb_strlen($kopf))) . '|', $rand];
    foreach (array_filter($zeilen, fn($z) => $z !== '') as $z) {
        // Umbruch nach Zeichen, nicht Bytes (Umlaute)
        $teil = '';
        foreach (preg_split('/\s+/u', $z) as $wort) {
            if ($teil !== '' && mb_strlen("$teil $wort") > $breite - 2) {
                $out[] = '| ' . $teil . str_repeat(' ', max(0, $breite - 1 - mb_strlen($teil))) . '|';
                $teil = $wort;
            } else {
                $teil = $teil === '' ? $wort : "$teil $wort";
            }
        }
        $out[] = '| ' . $teil . str_repeat(' ', max(0, $breite - 1 - mb_strlen($teil))) . '|';
    }
    $out[] = $rand;
    return implode("\n", $out);
}

function ics(array $runde, string $beginn): string
{
    $zone = new DateTimeZone(ZONE);
    $utc = new DateTimeZone('UTC');
    $start = (new DateTimeImmutable($beginn, $zone))->setTimezone($utc);
    $ende = $start->modify('+' . (int) $runde['dauer'] . ' minutes');
    $esc = fn(string $s) => str_replace(["\\", ';', ',', "\r\n", "\n"], ["\\\\", '\;', '\,', '\n', '\n'], $s);
    $link = BASIS . '/#/r/' . $runde['id'];
    $zeilen = [
        'BEGIN:VCALENDAR', 'VERSION:2.0', 'PRODID:-//bier.ingok.ch//Funkerbier//DE', 'METHOD:PUBLISH',
        'BEGIN:VEVENT',
        'UID:' . $runde['id'] . '@bier.ingok.ch',
        'DTSTAMP:' . gmdate('Ymd\THis\Z'),
        'DTSTART:' . $start->format('Ymd\THis\Z'),
        'DTEND:' . $ende->format('Ymd\THis\Z'),
        'SUMMARY:' . $esc($runde['titel']),
        'LOCATION:' . $esc($runde['ort']),
        'DESCRIPTION:' . $esc(trim($runde['notiz'] . "\n\n" . $link)),
        'URL:' . $link,
        'END:VEVENT', 'END:VCALENDAR',
    ];
    return implode("\r\n", $zeilen) . "\r\n";
}

// --- Aktionen ----------------------------------------------------------------

function a_runde(array $p): never
{
    $runde = runde_holen((string) ($p['id'] ?? ''));
    $admin = ist_admin($runde, $p['admin'] ?? null);
    $ich = null;
    $funker = funker_liste($runde['id']);
    $aus = [];
    foreach ($funker as $f) {
        if (is_string($p['ich'] ?? null) && $p['ich'] !== '' && hash_equals($f['token'], $p['ich'])) {
            $ich = ['id' => $f['id'], 'name' => $f['name'], 'rufzeichen' => $f['rufzeichen'],
                'mail' => $f['mail'], 'stimmen' => $f['stimmen']];
        }
        $eintrag = ['id' => $f['id'], 'name' => $f['name'], 'rufzeichen' => $f['rufzeichen'], 'stimmen' => $f['stimmen']];
        if ($admin) {
            $eintrag['mail'] = $f['mail'];
        }
        $aus[] = $eintrag;
    }
    antwort([
        'id' => $runde['id'], 'titel' => $runde['titel'], 'ort' => $runde['ort'], 'notiz' => $runde['notiz'],
        'wer' => $runde['wer'], 'dauer' => (int) $runde['dauer'],
        'final' => $runde['final'] !== null ? (int) $runde['final'] : null,
        'termine' => termine($runde['id']), 'funker' => $aus, 'admin' => $admin, 'ich' => $ich,
        'mail_admin' => $admin ? $runde['mail'] : null,
    ]);
}

function a_anlegen(array $p): never
{
    if (!empty($p['website'])) {
        antwort(['id' => 'qrt', 'admin' => 'qrt', 'mail' => true]); // Honigtopf: Bots bekommen ein falsches Okay
    }
    $titel = zeile(text($p['titel'] ?? '', 80, true, 'Titel'));
    $ort = zeile(text($p['ort'] ?? '', 120, false, 'Ort'));
    $notiz = text($p['notiz'] ?? '', 1000, false, 'Notiz');
    $wer = zeile(text($p['wer'] ?? '', 40, true, 'Dein Name'));
    $mail = mailadresse($p['mail'] ?? '');
    $dauer = max(30, min(720, (int) ($p['dauer'] ?? 180)));
    $termine = [];
    foreach ((array) ($p['termine'] ?? []) as $t) {
        $d = is_string($t) ? DateTimeImmutable::createFromFormat('!Y-m-d\TH:i', $t, new DateTimeZone(ZONE)) : false;
        if (!$d || $d->format('Y-m-d\TH:i') !== $t) {
            fehler('Ein Termin hat ein ungültiges Format.');
        }
        $termine[$t] = true;
    }
    $termine = array_keys($termine);
    sort($termine);
    if (!$termine) {
        fehler('Mindestens einen Termin vorschlagen.');
    }
    if (count($termine) > MAX_TERMINE) {
        fehler('Höchstens ' . MAX_TERMINE . ' Termine.');
    }
    bremse('anlegen', 6);

    $db = db();
    $id = runden_id();
    $admin = token(16);
    $db->beginTransaction();
    $db->prepare('INSERT INTO runde (id, admin, titel, ort, notiz, wer, mail, dauer, erstellt) VALUES (?,?,?,?,?,?,?,?,?)')
        ->execute([$id, $admin, $titel, $ort, $notiz, $wer, $mail, $dauer, time()]);
    $q = $db->prepare('INSERT INTO termin (runde, beginn) VALUES (?, ?)');
    foreach ($termine as $t) {
        $q->execute([$id, $t]);
    }
    $db->commit();

    $liste = implode("\n", array_map(fn($t) => '  - ' . datum_de($t), $termine));
    $ok = senden($mail, "Funkerbier: „{$titel}“ ist ausgerufen", qsl($titel, [
        "CQ CQ, $wer!",
        'Deine Runde steht. Diesen Link schickst du an alle:',
    ]) . "\n\nLink für alle:\n" . BASIS . "/#/r/$id\n\n"
        . "Dein Admin-Link (geheim halten, damit legst du den Termin fest):\n" . BASIS . "/#/r/$id/admin/$admin\n\n"
        . "Vorgeschlagene Termine:\n$liste");
    antwort(['id' => $id, 'admin' => $admin, 'mail' => $ok]);
}

function stimmen_lesen(mixed $roh, array $termine): array
{
    $gueltig = array_column($termine, 'id');
    $s = [];
    foreach ((array) $roh as $tid => $wert) {
        $tid = (int) $tid;
        $wert = (int) $wert;
        if (in_array($tid, $gueltig, true) && in_array($wert, [0, 1, 2], true)) {
            $s[$tid] = $wert;
        }
    }
    return $s;
}

function a_eintragen(array $p): never
{
    if (!empty($p['website'])) {
        antwort(['token' => 'qrt', 'mail' => true]);
    }
    $runde = runde_holen((string) ($p['id'] ?? ''));
    $name = zeile(text($p['name'] ?? '', 40, true, 'Name'));
    $ruf = rufzeichen($p['rufzeichen'] ?? '');
    $mail = mailadresse($p['mail'] ?? '');
    $termine = termine($runde['id']);
    $stimmen = stimmen_lesen($p['stimmen'] ?? [], $termine);
    $db = db();
    $jetzt = time();

    $token = is_string($p['token'] ?? null) ? $p['token'] : '';
    $vorhanden = null;
    if ($token !== '') {
        $q = $db->prepare('SELECT * FROM funker WHERE runde = ? AND token = ?');
        $q->execute([$runde['id'], $token]);
        $vorhanden = $q->fetch() ?: null;
        if (!$vorhanden) {
            fehler('Dein Bearbeiten-Link passt nicht zu dieser Runde.', 403);
        }
    }

    if ($vorhanden) {
        bremse('aendern', 60);
        $fid = (int) $vorhanden['id'];
        $db->beginTransaction();
        $db->prepare('UPDATE funker SET name = ?, rufzeichen = ?, mail = ?, geaendert = ? WHERE id = ?')
            ->execute([$name, $ruf, $mail, $jetzt, $fid]);
    } else {
        $q = $db->prepare('SELECT count(*) AS n, sum(lower(mail) = lower(?)) AS gleich FROM funker WHERE runde = ?');
        $q->execute([$mail, $runde['id']]);
        $stand = $q->fetch();
        if ((int) $stand['gleich'] > 0) {
            fehler('Diese Mailadresse ist schon im Log. Den Bearbeiten-Link schicke ich dir gern nochmals.', 409);
        }
        if ((int) $stand['n'] >= MAX_FUNKER) {
            fehler('Die Runde ist voll.');
        }
        bremse('eintragen', 20);
        $token = token(16);
        $db->beginTransaction();
        $db->prepare('INSERT INTO funker (runde, name, rufzeichen, mail, token, erstellt, geaendert) VALUES (?,?,?,?,?,?,?)')
            ->execute([$runde['id'], $name, $ruf, $mail, $token, $jetzt, $jetzt]);
        $fid = (int) $db->lastInsertId();
    }
    $db->prepare('DELETE FROM stimme WHERE funker = ?')->execute([$fid]);
    $q = $db->prepare('INSERT INTO stimme (funker, termin, wert) VALUES (?, ?, ?)');
    foreach ($stimmen as $tid => $wert) {
        $q->execute([$fid, $tid, $wert]);
    }
    $db->commit();

    $ok = true;
    if (!$vorhanden) {
        $zeichen = [0 => 'nein', 1 => 'vielleicht', 2 => 'JA'];
        $liste = implode("\n", array_map(fn($t) => '  ' . str_pad($zeichen[$stimmen[$t['id']] ?? 0], 10) . datum_de($t['beginn'], false), $termine));
        $wer = $ruf !== '' ? "$name ($ruf)" : $name;
        $ok = senden($mail, "Funkerbier: Eintrag für „{$runde['titel']}“ bestätigt", qsl($runde['titel'], [
            "TNX QSO, $wer!",
            "Organisiert von {$runde['wer']}" . ($runde['ort'] !== '' ? ", {$runde['ort']}" : '') . '.',
        ]) . "\n\nDein Logbuch-Eintrag:\n$liste\n\n"
            . "Ändern kannst du ihn jederzeit hier:\n" . BASIS . "/#/r/{$runde['id']}/ich/$token\n\n"
            . 'Sobald der Termin feststeht, bekommst du eine Mail mit Kalendereintrag.', $runde['mail']);
        $ja = count(array_filter($stimmen, fn($w) => $w === 2));
        senden($runde['mail'], "Funkerbier: $wer ist QRV", "$wer hat sich bei „{$runde['titel']}“ eingetragen ({$ja}× ja).\n\n"
            . 'Stand und Termin festlegen: ' . BASIS . "/#/r/{$runde['id']}/admin/{$runde['admin']}", $mail);
    }
    antwort(['token' => $token, 'mail' => $ok, 'neu' => !$vorhanden]);
}

function a_final(array $p): never
{
    $runde = runde_holen((string) ($p['id'] ?? ''));
    if (!ist_admin($runde, $p['admin'] ?? null)) {
        fehler('Nur der Organisator kann den Termin festlegen.', 403);
    }
    $tid = isset($p['termin']) && $p['termin'] !== null ? (int) $p['termin'] : null;
    $db = db();
    if ($tid === null) {
        $db->prepare('UPDATE runde SET final = NULL WHERE id = ?')->execute([$runde['id']]);
        antwort(['final' => null, 'gesendet' => 0]);
    }
    $termin = null;
    foreach (termine($runde['id']) as $t) {
        if ($t['id'] === $tid) {
            $termin = $t;
        }
    }
    if (!$termin) {
        fehler('Diesen Termin gibt es in der Runde nicht.');
    }
    $db->prepare('UPDATE runde SET final = ? WHERE id = ?')->execute([$tid, $runde['id']]);

    // Rundmail hoechstens alle 10 Minuten, damit Hin- und Herklicken niemanden zuspammt
    $gesendet = 0;
    $gebremst = time() - (int) $runde['final_mail'] < 600;
    if (!$gebremst && ($p['mail'] ?? true)) {
        $db->prepare('UPDATE runde SET final_mail = ? WHERE id = ?')->execute([time(), $runde['id']]);
        $kalender = ics($runde, $termin['beginn']);
        $wann = datum_de($termin['beginn']);
        foreach (funker_liste($runde['id']) as $f) {
            $text = qsl($runde['titel'], [
                'JACKPOT! Der Termin steht:',
                $wann,
                $runde['ort'] !== '' ? "QTH: {$runde['ort']}" : '',
            ]) . "\n\n" . ($runde['notiz'] !== '' ? $runde['notiz'] . "\n\n" : '')
                . 'Die Kalenderdatei hängt an. Übersicht: ' . BASIS . "/#/r/{$runde['id']}/ich/{$f['token']}";
            if (senden($f['mail'], "Funkerbier: {$runde['titel']} – $wann", $text, $runde['mail'], $kalender)) {
                $gesendet++;
            }
        }
    }
    antwort(['final' => $tid, 'gesendet' => $gesendet, 'gebremst' => $gebremst]);
}

function a_loeschen(array $p): never
{
    $runde = runde_holen((string) ($p['id'] ?? ''));
    if (!ist_admin($runde, $p['admin'] ?? null)) {
        fehler('Nur der Organisator kann Einträge löschen.', 403);
    }
    db()->prepare('DELETE FROM funker WHERE id = ? AND runde = ?')->execute([(int) ($p['funker'] ?? 0), $runde['id']]);
    antwort(['ok' => true]);
}

function a_link(array $p): never
{
    $runde = runde_holen((string) ($p['id'] ?? ''));
    $mail = mailadresse($p['mail'] ?? '');
    bremse('link', 5);
    $q = db()->prepare('SELECT token, name FROM funker WHERE runde = ? AND lower(mail) = lower(?)');
    $q->execute([$runde['id'], $mail]);
    if ($f = $q->fetch()) {
        senden($mail, "Funkerbier: dein Link für „{$runde['titel']}“",
            "Hallo {$f['name']},\n\nhier ist dein Bearbeiten-Link:\n" . BASIS . "/#/r/{$runde['id']}/ich/{$f['token']}");
    }
    antwort(['ok' => true]); // gleiche Antwort ohne Treffer: verraet nicht, wer eingetragen ist
}

function a_ics(array $p): never
{
    $runde = runde_holen((string) ($p['id'] ?? ''));
    if ($runde['final'] === null) {
        fehler('Noch kein Termin festgelegt.', 404);
    }
    foreach (termine($runde['id']) as $t) {
        if ($t['id'] === (int) $runde['final']) {
            header('Content-Type: text/calendar; charset=utf-8');
            header('Content-Disposition: attachment; filename="funkerbier.ics"');
            echo ics($runde, $t['beginn']);
            exit;
        }
    }
    fehler('Termin nicht gefunden.', 404);
}

try {
    if ($_SERVER['REQUEST_METHOD'] === 'POST') {
        $p = json_decode((string) file_get_contents('php://input'), true);
        if (!is_array($p)) {
            fehler('Ungültige Anfrage.');
        }
        $aktion = $p['a'] ?? '';
        match ($aktion) {
            'anlegen' => a_anlegen($p),
            'eintragen' => a_eintragen($p),
            'final' => a_final($p),
            'loeschen' => a_loeschen($p),
            'link' => a_link($p),
            default => fehler('Unbekannte Aktion.'),
        };
    }
    match ($_GET['a'] ?? '') {
        'runde' => a_runde($_GET),
        'ics' => a_ics($_GET),
        default => fehler('Unbekannte Aktion.'),
    };
} catch (PDOException $e) {
    if (db()->inTransaction()) {
        db()->rollBack();
    }
    error_log('bier: DB ' . $e->getMessage());
    fehler('Störung im Logbuch (Datenbank).', 500);
}
