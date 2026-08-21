// Selbstheilung nach einem abgerissenen Blazor-Server-Circuit.
//
// PROBLEM (gemeldet von Ingo am 2026-08-21): Beim Wechsel zwischen Modulen in der linken
// Navigation oder beim Klick auf Tabs passierte vielfach nichts. Erst ein echter
// Seitenaufruf ueber die Adresszeile half wieder.
//
// URSACHE: Die Anwendung laeuft als Blazor Server. Jeder Klick geht ueber den
// SignalR-Circuit. Reisst der ab, bleibt die Seite optisch unveraendert, reagiert aber auf
// nichts mehr. Blazor versucht eine begrenzte Zahl von Wiederverbindungen und gibt dann
// endgueltig auf. Ab da ist die Seite dauerhaft tot, bis sie neu geladen wird.
//
// WAS DIESE DATEI TUT: Sie laedt die Seite automatisch neu, sobald Blazor das
// Wiederverbinden endgueltig aufgegeben hat. Das Overlay aus `app.css` zeigt dem Benutzer
// waehrend der Versuche, was los ist, statt ihn auf eine tote Seite klicken zu lassen.
//
// WARUM UEBER DIE CSS-KLASSEN UND NICHT UEBER `Blazor.defaultReconnectionHandler`:
// Die Klassen `components-reconnect-show`, `-failed` und `-rejected` auf dem Element
// `#components-reconnect-modal` sind der dokumentierte, stabile Vertrag von Blazor. Der
// Reconnection-Handler ist dagegen internes API und hat zwischen .NET-Versionen schon
// seine Form geaendert. Ein MutationObserver auf den Klassen kommt ohne Interna aus.
//
// BEWUSST NICHT: keine eigene Wiederverbindungslogik und kein Reload waehrend `show` —
// dann laufen die Versuche noch, und ein Reload wuerde den Seitenzustand unnoetig
// wegwerfen. Behandelt wird nur der Endzustand, in dem Blazor selbst nicht weiterkommt.
(function () {
    'use strict';

    var modalId = 'components-reconnect-modal';

    // Nach dem Aufgeben kurz warten, bevor neu geladen wird. Ohne Pause laeuft ein Server,
    // der gerade neu startet (Deploy, Anwendungspool-Recycling), in eine Reload-Schleife.
    var reloadDelayMs = 3000;

    // Zustaende, in denen Blazor endgueltig aufgegeben hat.
    var terminalClasses = ['components-reconnect-failed', 'components-reconnect-rejected'];

    var reloadScheduled = false;

    function scheduleReload(reason) {
        if (reloadScheduled) {
            return;
        }
        reloadScheduled = true;
        console.log('[Reconnect] Verbindung endgueltig verloren (' + reason +
                    '), Seite wird in ' + reloadDelayMs + ' ms neu geladen.');
        window.setTimeout(function () {
            location.reload();
        }, reloadDelayMs);
    }

    function checkState(element) {
        for (var i = 0; i < terminalClasses.length; i++) {
            if (element.classList.contains(terminalClasses[i])) {
                scheduleReload(terminalClasses[i]);
                return;
            }
        }
    }

    function observe() {
        var modal = document.getElementById(modalId);
        if (!modal) {
            console.log('[Reconnect] Element #' + modalId +
                        ' fehlt, Selbstheilung ist inaktiv.');
            return;
        }

        // Der Zustand kann schon gesetzt sein, bevor der Observer haengt.
        checkState(modal);

        new MutationObserver(function () {
            checkState(modal);
        }).observe(modal, { attributes: true, attributeFilter: ['class'] });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', observe);
    } else {
        observe();
    }
})();
