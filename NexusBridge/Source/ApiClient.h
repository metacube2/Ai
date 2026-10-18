#pragma once

#include <juce_core/juce_core.h>

/** Zugangsdaten fuer den Media-AI-Dienst auf Port 8013. */
struct ApiZugang
{
    juce::String host { "127.0.0.1" };
    int port { 8013 };
    juce::String benutzer;
    juce::String passwort;

    bool vollstaendig() const noexcept { return host.isNotEmpty() && port > 0 && benutzer.isNotEmpty(); }
};

/**
    Duenner HTTP-Anschluss an den Media-AI-Dienst.

    Alle Methoden blockieren und gehoeren deshalb ausschliesslich auf einen
    Hintergrundthread. Der Dienst antwortet auf Basic Auth; Rechte haengen an
    der Rolle des Kontos (Vorleser braucht "reader", Song braucht "song").
*/
class ApiClient
{
public:
    struct Antwort
    {
        bool ok { false };
        int status { 0 };
        juce::String fehler;
        juce::var daten;
    };

    ApiClient() = default;

    void setZugang (const ApiZugang& neu);
    ApiZugang getZugang() const;

    Antwort get (const juce::String& pfad) const;
    Antwort post (const juce::String& pfad, const juce::var& koerper) const;

    /** Laedt eine lokale Datei als multipart/form-data hoch. */
    Antwort dateiHochladen (const juce::String& pfad, const juce::File& datei,
                            const juce::String& feld, const juce::StringPairArray& felder) const;

    /** Laedt eine Datei des Dienstes. Liegt sie lokal bereits vor (der Dienst
        laeuft auf demselben Rechner), wird sie direkt kopiert. */
    bool holeDatei (const juce::String& pfadMitQuery, const juce::String& lokalerPfad,
                    const juce::File& ziel, juce::String& fehler) const;

    /** Liest Host, Port und Konto aus einer config.env des n8n-Automationsordners. */
    static bool ausConfigEnv (const juce::File& datei, ApiZugang& ziel);

    /** Bequemer Zugriff auf ein Feld einer JSON-Antwort. */
    static juce::String feldText (const juce::var& daten, const juce::String& name);

private:
    juce::URL basisUrl (const juce::String& pfad) const;
    juce::String kopfzeilen() const;
    Antwort anfrage (const juce::String& pfad, const juce::var* koerper) const;

    mutable juce::CriticalSection sperre;
    ApiZugang zugang;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (ApiClient)
};
