# NuGet-Sicherheitsbefund, 2026-09-01

Stand: 2026-09-03. Die am 2026-09-01 gefundenen Luecken sind behoben, getestet und
produktiv ausgerollt.

## Ergebnis

Der Abruf

```powershell
dotnet list TrafagSalesExporter.sln package --vulnerable --include-transitive
```

gegen `https://api.nuget.org/v3/index.json` meldet nach der Aktualisierung fuer alle
sieben Projekte der Solution **keine anfaelligen Pakete** mehr.

| Paket | Alter Stand | Produktiver Stand | Umsetzung |
| --- | ---: | ---: | --- |
| `Microsoft.AspNetCore.Authentication.Negotiate` | `8.0.24` | entfernt | Die Anwendung verwendet IIS-Windows-Authentifizierung und kein `AddNegotiate`; DLL und Referenz fehlen produktiv. |
| `Microsoft.Bcl.Memory` | `9.0.0` | `9.0.14` | Sichere Version direkt aufgeloest. |
| `Microsoft.Kiota.Abstractions` | `1.17.1` | `1.22.0` | Sichere, mit Graph 5.80.0 kompatible Version direkt aufgeloest. |
| `SQLitePCLRaw.lib.e_sqlite3` | `2.1.6` | `2.1.12` | EF Core SQLite/Design und DeployConsole auf `8.0.30`; native SQLite-Kette dadurch auf `2.1.12`. |

Die zwei Negotiate-Advisories betreffen die Version `8.0.24`; beide sind ab `8.0.29`
behoben. Eine der beiden Luecken setzt LDAP-Rollenauflosung voraus. Die Anwendung verwendet
in `Program.cs` die IIS-Windows-Authentifizierung, ruft weder `AddNegotiate` auf noch
loest sie LDAP-Rollen auf. Die direkte Negotiate-Paketreferenz ist deshalb sehr wahrscheinlich
entbehrlich; das ist vor dem Entfernen per Build und Authentifizierungs-Smoke zu pruefen.

Die Kiota-Luecke kann bei hostuebergreifenden Redirects sensible Header weitergeben. Die
Graph-Verwendung liegt im SharePoint-Upload. `Microsoft.Graph 5.105.0` hebt zwar den
Graph-Core-Stand an, dessen Mindestversion fuer Kiota liegt aber noch bei `1.21.1`; ein
Graph-Patch allein ist daher kein ausreichender Nachweis. Entweder die Kiota-Pakete gezielt
auf einen sicheren, untereinander passenden Stand anheben oder das groessere Graph-6-Upgrade
mit Codepruefung durchfuehren.

Die SQLite-Advisory betrifft die mitgelieferte native SQLite-Version. EF Core SQLite `8.0.30`
fordert `SQLitePCLRaw.bundle_e_sqlite3 >= 2.1.12`; dieselbe Version ist auch im
`Tools/DeployConsole` fuer `Microsoft.Data.Sqlite` zu setzen. EF-Core- und Design-Paket
muessen dabei gemeinsam auf derselben `8.0.x`-Version bleiben.

## Umsetzungs- und Produktivnachweis

- Restore, Release-Build und **674/674** Tests sind gruen; der Build hat nur die bereits
  bekannten Warnungen.
- SQLite-Smoke mit nativer SQLite-Version `3.53.3` sowie echter SharePoint/Graph-
  Verbindungs-Smoke sind gruen.
- Funktionscommits `756e931` und `2444731`; produktiv deployed am 2026-09-03.
- Lokaler Referenz-Publish und Server sind fuer `BiDashboard.dll`, `BiDashboard.deps.json`,
  `Microsoft.Bcl.Memory.dll`, `Microsoft.Kiota.Abstractions.dll` und die native
  `e_sqlite3.dll` bitgleich. Die Server-`deps.json` nennt exakt BCL `9.0.14`, Kiota
  `1.22.0` und alle SQLitePCLRaw-Bausteine `2.1.12`; Negotiate kommt nicht mehr vor.
- Windows-Authentifizierung und alle geprueften Anwendungsrouten liefern HTTPS `200`.
- Vor dem finalen Publish wurde die gepruefte Blockkopie
  `trafag_exporter.db.before-purchasing-cache-security-20260903-081528.bak` erstellt;
  Produktivdatenbank und Sicherung sind je `358'772'736` Bytes gross.

Quellen: NuGet-Audit am 2026-09-01 sowie `GHSA-2p3q-h3hg-jcqq`,
`GHSA-8prm-248r-h957`, `GHSA-73j8-2gch-69rq`, `GHSA-7j59-v9qr-6fq9` und
`GHSA-2m69-gcr7-jv3q`.
