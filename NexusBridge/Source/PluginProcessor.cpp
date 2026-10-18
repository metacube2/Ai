#include "PluginProcessor.h"
#include "PluginEditor.h"

using namespace juce;

namespace
{
    const char* schluesselHost      = "host";
    const char* schluesselPort      = "port";
    const char* schluesselBenutzer  = "benutzer";
    const char* schluesselPasswort  = "passwort";

    File standardConfigEnv()
    {
        return File ("/Users/metacube/Projects/Git/n8n/automation/config.env");
    }

    /** Rechnet einen Puffer auf eine andere Abtastrate um. */
    AudioBuffer<float> umgerechnet (const AudioBuffer<float>& quelle, double quellRate, double zielRate)
    {
        const auto verhaeltnis = quellRate / zielRate;
        const auto zielLaenge = (int) std::floor ((double) quelle.getNumSamples() / verhaeltnis);

        AudioBuffer<float> ziel (quelle.getNumChannels(), jmax (1, zielLaenge));
        ziel.clear();

        for (int kanal = 0; kanal < quelle.getNumChannels(); ++kanal)
        {
            LagrangeInterpolator umsetzer;
            umsetzer.reset();
            umsetzer.process (verhaeltnis, quelle.getReadPointer (kanal), ziel.getWritePointer (kanal), zielLaenge);
        }

        return ziel;
    }
}

NexusBridgeProcessor::NexusBridgeProcessor()
    : AudioProcessor (BusesProperties().withInput  ("Eingang", AudioChannelSet::stereo(), true)
                                       .withOutput ("Ausgang", AudioChannelSet::stereo(), true))
{
    formate.registerBasicFormats();

    PropertiesFile::Options optionen;
    optionen.applicationName     = "NexusBridge";
    optionen.filenameSuffix      = "settings";
    optionen.folderName          = "Metacube";
    optionen.osxLibrarySubFolder = "Application Support";
    ablage = std::make_unique<PropertiesFile> (optionen);

    auto zugang = ladeZugang();

    if (! zugang.vollstaendig())
        zugangAusConfigEnv();
    else
        apiClient->setZugang (zugang);

    anTransport.store (ablage->getBoolValue ("transport_kopplung", false));

    startTimer (1500);
}

bool NexusBridgeProcessor::starteAufnahme (String& fehler)
{
    if (nimmtAuf())
        return true;

    const auto kanaele = jmax (1, getTotalNumInputChannels());
    const auto rate = hostAbtastrate.load();
    auto datei = arbeitsOrdner().getChildFile ("spur_" + Time::getCurrentTime().formatted ("%Y%m%d_%H%M%S") + ".wav");
    datei.deleteFile();

    std::unique_ptr<FileOutputStream> strom (datei.createOutputStream());

    if (strom == nullptr)
    {
        fehler = "Aufnahmedatei nicht beschreibbar";
        return false;
    }

    WavAudioFormat wav;
    std::unique_ptr<AudioFormatWriter> ziel (
        wav.createWriterFor (strom.get(), rate, (unsigned int) kanaele, 24, {}, 0));

    if (ziel == nullptr)
    {
        fehler = "Kein WAV-Schreiber";
        return false;
    }

    strom.release();

    if (! schreibThread.isThreadRunning())
        schreibThread.startThread (Thread::Priority::normal);

    // Puffer fuer gut zwei Sekunden: der Audiothread darf nie auf die Platte
    // warten, und mehr braucht der Schreibthread nicht als Vorlauf.
    auto neuer = std::make_unique<AudioFormatWriter::ThreadedWriter> (ziel.release(), schreibThread,
                                                                     (int) (rate * 2.0));
    aufgenommeneSamples.store (0);
    aufnahmeKanaele.store (kanaele);
    aufnahmeDatei = datei;

    {
        const ScopedLock haltevorrichtung (aufnahmeSperre);
        aufnahme = std::move (neuer);
        schreiber.store (aufnahme.get());
    }

    return true;
}

