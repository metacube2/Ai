namespace TrafagSalesExporter.Services;

// SAP-Tabelle T023T (Warengruppen-Texte), Sprache DE, geliefert von Ingo per 24.07.2026 als
// Listenausgabe (WGBEZ, SAP-seitig auf 20 Zeichen abgeschnitten - WGBEZ60 war in der Liste leer,
// Text daher 1:1 wie geliefert, auch wo sichtbar abgeschnitten). EN/FR-Zeilen aus derselben
// Listenausgabe waren SAP-Demo-Platzhaltertexte (z.B. "Material group 1") und wurden bewusst NICHT
// uebernommen. Neue Codes: hier ergaenzen (Ingo liefert Matkl;Wgbez, Zeile anhaengen) - unbekannte
// Codes fallen automatisch auf den reinen Matkl-Code zurueck, es gibt also nie eine Luecke.
public static class PurchasingMaterialGroupTextCatalog
{
    private static readonly Dictionary<string, string> TextByMatkl = new(StringComparer.OrdinalIgnoreCase)
    {
        ["01"] = "Dummy",
        ["01.00.00"] = "Rohmaterial",
        ["01.01.00"] = "Rohmaterial allgemei",
        ["10.00.00"] = "Elektronik & Elektro",
        ["10.01.00"] = "ElektronischeBauteil",
        ["10.02.00"] = "Schalter&Relais",
        ["10.03.00"] = "Displays&Anzeigen",
        ["10.04.00"] = "Kabel & Litzen",
        ["10.05.00"] = "Kabel konfektioniert",
        ["10.06.00"] = "Steckverbinder",
        ["10.07.00"] = "Leiterplatten unbest",
        ["10.08.00"] = "Leiterplatten bestüc",
        ["10.99.00"] = "sonstige Elektronik",
        ["20.00.00"] = "Mechanik",
        ["20.01.00"] = "Anschlussteile",
        ["20.01.01"] = "AnschlTeil gedr/gefr",
        ["20.02.00"] = "Gehäuseteile",
        ["20.02.01"] = "GehTeil gedr/gefr",
        ["20.02.02"] = "Gehäuseteile Guss",
        ["20.02.03"] = "Gehäuseteile Kunstst",
        ["20.03.00"] = "Komponente",
        ["20.03.01"] = "Komp. gedr/gefr",
        ["20.03.02"] = "Komponente Guss",
        ["20.03.03"] = "Komponente Kunststof",
        ["20.04.00"] = "Rohmembranen",
        ["20.05.00"] = "Bälge",
        ["20.06.00"] = "Stanz- Biegeteile",
        ["20.99.00"] = "sonstige Mechanik",
        ["30.00.00"] = "Norm- & Verbindungst",
        ["30.01.00"] = "Schrauben",
        ["30.02.00"] = "Muttern",
        ["30.03.00"] = "Scheiben",
        ["30.04.00"] = "Stifte",
        ["30.05.00"] = "Federn",
        ["30.06.00"] = "Dichtungen",
        ["30.07.00"] = "Filter",
        ["30.08.00"] = "Kabelverschraubungen",
        ["30.08.01"] = "Kabelverschr. Metall",
        ["30.08.02"] = "Kabelverschr. Kunst",
        ["30.99.00"] = "sonstige Normteile",
        ["40.00.00"] = "Baugruppen fremdbesc",
        ["40.01.00"] = "Ventile",
        ["40.02.00"] = "Sensoren",
        ["40.02.01"] = "Membranen DMS",
        ["40.02.02"] = "Membranen BiBA",
        ["40.02.03"] = "Membranen Keramik",
        ["40.03.00"] = "Messwerke",
        ["40.99.00"] = "sonstige",
        ["50.00.00"] = "Werkzeuge",
        ["50.01.00"] = "Bohrer",
        ["50.02.00"] = "Wendeplatten",
        ["50.03.00"] = "Löten",
        ["50.04.00"] = "Dosieren",
        ["50.99.00"] = "sonstige Werkzeuge",
        ["60.00.00"] = "Hilfs-, Betriebsmat.",
        ["60.01.00"] = "Arbeitsschutz",
        ["60.02.00"] = "Öl/Schmier/Reinig.",
        ["60.03.00"] = "Klebstoffe",
        ["60.04.00"] = "Gas",
        ["60.05.00"] = "Büromaterial&Werbear",
        ["60.99.00"] = "sonstige Verbrauchsm",
        ["70.00.00"] = "Verpackung & Logisti",
        ["70.01.00"] = "Kartonagen",
        ["70.02.00"] = "Einlagen",
        ["70.03.00"] = "Beutel",
        ["70.04.00"] = "Etiketten&Beschriftu",
        ["70.05.00"] = "Drucksachen",
        ["70.06.00"] = "Paletten",
        ["70.07.00"] = "Warenträger",
        ["70.99.00"] = "sonstige Verpackunge",
        ["LA01"] = "Längenabhängig",
        ["SL00"] = "Schutzrohrlänge",
        ["SL04"] = "PVC-Schlauch (+4%)",
        ["SL07"] = "Metallschlauch (+7%)",
        ["SL10"] = "Metallschlauch(+10%)",
        ["TS_ALU"] = "Aluschilder",
        ["TS_AUTO"] = "Typenschild automat.",
    };

    public static string Resolve(string matkl)
    {
        if (string.IsNullOrWhiteSpace(matkl) || matkl == "ohne Warengruppe")
            return matkl;

        return TextByMatkl.TryGetValue(matkl, out var text) ? $"{matkl} – {text}" : matkl;
    }
}
