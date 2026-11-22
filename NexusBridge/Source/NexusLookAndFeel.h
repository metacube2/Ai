#pragma once

#include <juce_gui_basics/juce_gui_basics.h>

/** Farben und Zeichnung der Oberflaeche - dunkel, ein Akzentton, klare Flaechen. */
namespace NexusFarben
{
    const juce::Colour hintergrund      { 0xff101318 };
    const juce::Colour flaeche          { 0xff181c23 };
    const juce::Colour flaecheHell      { 0xff20252e };
    const juce::Colour rand             { 0xff2c333f };
    const juce::Colour text             { 0xffe6ebf2 };
    const juce::Colour textGedaempft    { 0xff8b97a8 };
    const juce::Colour akzent           { 0xff4fd6c8 };
    const juce::Colour akzentDunkel     { 0xff2f9a90 };
    const juce::Colour warnung          { 0xffe2a03f };
    const juce::Colour fehler           { 0xffe0685f };
    const juce::Colour erfolg           { 0xff6bd28a };
}

class NexusLookAndFeel : public juce::LookAndFeel_V4
{
public:
    NexusLookAndFeel();

    juce::Font getTextButtonFont (juce::TextButton&, int buttonHeight) override;
    void drawButtonBackground (juce::Graphics&, juce::Button&, const juce::Colour& backgroundColour,
                               bool shouldDrawButtonAsHighlighted, bool shouldDrawButtonAsDown) override;
    void drawButtonText (juce::Graphics&, juce::TextButton&,
                         bool shouldDrawButtonAsHighlighted, bool shouldDrawButtonAsDown) override;

    void drawComboBox (juce::Graphics&, int width, int height, bool isButtonDown,
                       int buttonX, int buttonY, int buttonW, int buttonH, juce::ComboBox&) override;
    juce::Font getComboBoxFont (juce::ComboBox&) override;
    void positionComboBoxText (juce::ComboBox&, juce::Label&) override;

    void fillTextEditorBackground (juce::Graphics&, int width, int height, juce::TextEditor&) override;
    void drawTextEditorOutline (juce::Graphics&, int width, int height, juce::TextEditor&) override;

    void drawProgressBar (juce::Graphics&, juce::ProgressBar&, int width, int height,
                          double progress, const juce::String& textToShow) override;

    int getTabButtonBestWidth (juce::TabBarButton&, int tabDepth) override;
    void drawTabButton (juce::TabBarButton&, juce::Graphics&, bool isMouseOver, bool isMouseDown) override;
    void drawTabAreaBehindFrontButton (juce::TabbedButtonBar&, juce::Graphics&, int, int) override {}

    static juce::Font schrift (float hoehe, bool fett = false);
};