File NexusBridgeProcessor::stoppAufnahme()
{
    {
        const ScopedLock haltevorrichtung (aufnahmeSperre);
        schreiber.store (nullptr);
    }

    // Erst nach dem Abmelden freigeben: der Audiothread fasst den Zeiger
    // danach nicht mehr an, und der Destruktor schreibt den Rest weg.
    aufnahme.reset();
    return aufnahmeDatei;
}

double NexusBridgeProcessor::aufnahmeSekunden() const noexcept
{
    const auto rate = hostAbtastrate.load();
    return rate > 0.0 ? (double) aufgenommeneSamples.load() / rate : 0.0;
}

NexusBridgeProcessor::~NexusBridgeProcessor()
{
    stopTimer();
    stoppAufnahme();
    schreibThread.stopThread (2000);

    // Laufende HTTP-Auftraege duerfen die DAW nicht aufhalten: sie werden
    // abgemeldet, ihr Ergebnis faellt anschliessend ins Leere.
    auftragsWaechter->abmelden();

    angefordert.store (nullptr);
    imAudiothread = nullptr;
    gehalten.clear();
}

//== Zugang ======================================================================

void NexusBridgeProcessor::speichereZugang (const ApiZugang& zugang)
{
    apiClient->setZugang (zugang);

    ablage->setValue (schluesselHost, zugang.host);
    ablage->setValue (schluesselPort, zugang.port);
    ablage->setValue (schluesselBenutzer, zugang.benutzer);
    ablage->setValue (schluesselPasswort, zugang.passwort);
    ablage->saveIfNeeded();
}

ApiZugang NexusBridgeProcessor::ladeZugang() const
{
    ApiZugang zugang;
    zugang.host     = ablage->getValue (schluesselHost, "127.0.0.1");
    zugang.port     = ablage->getIntValue (schluesselPort, 8013);
    zugang.benutzer = ablage->getValue (schluesselBenutzer, "");
    zugang.passwort = ablage->getValue (schluesselPasswort, "");
    return zugang;
}

bool NexusBridgeProcessor::zugangAusConfigEnv()
{
    auto zugang = ladeZugang();

    if (! ApiClient::ausConfigEnv (standardConfigEnv(), zugang))
        return false;

    speichereZugang (zugang);
    return true;
}

//== Clip ========================================================================

bool NexusBridgeProcessor::ladeClip (const File& datei, const String& beschriftung, String& fehler)
{
    std::unique_ptr<AudioFormatReader> leser (formate.createReaderFor (datei));

    if (leser == nullptr)
    {
        fehler = "Audiodatei nicht lesbar";
        return false;
    }

    const auto laenge = (int) jmin ((int64) (leser->sampleRate * 60.0 * 30.0), leser->lengthInSamples);

    if (laenge <= 0)
    {
        fehler = "Audiodatei ist leer";
        return false;
    }

    AudioBuffer<float> puffer ((int) jmax (1u, jmin (2u, leser->numChannels)), laenge);
    leser->read (&puffer, 0, laenge, 0, true, leser->numChannels > 1);

    ClipPuffer::Ptr neuer = new ClipPuffer (std::move (puffer), leser->sampleRate, beschriftung, datei);

    gehalten.add (neuer);
    zuruecksetzen.store (true);
    angefordert.store (neuer.get());
    anteil.store (0.0);

    dawDateiPruefen();

    return true;
}

File NexusBridgeProcessor::arbeitsOrdner()
{
    // Nicht /tmp: eine gezogene Datei bleibt in Cubase eine Referenz, solange
    // "ins Projekt kopieren" nicht eingeschaltet ist. Aus /tmp waere sie
    // irgendwann weg.
    auto ordner = File::getSpecialLocation (File::userMusicDirectory).getChildFile ("NexusBridge");
    ordner.createDirectory();
    return ordner;
}

