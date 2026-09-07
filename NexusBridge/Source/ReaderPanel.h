#pragma once

#include <juce_gui_basics/juce_gui_basics.h>

#include "PluginProcessor.h"
#include "NexusLookAndFeel.h"

/** Zeichnet den geladenen Clip und dient zugleich als Ziehflaeche in die DAW. */
class WellenAnzeige : public juce::Component,
                      private juce::Timer
{
public:
    explicit WellenAnzeige (NexusBridgeProcessor& p);

    void paint (juce::Graphics&) override;
    void mouseDrag (const juce::MouseEvent&) override;
    void mouseEnter (const juce::MouseEvent&) override;
    void mouseExit (const juce::MouseEvent&) override;

    void neuBerechnen();

private:
    void timerCallback() override;

    NexusBridgeProcessor& prozessor;
    juce::Array<juce::Range<float>> huellkurve;
    juce::String beschriftung;
    bool zeigerDrueber { false };
    bool ziehtSchon { false };
    double letzterAnteil { -1.0 };
};

/** Sichtbarer Griff, mit dem die fertige Datei in eine Spur gezogen wird.

    Ein Plugin kann kein Audio in die Anordnung der DAW schreiben - dafuer gibt
    es in VST3 keine Schnittstelle. Ziehen ist der vorgesehene Weg, Bouncen der
    zweite (siehe Transportkopplung im Prozessor).
*/
class ZiehKnopf : public juce::Component
{
public:
    explicit ZiehKnopf (NexusBridgeProcessor& p);

    void paint (juce::Graphics&) override;
    void mouseDrag (const juce::MouseEvent&) override;
    void mouseDown (const juce::MouseEvent&) override;
    void mouseUp (const juce::MouseEvent&) override;
    void mouseEnter (const juce::MouseEvent&) override;
    void mouseExit (const juce::MouseEvent&) override;

private:
    NexusBridgeProcessor& prozessor;
    bool zeigerDrueber { false };
    bool ziehtSchon { false };
};

/** Vorleser-Tab: Text hinein, gesprochenes Audio zurueck. */
class ReaderPanel : public juce::Component,
                    private juce::Timer
{
public:
    explicit ReaderPanel (NexusBridgeProcessor& p);
    ~ReaderPanel() override;

    void paint (juce::Graphics&) override;
    void resized() override;

    /** Holt Engines, Stimmen und frühere Ausgaben vom Dienst. */
    void statusAbrufen();

private:
    struct StatusErgebnis
    {
        bool ok { false };
        juce::String fehler;
        juce::var daten;
    };

    struct ErzeugErgebnis
    {
        bool ok { false };
        juce::String fehler;
        juce::String dateiPfad;
        juce::String name;
        double sekunden { 0.0 };
    };

    void timerCallback() override;
    void engineGewechselt();
    void erzeugen();
    void arbeitAnzeigen (bool laeuft);
    void meldung (const juce::String& text, juce::Colour farbe);
    void ausgabeGewaehlt();
    void sichernUnter();
    void clipLaden (const juce::String& lokalerPfad, const juce::String& url, const juce::String& name);

    NexusBridgeProcessor& prozessor;

    juce::Label engineBeschriftung { {}, "Stimmerzeugung" };
    juce::ComboBox engineWahl;
    juce::Label stimmeBeschriftung { {}, "Stimme" };
    juce::ComboBox stimmenWahl;
    juce::TextButton aktualisierenKnopf { "Neu laden" };

    juce::Label textBeschriftung { {}, "Text" };
    juce::TextEditor textFeld;
    juce::Label zaehler;

    juce::TextButton erzeugenKnopf { "Sprechen lassen" };
    juce::TextButton abspielenKnopf { "Abspielen" };
    juce::Label statusZeile;
    double fortschritt { -1.0 };
    juce::ProgressBar balken { fortschritt };

    juce::Label ausgabenBeschriftung { {}, juce::CharPointer_UTF8 ("Frühere Ausgaben") };
    juce::ComboBox ausgabenWahl;
    juce::var ausgabenDaten;
    juce::var engineDaten;

    WellenAnzeige welle;
    ZiehKnopf ziehKnopf;
    juce::TextButton sichernKnopf { juce::CharPointer_UTF8 ("Sichern unter …") };
    juce::ToggleButton transportKnopf { "Mit DAW-Transport abspielen" };
    std::unique_ptr<juce::FileChooser> dateiwahl;

    bool arbeitLaeuft { false };

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (ReaderPanel)
};
