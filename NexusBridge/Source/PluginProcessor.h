#pragma once

#include <juce_audio_processors/juce_audio_processors.h>
#include <juce_audio_formats/juce_audio_formats.h>

#include "ApiClient.h"
#include "Auftrag.h"

/** Ein geladener Clip. Referenzgezaehlt, damit der Audiothread ihn halten kann,
    waehrend der Message-Thread bereits den naechsten vorbereitet. */
class ClipPuffer : public juce::ReferenceCountedObject
{
public:
    using Ptr = juce::ReferenceCountedObjectPtr<ClipPuffer>;

    ClipPuffer (juce::AudioBuffer<float>&& daten, double rate, juce::String beschriftung, juce::File quelle)
        : puffer (std::move (daten)), abtastrate (rate), name (std::move (beschriftung)), datei (std::move (quelle)) {}

    juce::AudioBuffer<float> puffer;
    double abtastrate { 44100.0 };
    juce::String name;
    juce::File datei;
};

class NexusBridgeProcessor : public juce::AudioProcessor,
                             private juce::Timer
{
public:
    NexusBridgeProcessor();
    ~NexusBridgeProcessor() override;

    //== AudioProcessor ==========================================================
    void prepareToPlay (double sampleRate, int samplesPerBlock) override;
    void releaseResources() override {}
    bool isBusesLayoutSupported (const BusesLayout& layouts) const override;
    void processBlock (juce::AudioBuffer<float>&, juce::MidiBuffer&) override;

    juce::AudioProcessorEditor* createEditor() override;
    bool hasEditor() const override { return true; }

    const juce::String getName() const override { return "Nexus Bridge"; }
    bool acceptsMidi() const override { return false; }
    bool producesMidi() const override { return false; }
    bool isMidiEffect() const override { return false; }
    double getTailLengthSeconds() const override { return 0.0; }

    int getNumPrograms() override { return 1; }
    int getCurrentProgram() override { return 0; }
    void setCurrentProgram (int) override {}
    const juce::String getProgramName (int) override { return {}; }
    void changeProgramName (int, const juce::String&) override {}

    void getStateInformation (juce::MemoryBlock&) override;
    void setStateInformation (const void*, int) override;

    //== Dienstanbindung =========================================================
    std::shared_ptr<ApiClient> api() const { return apiClient; }
    std::shared_ptr<AuftragsWaechter> waechter() const { return auftragsWaechter; }

    void speichereZugang (const ApiZugang& zugang);
    ApiZugang ladeZugang() const;
    bool zugangAusConfigEnv();

    juce::PropertiesFile& einstellungen() { return *ablage; }

    //== Clip und Wiedergabe =====================================================
    /** Laedt eine Audiodatei in den Abspielpuffer. Nur vom Message-Thread. */
    bool ladeClip (const juce::File& datei, const juce::String& beschriftung, juce::String& fehler);

    ClipPuffer::Ptr aktuellerClip() const;

    /** Datei fuer Ziehen und Sichern - auf die Projektrate umgerechnet.

        Der Dienst liefert 24000 Hz (Qwen3-TTS) beziehungsweise 22050 Hz
        (macOS-Stimmen). Cubase rechnet beim Ziehen nicht zwingend um und spielt
        so eine 24000er Datei in einem 48000er Projekt doppelt so schnell ab.
        Deshalb legt das Plugin eine Fassung in der Projektrate daneben. */
    juce::File aktuelleClipDatei() const;

    /** Ordner fuer die Dateien, die an die DAW gehen - bewusst nicht /tmp. */
    static juce::File arbeitsOrdner();

    void starteWiedergabe();
    void haltWiedergabe();
    bool laeuftWiedergabe() const noexcept { return spielt.load(); }

    /** Clip an die DAW-Wiedergabe koppeln - damit nimmt ein Bounce oder ein
        Freeze der Spur das Ergebnis auf. */
    bool transportKopplung() const noexcept { return anTransport.load(); }
    void setTransportKopplung (bool an);
    double wiedergabeAnteil() const noexcept { return anteil.load(); }

    //== Aufnahme von der Spur ===================================================
    /** Nimmt auf, was in das Plugin hineinlaeuft - also das Material der Spur,
        auf der es liegt. Geschrieben wird auf einem eigenen Thread; der
        Audiothread schiebt nur in eine Warteschlange. */
    bool starteAufnahme (juce::String& fehler);
    juce::File stoppAufnahme();
    bool nimmtAuf() const noexcept { return schreiber.load() != nullptr; }
    double aufnahmeSekunden() const noexcept;

    /** Text des zuletzt erzeugten Auftrags - nur zur Anzeige. */
    juce::String letzterAuftrag;

private:
    void timerCallback() override;

    std::shared_ptr<ApiClient> apiClient { std::make_shared<ApiClient>() };
    std::shared_ptr<AuftragsWaechter> auftragsWaechter { std::make_shared<AuftragsWaechter>() };
    std::unique_ptr<juce::PropertiesFile> ablage;

    juce::AudioFormatManager formate;

    // Uebergabe an den Audiothread: der Message-Thread haelt jeden Puffer in
    // `gehalten`, der Audiothread greift nur ueber `angefordert` zu.
    juce::ReferenceCountedArray<ClipPuffer> gehalten;
    std::atomic<ClipPuffer*> angefordert { nullptr };
    ClipPuffer::Ptr imAudiothread;

    void dawDateiPruefen();

    double leseposition { 0.0 };
    std::atomic<double> hostAbtastrate { 44100.0 };
    juce::File dawDatei;
    double dawRate { 0.0 };
    ClipPuffer* dawQuelle { nullptr };
    std::atomic<bool> spielt { false };
    std::atomic<bool> anTransport { false };
    bool hostSpielteZuletzt { false };
    std::atomic<bool> zuruecksetzen { false };
    std::atomic<double> anteil { 0.0 };

    juce::TimeSliceThread schreibThread { "NexusBridge Aufnahme" };
    std::unique_ptr<juce::AudioFormatWriter::ThreadedWriter> aufnahme;
    std::atomic<juce::AudioFormatWriter::ThreadedWriter*> schreiber { nullptr };
    juce::CriticalSection aufnahmeSperre;
    juce::File aufnahmeDatei;
    std::atomic<juce::int64> aufgenommeneSamples { 0 };
    std::atomic<int> aufnahmeKanaele { 0 };

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (NexusBridgeProcessor)
};
