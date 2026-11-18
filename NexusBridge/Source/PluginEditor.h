#pragma once

#include <juce_audio_processors/juce_audio_processors.h>

#include "PluginProcessor.h"
#include "NexusLookAndFeel.h"
#include "ReaderPanel.h"
#include "RestorePanel.h"
#include "PlatzhalterPanel.h"

/** Ueberlagerung fuer Host, Port und Konto des Media-AI-Dienstes. */
class ZugangsPanel : public juce::Component
{
public:
    explicit ZugangsPanel (NexusBridgeProcessor& p);

    void paint (juce::Graphics&) override;
    void resized() override;

    std::function<void()> beiSpeichern;

private:
    void ausConfigEnvLaden();
    void speichern();

    NexusBridgeProcessor& prozessor;

    juce::Label titel { {}, "Verbindung zum Media-AI-Dienst" };
    juce::Label hostBeschriftung { {}, "Host" };
    juce::TextEditor hostFeld;
    juce::Label portBeschriftung { {}, "Port" };
    juce::TextEditor portFeld;
    juce::Label benutzerBeschriftung { {}, "Benutzer" };
    juce::TextEditor benutzerFeld;
    juce::Label passwortBeschriftung { {}, "Passwort" };
    juce::TextEditor passwortFeld;
    juce::TextButton configKnopf { "Aus config.env" };
    juce::TextButton speichernKnopf { juce::CharPointer_UTF8 ("Übernehmen") };
    juce::TextButton schliessenKnopf { juce::CharPointer_UTF8 ("Schließen") };
    juce::Label hinweis;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (ZugangsPanel)
};

class NexusBridgeEditor : public juce::AudioProcessorEditor
{
public:
    explicit NexusBridgeEditor (NexusBridgeProcessor&);
    ~NexusBridgeEditor() override;

    void paint (juce::Graphics&) override;
    void resized() override;

private:
    NexusBridgeProcessor& prozessor;
    NexusLookAndFeel aussehen;

    juce::TabbedComponent tabs { juce::TabbedButtonBar::TabsAtTop };
    ReaderPanel* readerTab { nullptr };
    RestorePanel* restoreTab { nullptr };

    juce::TextButton verbindungKnopf { "Verbindung" };
    ZugangsPanel zugang;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (NexusBridgeEditor)
};
