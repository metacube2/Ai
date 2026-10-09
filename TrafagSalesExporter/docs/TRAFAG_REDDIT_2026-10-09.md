# Trafag Reddit (Forum mit Abstimmung)

Stand: 2026-10-09

Auftrag Ingo 2026-10-09: neuer Reiter „Trafag Reddit“ für alles, was in SharePoint keinen Platz
gefunden hat, mit Abstimmung wie bei Reddit, grafisch ansprechend, „das Beste aus Reddit und
StackOverflow“. Entscheide Ingo am selben Tag: **sofort für alle sichtbar**, Autor mit **echtem Namen
aus dem Windows-Login**, nach Tests **deployen**.

Hinweis zur Regel aus `docs/rag/LEARNINGS.md` (2026-10-06, Weltlage): neue Reiter erst nach Freigabe
des Fachabteilungschefs sichtbar machen. Hier hat Ingo die Sichtbarkeit ausdrücklich selbst
entschieden; eine Ankündigung (Intranet, Teams) ist damit **nicht** entschieden und wurde nicht gemacht.

## Funktionen

| Herkunft | Funktion |
|---|---|
| Reddit | Communities (r/slug) mit Farbe und Symbol, jeder Angemeldete kann eine anlegen |
| Reddit | Abstimmen hoch/runter, zweiter Klick nimmt zurück, keine Stimme für eigene Inhalte |
| Reddit | Sortierung Angesagt (Hot-Formel), Neu, Top mit Zeitraum, Aktiv, Kontrovers |
| Reddit | Verschachtelte Kommentare bis Tiefe 8, Faden-Linie zum Einklappen, „OP“-Marke, Speichern (Lesezeichen) |
| StackOverflow | Beitragsart Frage; die fragende Person akzeptiert eine direkte Antwort (grün, zuoberst) |
| StackOverflow | „Offene Fragen“, Tags (höchstens 5), Reputation mit Stufen, Ruhmeshalle |
| StackOverflow | Ähnliche Beiträge erscheinen beim Tippen des Titels |
| eigen | Live: offene Beitragsseiten laden bei Änderungen anderer nach, der Feed zeigt „Neues im Forum“ |
| eigen | Sicherer Markdown-Teil (HTML wird immer kodiert, Links nur http/https/mailto), Vorschau im Editor |

Rechenregeln (Hot, Wilson, Reputation, Stufen) stehen im Handbuchkapitel
`wwwroot/handbuch/kapitel/09_trafag_reddit.md` und im Code `Services/Forum/ForumRanking.cs`.

## Technik

| Was | Ort |
|---|---|
| Seite, Routen `/forum`, `/forum/r/{slug}`, `/forum/p/{id}`, `/forum/p/{id}/bearbeiten`, `/forum/u/{login}`, `/forum/gespeichert`, `/forum/neu` | `Components/Pages/Forum.razor` |
| Bausteine (Abstimmung, Karte, Kommentarbaum, Editor, Autor, Zeit, Art) | `Components/Forum/*.razor` |
| Service, Regeln, Reputation | `Services/Forum/ForumService.cs` |
| Markdown, Ranking, AD-Anzeigename, Live-Benachrichtigung | `Services/Forum/ForumMarkdown.cs`, `ForumRanking.cs`, `ForumUserDirectory.cs`, `ForumModels.cs` (`ForumNotifier`) |
| Tabellen `ForumCommunities`, `ForumPosts`, `ForumComments`, `ForumVotes`, `ForumBookmarks` | `Models/ForumModels.cs`, `Services/DatabaseInitializationService.SchemaSql.cs` (`GetForumCreateSql`, Indizes), `DatabaseSchemaMaintenanceService.EnsureForumTables` |
| Menü `trafag-reddit` (oberste Ebene, Sortierung 38, Icon `Forum`) | `Services/DatabaseSeedService.cs`, `Services/NavigationIconResolver.cs` |
| CSS (Präfix `frm-`) | `wwwroot/css/app.css` |
| Tests | `TrafagSalesExporter.Tests/ForumServiceTests.cs`, `ForumMarkdownTests.cs` |

- **Identität:** `User.Identity.Name` (IIS-Windows-Auth), Kurzname klein geschrieben als Login, Anzeigename
  per `DirectorySearcher` (`sAMAccountName` → `displayName`), einen Tag gemerkt, beim Schreiben im Beitrag
  gespeichert. Fällt AD aus, steht der Kurzname da.
- **Admin:** wer das Admin-Passwort entsperrt hat, darf anheften und fremde Beiträge/Kommentare löschen.
- **Erststart:** `EnsureDefaultsAsync` legt bei leerer Tabelle acht Communities und einen angehefteten
  Willkommensbeitrag an (Autor „Trafag Reddit“, Login `system`). Danach nie wieder.
- **Löschen** ist weich (`IsDeleted`); gelöschte Kommentare mit Antworten bleiben als „[gelöscht]“ stehen.
- **Doppelklick:** Stimmen und Lesezeichen laufen durch eine Schleuse (`SemaphoreSlim`), der Unique-Index
  `UX_ForumVotes_Target_Voter` hält trotzdem.
- **Aufrufe** zählen erst in der interaktiven Sitzung (`OnAfterRenderAsync`), nicht beim Vorab-Rendern.

## Stand

- 2026-10-09: gebaut, Commits `4d4693d` und `9efa491`, Release-Tests 1135/1135 im sauberen Worktree.
  Deploystatus siehe Abschnitt „Deploy“.

## Deploy

(wird nach dem Deploy nachgetragen)

## Offen

- Ankündigung im Intranet/Teams: nur auf Ingos Entscheid.
- Moderation über die Oberfläche (Communities umbenennen/archivieren) fehlt; bei Bedarf als Admin-Funktion.
- Keine Mail-Benachrichtigung bei Antworten.
