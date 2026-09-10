namespace TrafagSalesExporter.Models;

/// <summary>
/// Kuratiertes Namensmuster, das Kunden einem Marktsegment VORSCHLAEGT.
///
/// Vorschlag von Ingo, 2026-09-10: „man koennte so was machen im ersten Schritt
/// *deutsche bahn*, so was mit Regex ueber Namen." Hintergrund ist, dass das Segment
/// fachlich am Unternehmen haengt und nicht an der verkaufenden Gesellschaft. Deutsche
/// Bahn ist Bahn, gleich ob TRDE, TRFR oder TRIT die Rechnung stellt. Ueber die
/// Kundennummer laesst sich das nicht ausdruecken, weil jeder Standort eigene Nummern
/// fuehrt: von 24 bestaetigten deutschen Bahnkunden hat genau EINER einen exakten
/// Namenstreffer an einem anderen Standort.
///
/// Der Unterschied zum gescheiterten Abgleich mit der Marktumfrage ist nicht die
/// Technik, sondern die Herkunft der Muster. Dort wurde jeder Umfragename automatisch
/// gegen jeden Kundennamen gehalten; hier pflegt ein Mensch eine kurze Liste und
/// verantwortet sie. Gemessen am 2026-09-10: 12 kuratierte Muster finden rund 66
/// zusaetzliche Kunden ueber sechs Standorte, waehrend der Umfrageabgleich 172
/// Vorschlaege erzeugte, von denen bis heute genau einer bestaetigt wurde.
///
/// <para>
/// Die Grenze ist gemessen und bleibt bestehen: `siemens` faengt `SIEMENS ENERGY S.A`,
/// `abb` faengt `ABBOTT LABS US`, `voith` faengt `Voith Hydro`. Ein Muster ist deshalb
/// NIE eine Bestaetigung. Es erzeugt ausschliesslich Zeilen mit
/// <see cref="CustomerMarketSegment.IsConfirmed"/> = false, die ein Mensch auf der Seite
/// `/marktsegmente` bestaetigt oder verwirft.
/// </para>
/// </summary>
public class SegmentNamePattern
{
    public int Id { get; set; }

    /// <summary>
    /// Teilzeichenfolge, die im Kundennamen vorkommen muss. Gross- und Kleinschreibung
    /// spielt keine Rolle. Bewusst kein regulaerer Ausdruck: die Muster pflegen
    /// Fachanwender, und eine Teilzeichenfolge ist nachvollziehbar und kann keine
    /// Laufzeit sprengen.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>Marktsegment, das vorgeschlagen wird, zum Beispiel `Railway`.</summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Einschraenkung auf einen Standort. Leer heisst: gilt fuer alle. Damit
    /// laesst sich ein Muster zaehmen, das nur in einem Land eindeutig ist.
    /// </summary>
    public string Tsc { get; set; } = string.Empty;

    /// <summary>Inaktive Muster bleiben zur Nachvollziehbarkeit stehen, wirken aber nicht.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Warum es das Muster gibt, oder wovor es sich abgrenzt.</summary>
    public string Note { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }
}
