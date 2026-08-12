# RAG Admin

Stand: 2026-05-27

## Kurzstand

- Admin Bereich ist eigener Hauptmenuepunkt, nicht unter Finance.
- Route: `/admin/sessions`.
- Schutz: eigener App-interner Admin-Login ueber `AdminAccess`.
- Admin-Login ist unabhaengig vom Finance-Cockpit-Passwort.
- Admin Bereich darf nicht durch Finance-Cockpit-Login blockiert werden.

## Startseite

- Route `/` ist neutral und verlangt keinen Finance-Login.
- Landing Page nutzt Trafag-nahe Schrift, Manometer und optionales Strichmaennchen.
- Schalter fuer Strichmaennchen liegt im Admin Bereich.

## Rohquellen Nur Bei Bedarf

- Detaildoku: `docs/ADMIN_BEREICH_STARTSEITE_2026-05-21.md`

