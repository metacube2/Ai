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
    }
};
