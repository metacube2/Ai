#pragma once

#include <juce_gui_basics/juce_gui_basics.h>

/** Tab fuer eine noch nicht angebundene Funktion. Sagt offen, was fehlt,
    statt eine Bedienung vorzutaeuschen. */
class PlatzhalterPanel : public juce::Component
{
public:
    PlatzhalterPanel (juce::String titel, juce::String text, juce::String hinweis);

    void paint (juce::Graphics&) override;

private:
    juce::String ueberschrift, koerper, fussnote;

    JUCE_DECLARE_NON_COPYABLE_WITH_LEAK_DETECTOR (PlatzhalterPanel)
};
