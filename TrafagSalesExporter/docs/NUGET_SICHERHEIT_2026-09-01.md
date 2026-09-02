# NuGet-Sicherheitsbefund, 2026-09-01

Stand: 2026-09-01. Dieser Bericht beschreibt den lokalen Quell- und Restore-Stand,
nicht einen neu ausgerollten Produktivstand.

## Ergebnis

Der Abruf

```powershell
dotnet list TrafagSalesExporter.sln package --vulnerable --include-transitive
```

gegen `https://api.nuget.org/v3/index.json` meldet fuenf Advisories mit Schweregrad
`High` ueber vier Pakete. Es wurde **nichts** an Projektdateien, Paketen, Datenbank,
Tests oder Produktivserver geaendert.

| Paket | Aufgeloeste Version | Pfad | Advisories | Korrektur |
| --- | ---: | --- | ---: | --- |
| `Microsoft.AspNetCore.Authentication.Negotiate` | `8.0.24` | direkte Referenz der Webanwendung | 2 | entfernen oder auf `8.0.30` anheben |
| `Microsoft.Bcl.Memory` | `9.0.0` | `Microsoft.Graph 5.80.0` -> `Microsoft.Graph.Core 3.2.4` -> IdentityModel | 1 | mindestens `9.0.14` aufloesen |
| `Microsoft.Kiota.Abstractions` | `1.17.1` | `Microsoft.Graph 5.80.0` -> `Microsoft.Graph.Core 3.2.4` | 1 | mindestens `1.22.0` aufloesen |
| `SQLitePCLRaw.lib.e_sqlite3` | `2.1.6` | EF Core SQLite bzw. DeployConsole | 1 | mindestens `2.1.12` aufloesen |

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

## Empfohlene Umsetzung

1. Direkte Negotiate-Referenz entfernen; falls sie doch benoetigt wird, `8.0.30` verwenden.
2. `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design` und
   `Microsoft.Data.Sqlite` im DeployConsole gemeinsam auf `8.0.30` setzen.
3. Die Graph-/Kiota-Kette gezielt auf sichere Versionen aktualisieren; keine ungetestete
   automatische Major-Aktualisierung.
4. Danach `dotnet restore`, NuGet-Audit, Release-Build, gesamte Test-Suite sowie SQLite- und
   SharePoint-Smoke ausfuehren. Erst anschliessend ist ein neuer Produktivdeploy zu bewerten.

Quellen: NuGet-Audit am 2026-09-01 sowie `GHSA-2p3q-h3hg-jcqq`,
`GHSA-8prm-248r-h957`, `GHSA-73j8-2gch-69rq`, `GHSA-7j59-v9qr-6fq9` und
`GHSA-2m69-gcr7-jv3q`.
