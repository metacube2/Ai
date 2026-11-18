// Offline-Hülle: Seite und Assets aus dem Cache, wenn im Gym kein Netz ist.
// Die API wird nie gecacht. Version bei jeder Änderung hochzählen.
const CACHE = 'gym-20261014b';
const HUELLE = ['./', 'index.html', 'style.css?v=20261014b', 'app.js?v=20261014b', 'icon.svg', 'manifest.webmanifest'];

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(CACHE).then((c) => c.addAll(HUELLE)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys().then((ks) => Promise.all(ks.filter((k) => k !== CACHE).map((k) => caches.delete(k))))
    .then(() => self.clients.claim()));
});

self.addEventListener('fetch', (e) => {
  const url = new URL(e.request.url);
  if (e.request.method !== 'GET' || url.origin !== location.origin || url.pathname.endsWith('api.php')) return;
  // Netz zuerst, damit neue Versionen sofort ankommen; Cache als Rückfall
  e.respondWith(
    fetch(e.request)
      .then((r) => {
        if (r.ok) { const k = r.clone(); caches.open(CACHE).then((c) => c.put(e.request, k)); }
        return r;
      })
      .catch(() => caches.match(e.request, { ignoreSearch: url.pathname === '/' }).then((r) => r || caches.match('index.html')))
  );
});