void NexusBridgeProcessor::dawDateiPruefen()
{
    auto clip = aktuellerClip();

    if (clip == nullptr)
    {
        dawDatei = File();
        dawQuelle = nullptr;
        return;
    }

    const auto ziel = hostAbtastrate.load();

    if (dawQuelle == clip.get() && approximatelyEqual (dawRate, ziel) && dawDatei.existsAsFile())
        return;

    dawQuelle = clip.get();
    dawRate = ziel;

    // Gleiche Rate: die Originaldatei genuegt.
    if (approximatelyEqual (clip->abtastrate, ziel))
    {
        dawDatei = clip->datei;
        return;
    }

    const auto name = clip->datei.getFileNameWithoutExtension()
                        + "_" + String (roundToInt (ziel / 1000.0)) + "k.wav";
    auto ausgabe = arbeitsOrdner().getChildFile (File::createLegalFileName (name));

    const auto puffer = umgerechnet (clip->puffer, clip->abtastrate, ziel);

    ausgabe.deleteFile();
    std::unique_ptr<FileOutputStream> strom (ausgabe.createOutputStream());

    if (strom == nullptr)
    {
        dawDatei = clip->datei;
        return;
    }

    WavAudioFormat wav;
    std::unique_ptr<AudioFormatWriter> schreiber (
        wav.createWriterFor (strom.get(), ziel, (unsigned int) puffer.getNumChannels(), 24, {}, 0));

    if (schreiber == nullptr)
    {
        dawDatei = clip->datei;
        return;
    }

    strom.release();
    schreiber->writeFromAudioSampleBuffer (puffer, 0, puffer.getNumSamples());
    schreiber.reset();

    dawDatei = ausgabe;
}

ClipPuffer::Ptr NexusBridgeProcessor::aktuellerClip() const
{
    return ClipPuffer::Ptr (angefordert.load());
}

File NexusBridgeProcessor::aktuelleClipDatei() const
{
    if (dawDatei.existsAsFile())
        return dawDatei;

    if (auto clip = aktuellerClip())
        return clip->datei;

    return {};
}

void NexusBridgeProcessor::starteWiedergabe()
{
    if (angefordert.load() == nullptr)
        return;

    zuruecksetzen.store (true);
    spielt.store (true);
}

void NexusBridgeProcessor::haltWiedergabe()
{
    spielt.store (false);
    zuruecksetzen.store (true);
    anteil.store (0.0);
}

void NexusBridgeProcessor::setTransportKopplung (bool an)
{
    anTransport.store (an);
    ablage->setValue ("transport_kopplung", an);
    ablage->saveIfNeeded();
}

void NexusBridgeProcessor::timerCallback()
{
    // Die Projektrate kann sich aendern, dann muss die Datei fuer die DAW neu.
    dawDateiPruefen();

    // Puffer freigeben, die weder angefordert sind noch vom Audiothread
    // gehalten werden. getObjectPointer zaehlt nicht mit, Referenzzahl 1
    // heisst also: nur noch das Array haelt ihn.
    for (int i = gehalten.size(); --i >= 0;)
    {
        auto puffer = gehalten.getObjectPointer (i);

        if (puffer != nullptr && puffer->getReferenceCount() == 1 && puffer != angefordert.load())
            gehalten.remove (i);
    }
}

//== Audio =======================================================================

void NexusBridgeProcessor::prepareToPlay (double sampleRate, int)
{
    hostAbtastrate.store (sampleRate > 0.0 ? sampleRate : 44100.0);
    leseposition = 0.0;
}

bool NexusBridgeProcessor::isBusesLayoutSupported (const BusesLayout& layouts) const
{
    const auto ausgang = layouts.getMainOutputChannelSet();

    if (ausgang != AudioChannelSet::mono() && ausgang != AudioChannelSet::stereo())
        return false;

    const auto eingang = layouts.getMainInputChannelSet();

    return eingang == ausgang || eingang == AudioChannelSet::disabled();
}

