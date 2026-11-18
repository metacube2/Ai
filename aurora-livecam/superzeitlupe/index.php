<?php
/**
 * Superzeitlupe: ausgewählte Momente der Aurora-Livecam, verlangsamt (2026-11-13).
 *
 * Die Clips stehen in clips.json neben dieser Datei (per .htaccess nicht abrufbar) und liegen unter
 * clips/<id>/. Freigegeben wird ausschliesslich von Hand mit /usr/local/sbin/aurora-superzeitlupe auf
 * cAurora; die Rückschau selbst bleibt intern. Doku: cAuroraAndroid/backend/caurora-superzeitlupe/README.md
 */
header('Content-Type: text/html; charset=utf-8');
header('Cache-Control: no-cache');

$sprachen = ['de', 'en', 'it', 'fr', 'es', 'zh'];
$texte = [
    'titel' => ['de' => 'Superzeitlupe', 'en' => 'Super slow motion', 'it' => 'Super rallentatore', 'fr' => 'Super ralenti', 'es' => 'Supercámara lenta', 'zh' => '超级慢动作'],
    'unter' => [
        'de' => 'Was an der Livecam in einer Sekunde vorbei ist, bis 16-mal langsamer.',
        'en' => 'What passes the live cam in a second, up to 16 times slower.',
        'it' => 'Ciò che passa davanti alla livecam in un secondo, fino a 16 volte più lento.',
        'fr' => 'Ce qui passe devant la caméra en une seconde, jusqu’à 16 fois plus lentement.',
        'es' => 'Lo que pasa ante la cámara en un segundo, hasta 16 veces más lento.',
        'zh' => '镜头前一秒即逝的瞬间，最多放慢 16 倍。'],
    'zurueck' => ['de' => 'Zur Livecam', 'en' => 'To the live cam', 'it' => 'Alla livecam', 'fr' => 'Vers la caméra en direct', 'es' => 'A la cámara en directo', 'zh' => '返回直播'],
    'tempo' => ['de' => 'Tempo', 'en' => 'Speed', 'it' => 'Velocità', 'fr' => 'Vitesse', 'es' => 'Velocidad', 'zh' => '速度'],
    'ansicht' => ['de' => 'Ansicht', 'en' => 'View', 'it' => 'Vista', 'fr' => 'Vue', 'es' => 'Vista', 'zh' => '画面'],
    'nah-glatt' => ['de' => 'Nah, geglättet', 'en' => 'Close-up, smoothed', 'it' => 'Primo piano, fluido', 'fr' => 'Gros plan, lissé', 'es' => 'Primer plano, suavizado', 'zh' => '特写（平滑）'],
    'nah' => ['de' => 'Nah, echte Bilder', 'en' => 'Close-up, real frames', 'it' => 'Primo piano, fotogrammi reali', 'fr' => 'Gros plan, images réelles', 'es' => 'Primer plano, fotogramas reales', 'zh' => '特写（真实画面）'],
    'voll' => ['de' => 'Ganzes Bild', 'en' => 'Full view', 'it' => 'Immagine intera', 'fr' => 'Image entière', 'es' => 'Imagen completa', 'zh' => '完整画面'],
    'hinweis-echt' => [
        'de' => 'Echte Kamerabilder: 25 pro Sekunde, jedes wird länger gezeigt. Nichts ist berechnet.',
        'en' => 'Real camera frames: 25 per second, each shown for longer. Nothing is computed.',
        'it' => 'Fotogrammi reali: 25 al secondo, ognuno mostrato più a lungo. Nulla è calcolato.',
        'fr' => 'Images réelles : 25 par seconde, chacune affichée plus longtemps. Rien n’est calculé.',
        'es' => 'Fotogramas reales: 25 por segundo, cada uno se muestra más tiempo. Nada está calculado.',
        'zh' => '真实画面：每秒 25 帧，每帧显示更久，没有任何计算生成的内容。'],
    'hinweis-glatt' => [
        'de' => 'Geglättet: Die Kamera liefert 25 Bilder pro Sekunde. 7 von 8 Bildern hier sind berechnete Zwischenbilder, an den Flügelspitzen sieht man das manchmal.',
        'en' => 'Smoothed: the camera delivers 25 frames per second. 7 of 8 frames here are computed in-between frames; wing tips sometimes give it away.',
        'it' => 'Fluido: la camera fornisce 25 fotogrammi al secondo. Qui 7 fotogrammi su 8 sono intermedi calcolati; sulle punte delle ali a volte si nota.',
        'fr' => 'Lissé : la caméra fournit 25 images par seconde. Ici, 7 images sur 8 sont des images intermédiaires calculées ; cela se voit parfois au bout des ailes.',
        'es' => 'Suavizado: la cámara ofrece 25 fotogramas por segundo. Aquí 7 de cada 8 son fotogramas intermedios calculados; a veces se nota en las puntas de las alas.',
        'zh' => '平滑：摄像头每秒拍摄 25 帧。这里每 8 帧中有 7 帧是计算生成的中间帧，翅尖处有时能看出来。'],
    'bild' => ['de' => 'Bild', 'en' => 'Frame', 'it' => 'Fotogramma', 'fr' => 'Image', 'es' => 'Fotograma', 'zh' => '帧'],
    'alle' => ['de' => 'Alle Momente', 'en' => 'All moments', 'it' => 'Tutti i momenti', 'fr' => 'Tous les moments', 'es' => 'Todos los momentos', 'zh' => '全部瞬间'],
    'echtzeit' => ['de' => 'in Echtzeit', 'en' => 'in real time', 'it' => 'in tempo reale', 'fr' => 'en temps réel', 'es' => 'en tiempo real', 'zh' => '实际时长'],
    'leer' => [
        'de' => 'Die ersten Momente folgen in Kürze.', 'en' => 'The first moments will follow shortly.', 'it' => 'I primi momenti arriveranno a breve.',
        'fr' => 'Les premiers moments arrivent bientôt.', 'es' => 'Los primeros momentos llegarán pronto.', 'zh' => '首批瞬间即将上线。'],
    'so-titel' => ['de' => 'So entsteht sie', 'en' => 'How it is made', 'it' => 'Come nasce', 'fr' => 'Comment elle est faite', 'es' => 'Cómo se hace', 'zh' => '制作方式'],
    'so-1' => [
        'de' => 'Die Kamera nimmt rund um die Uhr 25 Bilder pro Sekunde in 3072 × 1728 Pixeln auf. Fliegt etwas nah vorbei, suchen wir die Stelle heraus und schneiden sie Bild für Bild aus.',
        'en' => 'The camera records 25 frames per second around the clock at 3072 × 1728 pixels. When something flies close by, we pick the spot and cut it out frame by frame.',
        'it' => 'La camera registra 24 ore su 24 a 25 fotogrammi al secondo in 3072 × 1728 pixel. Quando qualcosa passa vicino, cerchiamo il punto e lo ritagliamo fotogramma per fotogramma.',
        'fr' => 'La caméra enregistre jour et nuit 25 images par seconde en 3072 × 1728 pixels. Quand quelque chose passe tout près, nous repérons le passage et le découpons image par image.',
        'es' => 'La cámara graba las 24 horas a 25 fotogramas por segundo en 3072 × 1728 píxeles. Cuando algo pasa cerca, buscamos el momento y lo recortamos fotograma a fotograma.',
        'zh' => '摄像头全天候以每秒 25 帧、3072 × 1728 像素拍摄。有东西近距离飞过时，我们找出这一段并逐帧剪出。'],
    'so-2' => [
        'de' => 'Die Nahaufnahme ist ein Ausschnitt in voller Kameraauflösung, der mit dem Motiv mitläuft. Für die geglättete Fassung berechnet ein Programm zwischen je zwei echten Bildern sieben weitere.',
        'en' => 'The close-up is a crop at full camera resolution that follows the subject. For the smoothed version, software computes seven more frames between every two real ones.',
        'it' => 'Il primo piano è un ritaglio alla piena risoluzione della camera che segue il soggetto. Per la versione fluida un programma calcola altri sette fotogrammi tra due reali.',
        'fr' => 'Le gros plan est un recadrage en pleine résolution qui suit le sujet. Pour la version lissée, un logiciel calcule sept images supplémentaires entre deux images réelles.',
        'es' => 'El primer plano es un recorte a resolución completa de la cámara que sigue al motivo. Para la versión suavizada, un programa calcula siete fotogramas más entre cada dos reales.',
        'zh' => '特写是随主体移动的全分辨率裁切画面。平滑版本中，程序在每两帧真实画面之间再计算出七帧。'],
];

