// Kompass Service Worker: nur die App-Huelle offline, Daten immer frisch vom Server.
const HUELLE = 'kompass-huelle-1';
const DATEIEN = ['/static/stil.css?v=1', '/static/app.js?v=1', '/static/icon.svg', '/static/icon-192.png'];

self.addEventListener('install', (ev) => { ev.waitUntil(caches.open(HUELLE).then((c) => c.addAll(DATEIEN)).then(() => self.skipWaiting())); });
self.addEventListener('activate', (ev) => {
  ev.waitUntil(caches.keys().then((alle) => Promise.all(alle.filter((n) => n !== HUELLE).map((n) => caches.delete(n)))).then(() => self.clients.claim()));
});
self.addEventListener('fetch', (ev) => {
  const url = new URL(ev.request.url);
  if (ev.request.method !== 'GET' || url.origin !== location.origin || url.pathname.startsWith('/api/')) return;
  if (url.pathname.startsWith('/static/')) {
    ev.respondWith(caches.match(ev.request).then((treffer) => treffer || fetch(ev.request)));
    return;
  }
  if (ev.request.mode === 'navigate') {
    // Seite immer vom Netz (Anmeldung!); offline eine schlichte Meldung
    ev.respondWith(fetch(ev.request).catch(() => new Response(
      '<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>Kompass</title>' +
      '<body style="font:18px system-ui;padding:40px;text-align:center;background:#f6f4ef;color:#23272e">' +
      '<h1>Gerade offline</h1><p>Kompass braucht eine Verbindung. Sobald das Netz zurück ist, einfach neu laden.</p>',
      { headers: { 'Content-Type': 'text/html; charset=utf-8' } })));
  }
});