void NexusBridgeProcessor::processBlock (AudioBuffer<float>& buffer, MidiBuffer&)
{
    ScopedNoDenormals schutz;

    // Vor allem anderen: das, was hereinkommt, ist das Material der Spur.
    // Danach mischt die Wiedergabe ihren Clip dazu und wuerde sich selbst
    // mitschneiden.
    if (auto* mitschnitt = schreiber.load())
    {
        // Nur schreiben, wenn der Block mindestens so viele Kanaele hat wie der
        // Schreiber erwartet - sonst laese er ueber das Ende hinaus.
        if (buffer.getNumChannels() >= aufnahmeKanaele.load())
        {
            mitschnitt->write (buffer.getArrayOfReadPointers(), buffer.getNumSamples());
            aufgenommeneSamples.fetch_add (buffer.getNumSamples());
        }
    }

    for (int kanal = getTotalNumInputChannels(); kanal < getTotalNumOutputChannels(); ++kanal)
        buffer.clear (kanal, 0, buffer.getNumSamples());

    // Uebergabe vom Message-Thread: nur Zeigervergleich, keine Sperre.
    auto* gewuenscht = angefordert.load();

    if (gewuenscht != imAudiothread.get())
    {
        imAudiothread = ClipPuffer::Ptr (gewuenscht);
        leseposition = 0.0;
    }

    // An der DAW-Wiedergabe haengen: der Clip startet mit dem Transport, damit
    // ein Bounce oder ein Freeze der Spur ihn aufnimmt.
    if (anTransport.load())
    {
        if (auto* kopf = getPlayHead())
        {
            if (const auto lage = kopf->getPosition())
            {
                const auto hostSpielt = lage->getIsPlaying();

                if (hostSpielt != hostSpielteZuletzt)
                {
                    hostSpielteZuletzt = hostSpielt;

                    if (hostSpielt && imAudiothread != nullptr)
                    {
                        leseposition = 0.0;
                        spielt.store (true);
                    }
                    else if (! hostSpielt)
                    {
                        spielt.store (false);
                    }
                }
            }
        }
    }

    if (zuruecksetzen.exchange (false))
        leseposition = 0.0;

    if (! spielt.load() || imAudiothread == nullptr)
        return;

    const auto& quelle = imAudiothread->puffer;
    const auto quellLaenge = quelle.getNumSamples();
    const auto quellKanaele = quelle.getNumChannels();

    if (quellLaenge <= 0 || quellKanaele <= 0)
        return;

    const auto schritt = imAudiothread->abtastrate / hostAbtastrate.load();
    const auto zielKanaele = buffer.getNumChannels();
    const auto bloecke = buffer.getNumSamples();

    for (int i = 0; i < bloecke; ++i)
    {
        if (leseposition >= (double) (quellLaenge - 1))
        {
            spielt.store (false);
            anteil.store (1.0);
            leseposition = 0.0;
            break;
        }

        const auto index = (int) leseposition;
        const auto bruch = (float) (leseposition - (double) index);

        for (int kanal = 0; kanal < zielKanaele; ++kanal)
        {
            const auto* daten = quelle.getReadPointer (jmin (kanal, quellKanaele - 1));
            const auto wert = daten[index] + bruch * (daten[index + 1] - daten[index]);
            buffer.addSample (kanal, i, wert);
        }

        leseposition += schritt;
    }

    if (spielt.load())
        anteil.store (jlimit (0.0, 1.0, leseposition / (double) quellLaenge));
}

//== Zustand =====================================================================

void NexusBridgeProcessor::getStateInformation (MemoryBlock& ziel)
{
    ValueTree baum ("NexusBridge");
    baum.setProperty ("text", letzterAuftrag, nullptr);

    MemoryOutputStream strom (ziel, false);
    baum.writeToStream (strom);
}

void NexusBridgeProcessor::setStateInformation (const void* daten, int groesse)
{
    if (daten == nullptr || groesse <= 0)
        return;

    const auto baum = ValueTree::readFromData (daten, (size_t) groesse);

    if (baum.isValid() && baum.hasType ("NexusBridge"))
        letzterAuftrag = baum.getProperty ("text").toString();
}

AudioProcessorEditor* NexusBridgeProcessor::createEditor()
{
    return new NexusBridgeEditor (*this);
}

//== Einsprungpunkt ==============================================================

juce::AudioProcessor* JUCE_CALLTYPE createPluginFilter()
{
    return new NexusBridgeProcessor();
}
