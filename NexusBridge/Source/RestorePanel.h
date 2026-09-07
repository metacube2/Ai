#pragma once

#include <juce_gui_basics/juce_gui_basics.h>

#include "PluginProcessor.h"
#include "NexusLookAndFeel.h"

/** Zeichnet Spektren und die angewandte EQ-Kurve, Frequenzachse logarithmisch. */
class KurvenAnzeige : public juce::Component
{
public:
    struct Reihe
    {
        juce::String titel;
        juce::Colour farbe;
        juce::Array<juce::Point<float>> punkte;   // x = Hz, y = dB
        float staerke { 2.0f };
    };

    void setReihen (juce::Array<Reihe> neue);
    void leeren();

    void paint (juce::Graphics&) override;

private:
    juce::Array<Reihe> reihen;
};

/** Restaurieren-Tab: Frequenzgang uebertragen und entrauschen ueber /restore/*. */
class RestorePanel : public juce::Component,
                     private juce::Timer
{
public:
    explicit RestorePanel (NexusBridgeProcessor& p);
    ~RestorePanel() override;

    void paint (juce::Graphics&) override;
    void resized() override;

    void statusAbrufen();

private:
    struct Antwort
    {
        bool ok { false };
        juce::String fehler;
        juce::var daten;
        juce::String dateiPfad;
        juce::String name;
    };

    void timerCallback() override;
    void meldung (const juce::String& text, juce::Colour farbe);
    void arbeitAnzeigen (bool laeuft);
    void hochladen (bool alsReferenz);
    void clipHochladen();
    void aufnahmeUmschalten();
    void dateiHochladen (const juce::File& datei, bool alsReferenz, const juce::String& hinweis);
    void analysieren();
    void starten();
    void listenFuellen (const juce::var& eintraege);
    juce::Array<juce::Point<float>> punkteAus (const juce::var& liste) const;
    juce::String gewaehlt (const juce::ComboBox& box) const;

    NexusBridgeProcessor& prozessor;

    juce::Label quelleBeschriftung { {}, "Quelle" };
    juce::ComboBox quelleWahl;
    juce::Label referenzBeschriftung { {}, "Referenz (leer = nur entrauschen)" };
    juce::ComboBox referenzWahl;

    juce::TextButton hochladenKnopf { "Datei als Quelle …" };
    juce::TextButton referenzKnopf { "Datei als Referenz …" };
    juce::TextButton clipKnopf { "Aktuellen Clip als Quelle" };
    juce::TextButton aufnahmeKnopf { "Von der Spur aufnehmen" };

    juce::Label limitBeschriftung { {}, "Grenze dB" };
    juce::Slider limitRegler;
    juce::Label staerkeBeschriftung { {}, "Stärke" };
    juce::Slider staerkeRegler;
    juce::ToggleButton entrauschenKnopf { "Vorher entrauschen (DeepFilterNet3)" };
    juce::ToggleButton superresKnopf { "Superresolution (AudioSR, 48 kHz)" };
    juce::ComboBox srModellWahl;
    juce::Label schritteBeschriftung { {}, "Schritte" };
    juce::Slider schritteRegler;

    juce::TextButton startKnopf { "Bearbeiten" };
    juce::TextButton analyseKnopf { "Nur Spektrum" };
    juce::Label statusZeile;
    double fortschritt { -1.0 };
    juce::ProgressBar balken { fortschritt };

    KurvenAnzeige kurve;
    juce::var eingaben;
    std::unique_ptr<juce::FileChooser> dateiwahl;
    bool arbeitLaeuft { false };

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (RestorePanel)
};
