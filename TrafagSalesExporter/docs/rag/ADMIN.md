# RAG Admin

Stand: 2026-11-12 (Doku-Pruefung: aktueller Kurzstand vorangestellt, der Stand vom 2026-05-27 darunter als in Teilen ueberholt markiert); davor 2026-05-27

## Kurzstand 2026-11-12

Zusammengestellt bei der Doku-Pruefung aus den genannten Detaildateien und dem Code (`Services/DatabaseSeedService.cs`,
`@page`-Routen), nicht auf dem Server nachgesehen. Passwoerter und Hashes stehen bewusst in keiner Doku-Datei.

- **Admin Bereich ist eine eigene Menuegruppe auf oberster Ebene** (seit 2026-08-11,
  `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`), nicht unter Finance. Eintraege: Aktive Logins (`/admin/sessions`),
  Nutzung (`/admin/nutzung`, seit 2026-10-14, anonyme Nutzungsmessung, `docs/NUTZUNG_2026-10-14.md`), Passwoerter
  (`/admin/passwoerter`, seit 2026-10-15), Standorte, Transformationen, Finance Regeln, Settings, Menuestruktur
  (`/admin/menu-structure`), Logs.
- **Schutz:** eigener App-interner Admin-Login ueber `AdminAccess`, unabhaengig vom Finance-Cockpit-Passwort; der Admin Bereich
  darf nicht durch den Finance-Login blockiert werden (unveraendert seit Mai).
- **Passwoerter aller Bereichs-Logins** (Admin, Finance, HR, Operations, Logistik) setzt der Admin auf der Seite Admin Bereich >
  Passwoerter. Die Hashes liegen seit `e1afd49` (2026-10-15) in der Cockpit-DB (Tabelle `AccessPasswords`) und haben Vorrang;
  `appsettings.json` ist nur noch Rueckfall. Vorher setzte jeder Deploy den Hash aus dem Repository zurueck
  (`docs/rag/DEPLOYMENT.md`, Falle Admin-Schalter, Nachtrag 2026-10-15).
- **Operations: Schalter „Login noetig“** auf derselben Seite (seit 2026-10-15, Tabelle `AccessSettings`), hat Vorrang vor
  `Shopfloor:OpenForAll` (`docs/SHOPFLOOR_2026-10-07.md` Abschnitt 3).
- **Startseite `/`** ist neutral und verlangt keinen Login. Sie zeigt seit 2026-10-14 die animierte Standortgrafik
  (`Components/Home/SiteNetworkGraphic.razor`, ersetzt `mainimage.png`). Der Manometer ist standardmaessig aus; sein Schalter
  liegt im Admin und in der DB (`ExportSettings.ShowLandingManometer`) und uebersteht Deploys. Der Schalter fuer das
  Strichmaennchen (`LandingPage:ShowWalkingLabFigure`) schreibt dagegen weiter in `appsettings.json` und ueberlebt den naechsten
  Deploy nicht (`docs/rag/DEPLOYMENT.md`, Falle Admin-Schalter).

## Kurzstand vom 2026-05-27

*Ueberholt in Teilen (markiert 2026-11-12): Es gibt nicht mehr nur die Route `/admin/sessions`, und die Startseite zeigt die
Standortgrafik statt des Manometers als Vorgabe; siehe „Kurzstand 2026-11-12“ oben. Die Aussagen zum eigenen Admin-Login gelten
weiter.*

- Admin Bereich ist eigener Hauptmenuepunkt, nicht unter Finance.
- Route: `/admin/sessions`.
- Schutz: eigener App-interner Admin-Login ueber `AdminAccess`.
- Admin-Login ist unabhaengig vom Finance-Cockpit-Passwort.
- Admin Bereich darf nicht durch Finance-Cockpit-Login blockiert werden.

## Startseite (Stand 2026-05-27)

- Route `/` ist neutral und verlangt keinen Finance-Login.
- Landing Page nutzt Trafag-nahe Schrift, Manometer und optionales Strichmaennchen. *(Ueberholt am 2026-10-14: Standortgrafik,
  Manometer standardmaessig aus, siehe „Kurzstand 2026-11-12“ oben.)*
- Schalter fuer Strichmaennchen liegt im Admin Bereich.

## Rohquellen Nur Bei Bedarf

- Detaildoku: `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md` (Stand Mai 2026, in Teilen ueberholt)
- Zusammenfuehrung der Admin-Menues: `docs/ADMIN_MENUE_ZUSAMMENFUEHRUNG_2026-08-11.md`
- Nutzungsmessung: `docs/NUTZUNG_2026-10-14.md`
- Arbeiten 14./15.10. (Startseite, Passwoerter in der DB, Manometer-Schalter): `docs/ARBEITSSTAND_2026-10-14_15.md`

