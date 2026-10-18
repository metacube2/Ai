#include "ApiClient.h"

namespace
{
    /** Der Dienst rechnet lange: TTS bis 30 Minuten, ACE-Step steht zusaetzlich
        in der gemeinsamen Warteschlange. Nur der Verbindungsaufbau bekommt
        deshalb ein knappes Zeitlimit, das Lesen selbst nicht. */
    constexpr int verbindungsZeitlimitMs = 8000;
}

void ApiClient::setZugang (const ApiZugang& neu)
{
    const juce::ScopedLock haltevorrichtung (sperre);
    zugang = neu;
}

ApiZugang ApiClient::getZugang() const
{
    const juce::ScopedLock haltevorrichtung (sperre);
    return zugang;
}

juce::URL ApiClient::basisUrl (const juce::String& pfad) const
{
    const auto z = getZugang();
    return juce::URL ("http://" + z.host + ":" + juce::String (z.port) + pfad);
}

juce::String ApiClient::kopfzeilen() const
{
    const auto z = getZugang();
    const auto roh = juce::Base64::toBase64 (z.benutzer + ":" + z.passwort);
    return "Authorization: Basic " + roh;
}

ApiClient::Antwort ApiClient::anfrage (const juce::String& pfad, const juce::var* koerper) const
{
    Antwort antwort;

    if (! getZugang().vollstaendig())
    {
        antwort.fehler = "Kein Zugang eingetragen";
        return antwort;
    }

    auto url = basisUrl (pfad);
    auto kopf = kopfzeilen();

    if (koerper != nullptr)
    {
        url = url.withPOSTData (juce::JSON::toString (*koerper));
        kopf += "\r\nContent-Type: application/json";
    }

    auto optionen = juce::URL::InputStreamOptions (koerper != nullptr ? juce::URL::ParameterHandling::inPostData
                                                                     : juce::URL::ParameterHandling::inAddress)
                        .withExtraHeaders (kopf)
                        .withConnectionTimeoutMs (verbindungsZeitlimitMs)
                        .withStatusCode (&antwort.status);

    auto strom = url.createInputStream (optionen);

    if (strom == nullptr)
    {
        antwort.fehler = "Dienst nicht erreichbar";
        return antwort;
    }

    const auto text = strom->readEntireStreamAsString();

    if (antwort.status == 401)
    {
        antwort.fehler = "Anmeldung abgelehnt";
        return antwort;
    }

    if (! juce::JSON::parse (text, antwort.daten).wasOk())
    {
        antwort.fehler = "Unlesbare Antwort (Status " + juce::String (antwort.status) + ")";
        return antwort;
    }

    // Der Dienst meldet Fehler als {"ok": false, "error": "..."} mit Status 200
    // oder 4xx - beides muss hier gleich behandelt werden.
    if (auto* objekt = antwort.daten.getDynamicObject())
    {
        if (objekt->hasProperty ("ok") && ! static_cast<bool> (objekt->getProperty ("ok")))
        {
            antwort.fehler = feldText (antwort.daten, "error");

            if (antwort.fehler.isEmpty())
                antwort.fehler = "Dienst meldet einen Fehler";

            return antwort;
        }
    }

    if (antwort.status >= 400)
    {
        antwort.fehler = "Status " + juce::String (antwort.status);
        return antwort;
    }

    antwort.ok = true;
    return antwort;
}

ApiClient::Antwort ApiClient::get (const juce::String& pfad) const
{
    return anfrage (pfad, nullptr);
}

ApiClient::Antwort ApiClient::post (const juce::String& pfad, const juce::var& koerper) const
{
    return anfrage (pfad, &koerper);
}

