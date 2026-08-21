using MudBlazor;

namespace TrafagSalesExporter.Services;

/// <summary>Eine festgehaltene Meldung der Anwendung.</summary>
/// <param name="Message">Der Meldungstext, wie er als Snackbar erschienen ist.</param>
/// <param name="Severity">Einstufung von MudBlazor.</param>
/// <param name="At">
/// Zeitpunkt der Beobachtung. MudBlazor fuehrt an einer Meldung KEINEN Zeitstempel (an
/// `MudBlazor.Snackbar` gemessen: nur `Message` und `Severity`), deshalb wird er hier
/// gesetzt, sobald die Meldung entdeckt wird.
/// </param>
public sealed record AppNotification(string Message, Severity Severity, DateTime At);

/// <summary>
/// Sammelstelle fuer alle Meldungen der Anwendung, damit sie nicht mit der Snackbar
/// verschwinden.
/// </summary>
public interface IAppNotificationCenter
{
    /// <summary>Alle festgehaltenen Meldungen, neueste zuerst.</summary>
    IReadOnlyList<AppNotification> Notifications { get; }

    /// <summary>Anzahl der Meldungen, die der Benutzer noch nicht angesehen hat.</summary>
    int UnseenCount { get; }

    /// <summary>
    /// Schwerste Einstufung unter den ungesehenen Meldungen. Bestimmt die Ampelfarbe.
    /// Ohne ungesehene Meldungen <c>Severity.Success</c>, also gruen.
    /// </summary>
    Severity WorstUnseen { get; }

    /// <summary>Wird bei jeder Aenderung ausgeloest, damit die Ampel neu zeichnet.</summary>
    event Action? Changed;

    /// <summary>
    /// Haelt eine Meldung fest. Mehrfach dieselbe Instanz zu melden ist unschaedlich, das
    /// filtert der Aufrufer ueber die Instanzverfolgung.
    /// </summary>
    void Record(string message, Severity severity);

    /// <summary>Setzt den Ungesehen-Zaehler zurueck; die Liste bleibt.</summary>
    void MarkAllSeen();

    /// <summary>Leert die Liste vollstaendig.</summary>
    void Clear();
}

/// <summary>
/// Speichert die Meldungen im Arbeitsspeicher der Sitzung.
///
/// WARUM DIESE KLASSE EXISTIERT: Die Anwendung zeigt Meldungen an 129 Stellen ueber
/// `ISnackbar`. Eine Snackbar verschwindet nach wenigen Sekunden; wer gerade nicht
/// hinsieht, verpasst sie ersatzlos. Ingo hat am 2026-08-21 eine Ampel in der Kopfleiste
/// gewuenscht, die den Zustand dauerhaft zeigt und die Meldungen nachlesbar haelt.
///
/// WARUM SCOPED UND NICHT SINGLETON: `ISnackbar` ist bei MudBlazor pro Sitzung registriert,
/// und Meldungen entstehen aus den Aktionen EINES Benutzers. Ein Singleton wuerde die
/// Meldungen aller Benutzer vermischen und damit fremde Vorgaenge sichtbar machen.
///
/// KEINE PERSISTENZ, BEWUSST: Nach einem Neustart der Anwendung ist die Liste leer. Wer
/// Meldungen dauerhaft braucht, findet sie im Anwendungsprotokoll unter Admin, Logs. Die
/// Ampel ist eine Sofortanzeige, kein zweites Protokoll.
/// </summary>
public sealed class AppNotificationCenter : IAppNotificationCenter
{
    /// <summary>
    /// Obergrenze der Liste. Ein Vollimport oder ein langer Lauf kann viele Meldungen
    /// erzeugen; ohne Grenze waechst die Liste ueber die Lebensdauer der Sitzung
    /// unbegrenzt. Aelteste fallen heraus.
    /// </summary>
    public const int MaxEntries = 200;

    private readonly List<AppNotification> _entries = [];
    private readonly object _gate = new();
    private int _unseen;

    public event Action? Changed;

    public IReadOnlyList<AppNotification> Notifications
    {
        get
        {
            lock (_gate)
            {
                // Kopie, damit ein gleichzeitiges `Record` beim Aufzaehlen in der
                // Oberflaeche nicht in eine Ausnahme laeuft.
                return _entries.ToList();
            }
        }
    }

    public int UnseenCount
    {
        get { lock (_gate) { return _unseen; } }
    }

    public Severity WorstUnseen
    {
        get
        {
            lock (_gate)
            {
                if (_unseen == 0)
                    return Severity.Success;

                // Nur die ungesehenen betrachten. Die liegen vorn, weil neueste zuerst
                // einsortiert werden.
                var worst = Severity.Success;
                foreach (var entry in _entries.Take(_unseen))
                {
                    if (Rank(entry.Severity) > Rank(worst))
                        worst = entry.Severity;
                }

                return worst;
            }
        }
    }

    public void Record(string message, Severity severity)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        lock (_gate)
        {
            _entries.Insert(0, new AppNotification(message.Trim(), severity, DateTime.Now));
            _unseen++;

            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);

            // Der Zaehler darf die Liste nie ueberschreiten, sonst zeigte die Ampel mehr
            // ungesehene Meldungen an, als ueberhaupt nachlesbar sind.
            if (_unseen > _entries.Count)
                _unseen = _entries.Count;
        }

        Changed?.Invoke();
    }

    public void MarkAllSeen()
    {
        lock (_gate)
        {
            if (_unseen == 0)
                return;
            _unseen = 0;
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (_entries.Count == 0 && _unseen == 0)
                return;
            _entries.Clear();
            _unseen = 0;
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Rangfolge fuer die Ampel. `Normal` und `Info` sind blosse Hinweise und stehen
    /// deshalb nicht ueber `Success`; die Ampel soll nur bei echten Auffaelligkeiten von
    /// Gruen weggehen.
    /// </summary>
    internal static int Rank(Severity severity) => severity switch
    {
        Severity.Error => 3,
        Severity.Warning => 2,
        _ => 1
    };
}