function alle_sprachen(array $werte): string {
    $aus = '';
    foreach ($werte as $s => $w) {
        if (preg_match('/^[a-z]{2}$/', (string) $s) && is_string($w) && $w !== '') {
            $aus .= ' data-' . $s . '="' . htmlspecialchars($w, ENT_QUOTES, 'UTF-8') . '"';
        }
    }
    return $aus;
}
function t(string $schluessel): string {
    global $texte;
    return alle_sprachen($texte[$schluessel]) . '>' . htmlspecialchars($texte[$schluessel]['de'], ENT_QUOTES, 'UTF-8');
}

// Clips laden; nur zeigen, was vollständig und sauber benannt ist.
$clips = [];
$roh = @file_get_contents(__DIR__ . '/clips.json');
$daten = $roh ? json_decode($roh, true) : null;
foreach (($daten['clips'] ?? []) as $c) {
    $id = (string) ($c['id'] ?? '');
    if (!preg_match('/^[a-z0-9-]{3,60}$/', $id) || empty($c['titel']['de'])) {
        continue;
    }
    $fassungen = [];
    foreach (($c['fassungen'] ?? []) as $f) {
        $art = (string) ($f['art'] ?? '');
        $datei = (string) ($f['datei'] ?? '');
        $pfad = __DIR__ . "/clips/$id/$datei";
        if (!in_array($art, ['nah-glatt', 'nah', 'voll'], true) || !preg_match('/^[a-z0-9-]+\.mp4$/', $datei) || !is_file($pfad)) {
            continue;
        }
        $fassungen[] = ['art' => $art, 'url' => "clips/$id/$datei?v=" . filemtime($pfad),
                        'faktor' => max(1, (int) ($f['faktor'] ?? 1)), 'berechnet' => !empty($f['berechnet'])];
    }
    if (!$fassungen) {
        continue;
    }
    $poster = is_file(__DIR__ . "/clips/$id/poster.jpg") ? "clips/$id/poster.jpg?v=" . filemtime(__DIR__ . "/clips/$id/poster.jpg") : '';
    $clips[] = ['id' => $id, 'zeit' => (string) ($c['zeit'] ?? ''), 'dauer' => (float) ($c['dauer'] ?? 0),
                'titel' => (array) $c['titel'], 'text' => (array) ($c['text'] ?? []), 'fassungen' => $fassungen, 'poster' => $poster];
}
$erster = $clips[0] ?? null;
$basis = 'https://www.aurora-weather-livecam.com/superzeitlupe/';
?>
<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Superzeitlupe – Aurora Livecam</title>
<meta name="description" content="Ausgewählte Momente der Aurora-Livecam in Superzeitlupe: Rotmilane und andere schnelle Gäste, bis 16-mal langsamer.">
<link rel="canonical" href="<?php echo $basis; ?>">
<meta property="og:title" content="Superzeitlupe – Aurora Livecam">
<meta property="og:description" content="Was an der Livecam in einer Sekunde vorbei ist, bis 16-mal langsamer.">
<meta property="og:type" content="website">
<meta property="og:url" content="<?php echo $basis; ?>">
<?php if ($erster && $erster['poster']) { ?><meta property="og:image" content="<?php echo $basis . htmlspecialchars($erster['poster'], ENT_QUOTES, 'UTF-8'); ?>">
<?php } ?>
<link rel="icon" type="image/svg+xml" href="/app/icon.svg">
<style>
:root { --grund:#050b24; --flaeche:#0d1a3d; --rand:#22386b; --text:#eef4ff; --leise:#9db0d6; --akzent:#23f5b1; --akzent2:#1fd3e6; }
* { box-sizing: border-box; }
html { -webkit-text-size-adjust: 100%; }
body { margin: 0; background: linear-gradient(180deg, #050b24 0%, #0b1c44 60%, #050b24 100%) fixed; color: var(--text);
       font: 16px/1.5 -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "PingFang SC", "Microsoft YaHei", sans-serif; }
a { color: var(--akzent2); }
.rahmen { max-width: 1100px; margin: 0 auto; padding: 0 16px; }
header.kopf { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 14px 0; flex-wrap: wrap; }
.zurueck { display: inline-flex; align-items: center; gap: 8px; color: var(--text); text-decoration: none; padding: 8px 14px; border: 1px solid var(--rand); border-radius: 999px; background: rgba(13,26,61,.7); }
.zurueck:hover { border-color: var(--akzent2); }
.sprachen { display: flex; gap: 4px; flex-wrap: wrap; }
.sprachen button { background: none; border: 1px solid transparent; color: var(--leise); padding: 6px 9px; border-radius: 8px; font: inherit; font-size: .85em; cursor: pointer; text-transform: uppercase; }
.sprachen button[aria-pressed="true"] { color: var(--grund); background: var(--akzent); font-weight: 700; }
h1 { font-size: clamp(2em, 6vw, 3.4em); line-height: 1.1; margin: 18px 0 6px; letter-spacing: -.02em;
     background: linear-gradient(90deg, var(--akzent), var(--akzent2)); -webkit-background-clip: text; background-clip: text; color: transparent; }
.unter { color: var(--leise); font-size: 1.15em; margin: 0 0 22px; max-width: 46em; }
.buehne { background: #000; border-radius: 18px; overflow: hidden; box-shadow: 0 18px 50px rgba(0,0,0,.5); border: 1px solid var(--rand); }
.buehne video { display: block; width: 100%; aspect-ratio: 16 / 9; background: #000; cursor: pointer; }
.cliptitel { margin: 16px 0 2px; font-size: 1.35em; }
.clipzeit { color: var(--leise); margin: 0 0 4px; font-size: .95em; }
.cliptext { margin: 6px 0 0; max-width: 52em; }
.regler { display: flex; flex-wrap: wrap; gap: 14px 26px; margin: 16px 0 6px; align-items: center; }
.gruppe { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.gruppe > span { color: var(--leise); font-size: .85em; text-transform: uppercase; letter-spacing: .06em; margin-right: 4px; }
.knopf { font: inherit; color: var(--text); background: var(--flaeche); border: 1px solid var(--rand); padding: 8px 13px; border-radius: 10px; cursor: pointer; min-height: 40px; }
.knopf:hover { border-color: var(--akzent2); }
.knopf[aria-pressed="true"] { background: linear-gradient(90deg, var(--akzent), var(--akzent2)); color: #04130e; border-color: transparent; font-weight: 700; }
.knopf:focus-visible, .sprachen button:focus-visible, .karte:focus-visible, .zurueck:focus-visible { outline: 3px solid var(--akzent2); outline-offset: 2px; }
.hinweis { color: var(--leise); font-size: .92em; margin: 8px 0 0; min-height: 2.8em; max-width: 60em; }
h2 { margin: 38px 0 14px; font-size: 1.4em; }
.liste { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 16px; }
.karte { text-align: left; font: inherit; color: var(--text); background: var(--flaeche); border: 1px solid var(--rand); border-radius: 14px; padding: 0; overflow: hidden; cursor: pointer; display: block; width: 100%; }
.karte:hover { border-color: var(--akzent2); }
.karte[aria-current="true"] { border-color: var(--akzent); box-shadow: 0 0 0 2px var(--akzent); }
.karte img { display: block; width: 100%; height: auto; aspect-ratio: 16 / 9; object-fit: cover; background: #000; }
.karte b { display: block; padding: 10px 12px 0; }
.karte small { display: block; padding: 2px 12px 12px; color: var(--leise); }
.so { background: rgba(13,26,61,.6); border: 1px solid var(--rand); border-radius: 14px; padding: 4px 20px 10px; margin: 38px 0; }
.so p { max-width: 60em; }
footer { color: var(--leise); text-align: center; padding: 10px 0 40px; font-size: .9em; }
@media (max-width: 560px) { .regler { gap: 12px; } .knopf { padding: 8px 11px; } }
</style>
</head>
<body>
<div class="rahmen">
    <header class="kopf">
        <a class="zurueck" href="/">← <span<?php echo t('zurueck'); ?></span></a>
        <div class="sprachen" role="group" aria-label="Sprache / Language">
<?php foreach ($sprachen as $s) { ?>
            <button type="button" data-sprache="<?php echo $s; ?>" aria-pressed="<?php echo $s === 'de' ? 'true' : 'false'; ?>"><?php echo $s === 'zh' ? '中文' : $s; ?></button>
<?php } ?>
        </div>
    </header>

    <h1<?php echo t('titel'); ?></h1>
    <p class="unter"<?php echo t('unter'); ?></p>

<?php if (!$clips) { ?>
    <p<?php echo t('leer'); ?></p>
<?php } else { ?>
    <div class="buehne">
        <video id="film" muted loop playsinline preload="metadata"<?php echo $erster['poster'] ? ' poster="' . htmlspecialchars($erster['poster'], ENT_QUOTES, 'UTF-8') . '"' : ''; ?>></video>
    </div>
    <h2 class="cliptitel" id="cliptitel"></h2>
    <p class="clipzeit" id="clipzeit"></p>
    <p class="cliptext" id="cliptext"></p>

    <div class="regler">
        <div class="gruppe" id="tempo" role="group">
            <span<?php echo t('tempo'); ?></span>
<?php foreach ([1, 2, 4, 8, 16] as $z) { ?>
            <button type="button" class="knopf" data-tempo="<?php echo $z; ?>" aria-pressed="<?php echo $z === 8 ? 'true' : 'false'; ?>"><?php echo $z === 1 ? '1×' : "1/$z"; ?></button>
<?php } ?>
        </div>
        <div class="gruppe" id="ansicht" role="group">
            <span<?php echo t('ansicht'); ?></span>
<?php foreach (['nah-glatt', 'nah', 'voll'] as $art) { ?>
            <button type="button" class="knopf" data-art="<?php echo $art; ?>" aria-pressed="false"<?php echo t($art); ?></button>
<?php } ?>
        </div>
        <div class="gruppe" role="group">
            <button type="button" class="knopf" id="zurueck-bild" aria-label="Bild zurück">◀︎ <span<?php echo t('bild'); ?></span></button>
            <button type="button" class="knopf" id="spielen" aria-label="Abspielen / Halt">⏸</button>
            <button type="button" class="knopf" id="vor-bild" aria-label="Bild vor"><span<?php echo t('bild'); ?></span> ▶︎</button>
        </div>
    </div>
    <p class="hinweis" id="hinweis"></p>

    <h2<?php echo t('alle'); ?></h2>
    <div class="liste" id="liste">
<?php foreach ($clips as $c) { ?>
        <button type="button" class="karte" data-id="<?php echo $c['id']; ?>">
            <?php if ($c['poster']) { ?><img src="<?php echo htmlspecialchars($c['poster'], ENT_QUOTES, 'UTF-8'); ?>" alt="" loading="lazy" width="1280" height="720"><?php } ?>
            <b<?php echo alle_sprachen($c['titel']); ?>><?php echo htmlspecialchars($c['titel']['de'], ENT_QUOTES, 'UTF-8'); ?></b>
            <small data-zeit="<?php echo htmlspecialchars($c['zeit'], ENT_QUOTES, 'UTF-8'); ?>" data-dauer="<?php echo $c['dauer']; ?>"></small>
        </button>
<?php } ?>
    </div>
<?php } ?>

    <section class="so">
        <h2<?php echo t('so-titel'); ?></h2>
        <p<?php echo t('so-1'); ?></p>
        <p<?php echo t('so-2'); ?></p>
    </section>
    <footer>
        <a href="/">Aurora Weather Livecam</a>
    </footer>
</div>

<script>
(function () {
    'use strict';
    var clips = <?php echo json_encode($clips, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_HEX_TAG | JSON_HEX_AMP); ?>;
    var texte = <?php echo json_encode(['echt' => $texte['hinweis-echt'], 'glatt' => $texte['hinweis-glatt'], 'echtzeit' => $texte['echtzeit']], JSON_UNESCAPED_UNICODE | JSON_HEX_TAG | JSON_HEX_AMP); ?>;
    var sprachen = <?php echo json_encode($sprachen); ?>;
    var sprache = 'de', tempo = 8, wunschArt = 'nah-glatt', clip = null, fassung = null, schrittTimer = null;
    var film = document.getElementById('film');

    function wort(werte) { return (werte && (werte[sprache] || werte.en || werte.de)) || ''; }

    function zeittext(iso, dauer) {
        var teile = [];
        var d = iso ? new Date(iso) : null;
        if (d && !isNaN(d)) {
            try {
                teile.push(d.toLocaleString(sprache === 'zh' ? 'zh-CN' : sprache, { timeZone: 'Europe/Zurich', day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }));
            } catch (e) { teile.push(iso.slice(0, 16).replace('T', ' ')); }
        }
        if (dauer > 0) {
            var zahl = new Intl.NumberFormat(sprache === 'zh' ? 'zh-CN' : sprache, { minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(dauer);
            teile.push(zahl + ' s ' + wort(texte.echtzeit));
        }
        return teile.join(' · ');
    }

    function spracheSetzen(neu, merken) {
        if (sprachen.indexOf(neu) < 0) { return; }
        sprache = neu;
        document.querySelectorAll('[data-de]').forEach(function (e) {
            var w = e.getAttribute('data-' + neu) || e.getAttribute('data-en') || e.getAttribute('data-de');
            if (w) { e.textContent = w; }
        });
        document.documentElement.lang = neu === 'zh' ? 'zh-CN' : neu;
        document.querySelectorAll('.sprachen button').forEach(function (b) { b.setAttribute('aria-pressed', b.dataset.sprache === neu ? 'true' : 'false'); });
        document.querySelectorAll('.karte small').forEach(function (s) { s.textContent = zeittext(s.dataset.zeit, parseFloat(s.dataset.dauer)); });
        if (merken) { try { localStorage.setItem('auroraSprache', neu); } catch (e) {} }
        if (clip) { beschriften(); }
    }

    function beschriften() {
        document.getElementById('cliptitel').textContent = wort(clip.titel);
        document.getElementById('clipzeit').textContent = zeittext(clip.zeit, clip.dauer);
        document.getElementById('cliptext').textContent = wort(clip.text);
        document.getElementById('hinweis').textContent = wort(fassung.berechnet ? texte.glatt : texte.echt);
    }

    // Tempo 1/z: Die Datei ist bereits um `faktor` verlangsamt, der Rest kommt über die Abspielrate.
    // Lehnt der Browser eine sehr kleine Rate ab, wird Bild für Bild weitergeschaltet.
    function tempoAnwenden() {
        clearInterval(schrittTimer); schrittTimer = null;
        var rate = fassung.faktor / tempo;
        try {
            film.playbackRate = rate;
            film.defaultPlaybackRate = rate;
            if (Math.abs(film.playbackRate - rate) > 1e-6) { throw new Error('Rate nicht übernommen'); }
        } catch (e) {
            film.playbackRate = 1; film.defaultPlaybackRate = 1;
            if (!film.paused) { handbetrieb(rate); }
        }
        document.querySelectorAll('#tempo .knopf').forEach(function (b) { b.setAttribute('aria-pressed', Number(b.dataset.tempo) === tempo ? 'true' : 'false'); });
    }
    function handbetrieb(rate) {
        film.pause();
        schrittTimer = setInterval(function () {
            var t = film.currentTime + 0.04;
            film.currentTime = (film.duration && t >= film.duration - 0.02) ? 0 : t;
        }, 40 / rate);
        document.getElementById('spielen').textContent = '⏸';
    }
    function laeuft() { return !film.paused || schrittTimer !== null; }
    function halt() { clearInterval(schrittTimer); schrittTimer = null; film.pause(); document.getElementById('spielen').textContent = '▶︎'; }
    function los() {
        document.getElementById('spielen').textContent = '⏸';
        var p = film.play();
        if (p && p.catch) { p.catch(function () { document.getElementById('spielen').textContent = '▶︎'; }); }
        tempoAnwenden();
    }

    function fassungWaehlen(art, anStelle, spielen) {
        wunschArt = art;
        var neu = clip.fassungen.filter(function (f) { return f.art === art; })[0] || clip.fassungen[0];
        var lief = spielen || laeuft();
        // Stelle im Clip beim Wechsel der Ansicht halten (in Echtzeit-Sekunden gerechnet)
        var echt = (fassung && anStelle) ? film.currentTime / fassung.faktor : 0;
        fassung = neu;
        clearInterval(schrittTimer); schrittTimer = null;
        film.src = fassung.url;
        film.addEventListener('loadedmetadata', function einmal() {
            film.removeEventListener('loadedmetadata', einmal);
            if (echt > 0) { try { film.currentTime = Math.min(echt * fassung.faktor, Math.max(0, film.duration - 0.05)); } catch (e) {} }
            if (lief) { los(); } else { tempoAnwenden(); }
        });
        film.load();
        document.querySelectorAll('#ansicht .knopf').forEach(function (b) {
            b.hidden = !clip.fassungen.some(function (f) { return f.art === b.dataset.art; });
            b.setAttribute('aria-pressed', b.dataset.art === fassung.art ? 'true' : 'false');
        });
        beschriften();
    }

    // Ein neu gewählter Clip läuft immer los (stumm, deshalb ohne Rückfrage des Browsers).
    function clipWaehlen(id, rollen) {
        clip = clips.filter(function (c) { return c.id === id; })[0] || clips[0];
        film.poster = clip.poster || '';
        fassung = null;
        fassungWaehlen(wunschArt, false, true);
        document.querySelectorAll('.karte').forEach(function (k) { k.setAttribute('aria-current', k.dataset.id === clip.id ? 'true' : 'false'); });
        try { history.replaceState(null, '', '?clip=' + encodeURIComponent(clip.id)); } catch (e) {}
        if (rollen) { document.querySelector('.buehne').scrollIntoView({ behavior: 'smooth', block: 'start' }); }
    }

    document.querySelectorAll('.sprachen button').forEach(function (b) { b.addEventListener('click', function () { spracheSetzen(b.dataset.sprache, true); }); });
    var start = null;
    try { start = new URLSearchParams(location.search).get('lang') || localStorage.getItem('auroraSprache'); } catch (e) {}
    spracheSetzen(start || 'de', false);

    if (!film || !clips.length) { return; }
    document.querySelectorAll('#tempo .knopf').forEach(function (b) { b.addEventListener('click', function () { tempo = Number(b.dataset.tempo); tempoAnwenden(); if (laeuft()) { los(); } }); });
    document.querySelectorAll('#ansicht .knopf').forEach(function (b) { b.addEventListener('click', function () { fassungWaehlen(b.dataset.art, true, false); }); });
    document.querySelectorAll('.karte').forEach(function (k) { k.addEventListener('click', function () { clipWaehlen(k.dataset.id, true); }); });
    function umschalten() { if (laeuft()) { halt(); } else { los(); } }
    document.getElementById('spielen').addEventListener('click', umschalten);
    film.addEventListener('click', umschalten);
    function schritt(richtung) {
        halt();
        var t = film.currentTime + richtung * 0.04;
        film.currentTime = Math.min(Math.max(t, 0), Math.max(0, (film.duration || 0) - 0.02));
    }
    document.getElementById('zurueck-bild').addEventListener('click', function () { schritt(-1); });
    document.getElementById('vor-bild').addEventListener('click', function () { schritt(1); });
    document.addEventListener('keydown', function (e) {
        if (e.target.tagName === 'BUTTON' && (e.key === ' ' || e.key === 'Enter')) { return; }
        if (e.key === ' ') { e.preventDefault(); umschalten(); }
        if (e.key === 'ArrowLeft') { schritt(-1); }
        if (e.key === 'ArrowRight') { schritt(1); }
    });

    var gewuenscht = null;
    try { gewuenscht = new URLSearchParams(location.search).get('clip'); } catch (e) {}
    clipWaehlen(gewuenscht || clips[0].id, false);
})();
</script>
</body>
</html>
