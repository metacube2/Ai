// Hell/Dunkel-Wahl des Cockpits, je Browser gemerkt (Components/Layout/MainLayout.razor).
// `data-theme` am <html> steuert die eigenen Farben in app.css; das Setzen schon im <head>
// (App.razor) verhindert ein weisses Aufblitzen, bevor MudBlazor sein Theme gezeichnet hat.
window.trafagTheme = {
    get: function () {
        try { return localStorage.getItem("trafag-theme"); } catch { return null; }
    },
    set: function (mode) {
        try { localStorage.setItem("trafag-theme", mode); } catch { }
        document.documentElement.setAttribute("data-theme", mode);
    },
    // Skin: "ci" (Trafag CI, orange, Standard seit 2026-10-01) oder "classic" (bisheriges Rot).
    getSkin: function () {
        try { return localStorage.getItem("trafag-skin"); } catch { return null; }
    },
    setSkin: function (skin) {
        try { localStorage.setItem("trafag-skin", skin); } catch { }
        document.documentElement.setAttribute("data-skin", skin);
    },
    // Dimmer (2026-10-01): 0 = dunkelster, 100 = hellster Hintergrund, 50 = bisheriger Stand.
    // Ueberschreibt die MudBlazor-Variablen am <html>; ein Inline-Stil schlaegt :root aus dem
    // ThemeProvider. Bei Hell/Dunkel-Wechsel neu anwenden (MainLayout).
    getDim: function () {
        var v = null;
        try { v = parseInt(localStorage.getItem("trafag-dim"), 10); } catch { }
        return isNaN(v) ? 50 : v;
    },
    setDim: function (value) {
        try { localStorage.setItem("trafag-dim", String(value)); } catch { }
        window.trafagTheme.applyDim(value);
    },
    applyDim: function (value) {
        var v = Math.max(0, Math.min(100, value));
        var root = document.documentElement.style;
        var dark = document.documentElement.getAttribute("data-theme") !== "light";
        // Dunkel: Helligkeit 5 % bis 27 %, bei 50 rund 13 % wie bisher (#1E1F25).
        // Hell: 84 % bis 100 %, bei 50 weiss wie bisher.
        var l = dark ? 5 + v * 0.17 : Math.min(100, 84 + v * 0.32);
        var hsl = function (x) { return "hsl(230, " + (dark ? 10 : 12) + "%, " + Math.max(0, Math.min(100, x)).toFixed(1) + "%)"; };
        root.setProperty("--mud-palette-background", hsl(l));
        root.setProperty("--mud-palette-surface", hsl(dark ? l + 4 : Math.min(100, l + 6)));
        root.setProperty("--mud-palette-drawer-background", hsl(dark ? l + 2 : Math.min(100, l + 3)));
        document.body && (document.body.style.background = hsl(l));
    }
};