ApiClient::Antwort ApiClient::dateiHochladen (const juce::String& pfad, const juce::File& datei,
                                             const juce::String& feld, const juce::StringPairArray& felder) const
{
    Antwort antwort;

    if (! datei.existsAsFile())
    {
        antwort.fehler = "Datei nicht gefunden";
        return antwort;
    }

    auto url = basisUrl (pfad);

    for (const auto& name : felder.getAllKeys())
        url = url.withParameter (name, felder[name]);

    // Content-Type setzt JUCE fuer den multipart-Rumpf selbst - hier darf nur
    // die Anmeldung als zusaetzlicher Kopf dazu.
    url = url.withFileToUpload (feld, datei, "application/octet-stream");

    auto strom = url.createInputStream (juce::URL::InputStreamOptions (juce::URL::ParameterHandling::inPostData)
                                            .withExtraHeaders (kopfzeilen())
                                            .withConnectionTimeoutMs (verbindungsZeitlimitMs)
                                            .withStatusCode (&antwort.status));

    if (strom == nullptr)
    {
        antwort.fehler = "Dienst nicht erreichbar";
        return antwort;
    }

    const auto text = strom->readEntireStreamAsString();

    if (! juce::JSON::parse (text, antwort.daten).wasOk())
    {
        antwort.fehler = "Unlesbare Antwort (Status " + juce::String (antwort.status) + ")";
        return antwort;
    }

    if (auto* objekt = antwort.daten.getDynamicObject())
    {
        if (objekt->hasProperty ("ok") && ! static_cast<bool> (objekt->getProperty ("ok")))
        {
            antwort.fehler = feldText (antwort.daten, "error");
            return antwort;
        }
    }

    antwort.ok = antwort.status < 400;

    if (! antwort.ok)
        antwort.fehler = "Status " + juce::String (antwort.status);

    return antwort;
}

bool ApiClient::holeDatei (const juce::String& pfadMitQuery, const juce::String& lokalerPfad,
                           const juce::File& ziel, juce::String& fehler) const
{
    // Laeuft der Dienst auf demselben Rechner, ist der zurueckgemeldete
    // absolute Pfad der kuerzeste und verlustfreieste Weg.
    if (lokalerPfad.isNotEmpty())
    {
        const juce::File quelle (lokalerPfad);

        if (quelle.existsAsFile())
        {
            ziel.deleteFile();

            if (quelle.copyFileTo (ziel))
                return true;
        }
    }

    if (pfadMitQuery.isEmpty())
    {
        fehler = "Keine Quelle fuer die Audiodatei";
        return false;
    }

    int status = 0;
    auto strom = basisUrl (pfadMitQuery)
                     .createInputStream (juce::URL::InputStreamOptions (juce::URL::ParameterHandling::inAddress)
                                             .withExtraHeaders (kopfzeilen())
                                             .withConnectionTimeoutMs (verbindungsZeitlimitMs)
                                             .withStatusCode (&status));

    if (strom == nullptr || status >= 400)
    {
        fehler = "Download fehlgeschlagen (Status " + juce::String (status) + ")";
        return false;
    }

    ziel.deleteFile();
    juce::FileOutputStream ausgabe (ziel);

    if (! ausgabe.openedOk())
    {
        fehler = "Zieldatei nicht beschreibbar";
        return false;
    }

    ausgabe.writeFromInputStream (*strom, -1);
    ausgabe.flush();

    if (ziel.getSize() <= 0)
    {
        fehler = "Leere Audiodatei empfangen";
        return false;
    }

    return true;
}

bool ApiClient::ausConfigEnv (const juce::File& datei, ApiZugang& ziel)
{
    if (! datei.existsAsFile())
        return false;

    juce::StringArray zeilen;
    zeilen.addLines (datei.loadFileAsString());

    bool etwasGefunden = false;

    for (auto zeile : zeilen)
    {
        zeile = zeile.trim();

        if (zeile.isEmpty() || zeile.startsWithChar ('#') || ! zeile.containsChar ('='))
            continue;

        const auto schluessel = zeile.upToFirstOccurrenceOf ("=", false, false).trim();
        auto wert = zeile.fromFirstOccurrenceOf ("=", false, false).trim();
        wert = wert.unquoted();

        if (schluessel == "MEDIA_AI_API_PORT")       { ziel.port = wert.getIntValue(); etwasGefunden = true; }
        else if (schluessel == "MEDIA_AI_AUTH_USER")     { ziel.benutzer = wert; etwasGefunden = true; }
        else if (schluessel == "MEDIA_AI_AUTH_PASSWORD") { ziel.passwort = wert; etwasGefunden = true; }
    }

    return etwasGefunden;
}

juce::String ApiClient::feldText (const juce::var& daten, const juce::String& name)
{
    if (auto* objekt = daten.getDynamicObject())
        return objekt->getProperty (name).toString();

    return {};
}
