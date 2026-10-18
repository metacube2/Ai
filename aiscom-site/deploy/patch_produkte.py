"""Fuegt der AISCOM-Next.js-Seite den Bereich "Projekte im Verkauf" hinzu.

Im Ordner /var/www/html/aiscom/frontend auf cAiscom (LXC 104) ausfuehren.
Bricht ab, wenn der Bereich schon drin ist.
"""
import json

T = {
    "de": {"nav": "Produkte", "title": "Projekte im Verkauf",
           "subtitle": "Eigene Software – erhältlich oder kurz vor dem Start.",
           "items": [
               {"name": "MacYaesu", "badge": "Erhältlich",
                "description": "macOS-Fernsteuerung für den Yaesu FT-991A: CAT-Steuerung, Logbuch und Audio-Routing. Demo zum Testen, Vollversion mit Lizenz.",
                "href": "/macyaesu/", "cta": "Zur Produktseite"},
               {"name": "Kamera-Uploader", "badge": "Bald verfügbar",
                "description": "Neues Projekt. Beschreibung, Preis und Download folgen in Kürze.",
                "href": "/kamera-uploader/", "cta": "Mehr erfahren"}]},
    "en": {"nav": "Products", "title": "Products for sale",
           "subtitle": "Own software – available now or launching soon.",
           "items": [
               {"name": "MacYaesu", "badge": "Available",
                "description": "macOS remote control for the Yaesu FT-991A: CAT control, logbook and audio routing. Free demo, full version with licence.",
                "href": "/macyaesu/", "cta": "Product page"},
               {"name": "Kamera-Uploader", "badge": "Coming soon",
                "description": "New project. Description, price and download will follow shortly.",
                "href": "/kamera-uploader/", "cta": "Learn more"}]},
    "it": {"nav": "Prodotti", "title": "Prodotti in vendita",
           "subtitle": "Software proprio – disponibile o in arrivo.",
           "items": [
               {"name": "MacYaesu", "badge": "Disponibile",
                "description": "Controllo remoto macOS per lo Yaesu FT-991A: controllo CAT, logbook e routing audio. Demo gratuita, versione completa con licenza.",
                "href": "/macyaesu/", "cta": "Pagina prodotto"},
               {"name": "Kamera-Uploader", "badge": "In arrivo",
                "description": "Nuovo progetto. Descrizione, prezzo e download seguiranno a breve.",
                "href": "/kamera-uploader/", "cta": "Scopri di più"}]},
}

SECTION = '''      {/* Products Section */}
      <section id="produkte" className="py-20 bg-primary scroll-mt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="section-title">{dictionary.products.title}</h2>
            <p className="section-subtitle">{dictionary.products.subtitle}</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-4xl mx-auto">
            {dictionary.products.items.map((product) => (
              <a key={product.href} href={product.href} className="block">
                <Card variant="hover" className="h-full">
                  <span className="inline-block px-3 py-1 mb-4 bg-primary-light rounded-full text-accent text-xs font-semibold uppercase tracking-wide">
                    {product.badge}
                  </span>
                  <h3 className="text-2xl font-semibold text-white mb-3">{product.name}</h3>
                  <p className="text-gray-400 mb-4">{product.description}</p>
                  <p className="text-accent font-semibold">{product.cta} &rarr;</p>
                </Card>
              </a>
            ))}
          </div>
        </div>
      </section>

'''

page = "app/[locale]/page.tsx"
s = open(page, encoding="utf-8").read()
if "Products Section" in s:
    raise SystemExit("Bereich ist schon eingebaut")
marker = "      {/* About Preview Section */}"
assert s.count(marker) == 1, "Marker in page.tsx nicht eindeutig"

header = "components/layout/Header.tsx"
h = open(header, encoding="utf-8").read()
nav_old = "    { href: `/${locale}/portfolio`, label: dictionary.nav.portfolio },\n"
assert h.count(nav_old) == 1, "Navigationszeile in Header.tsx nicht eindeutig"
assert h.count("      portfolio: string;\n") == 1, "Typ in Header.tsx nicht eindeutig"

for loc, t in T.items():
    p = f"locales/{loc}.json"
    d = json.load(open(p, encoding="utf-8"))
    d["nav"]["products"] = t["nav"]
    d["products"] = {"title": t["title"], "subtitle": t["subtitle"], "items": t["items"]}
    with open(p, "w", encoding="utf-8") as f:
        json.dump(d, f, ensure_ascii=False, indent=2)
        f.write("\n")

open(page, "w", encoding="utf-8").write(s.replace(marker, SECTION + marker))
h = h.replace("      portfolio: string;\n", "      portfolio: string;\n      products: string;\n")
h = h.replace(nav_old, nav_old + "    { href: `/${locale}#produkte`, label: dictionary.nav.products },\n")
open(header, "w", encoding="utf-8").write(h)
print("Produkte-Bereich eingebaut")
