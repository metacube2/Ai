#include "NexusLookAndFeel.h"

#include <cmath>

using namespace juce;

NexusLookAndFeel::NexusLookAndFeel()
{
    setColour (ResizableWindow::backgroundColourId, NexusFarben::hintergrund);
    setColour (Label::textColourId,                 NexusFarben::text);
    setColour (TextButton::buttonColourId,          NexusFarben::flaecheHell);
    setColour (TextButton::buttonOnColourId,        NexusFarben::akzentDunkel);
    setColour (TextButton::textColourOffId,         NexusFarben::text);
    setColour (TextButton::textColourOnId,          NexusFarben::hintergrund);
    setColour (ComboBox::backgroundColourId,        NexusFarben::flaecheHell);
    setColour (ComboBox::textColourId,              NexusFarben::text);
    setColour (ComboBox::outlineColourId,           NexusFarben::rand);
    setColour (ComboBox::arrowColourId,             NexusFarben::textGedaempft);
    setColour (PopupMenu::backgroundColourId,       NexusFarben::flaeche);
    setColour (PopupMenu::textColourId,             NexusFarben::text);
    setColour (PopupMenu::highlightedBackgroundColourId, NexusFarben::akzentDunkel);
    setColour (PopupMenu::highlightedTextColourId,  NexusFarben::hintergrund);
    setColour (TextEditor::backgroundColourId,      NexusFarben::flaecheHell);
    setColour (TextEditor::textColourId,            NexusFarben::text);
    setColour (TextEditor::highlightColourId,       NexusFarben::akzentDunkel);
    setColour (TextEditor::outlineColourId,         NexusFarben::rand);
    setColour (TextEditor::focusedOutlineColourId,  NexusFarben::akzent);
    setColour (CaretComponent::caretColourId,       NexusFarben::akzent);
    setColour (ScrollBar::thumbColourId,            NexusFarben::rand);
    setColour (TabbedComponent::backgroundColourId, NexusFarben::hintergrund);
    setColour (TabbedComponent::outlineColourId,    Colours::transparentBlack);
    setColour (ProgressBar::backgroundColourId,     NexusFarben::flaecheHell);
    setColour (ProgressBar::foregroundColourId,     NexusFarben::akzent);
}

Font NexusLookAndFeel::schrift (float hoehe, bool fett)
{
    auto optionen = FontOptions().withHeight (hoehe);

    if (fett)
        optionen = optionen.withStyle ("Bold");

    return Font (optionen);
}

Font NexusLookAndFeel::getTextButtonFont (TextButton&, int buttonHeight)
{
    return schrift (jmin (15.0f, buttonHeight * 0.52f), true);
}

void NexusLookAndFeel::drawButtonBackground (Graphics& g, Button& button, const Colour& backgroundColour,
                                             bool shouldDrawButtonAsHighlighted, bool shouldDrawButtonAsDown)
{
    auto bereich = button.getLocalBounds().toFloat().reduced (0.5f);
    const auto radius = jmin (8.0f, bereich.getHeight() * 0.28f);

    auto grund = backgroundColour;

    if (! button.isEnabled())
        grund = NexusFarben::flaeche;
    else if (shouldDrawButtonAsDown)
        grund = grund.darker (0.25f);
    else if (shouldDrawButtonAsHighlighted)
        grund = grund.brighter (0.12f);

    g.setColour (grund);
    g.fillRoundedRectangle (bereich, radius);

    g.setColour (button.isEnabled() ? NexusFarben::rand : NexusFarben::rand.withAlpha (0.5f));
    g.drawRoundedRectangle (bereich, radius, 1.0f);
}

void NexusLookAndFeel::drawButtonText (Graphics& g, TextButton& button,
                                       bool shouldDrawButtonAsHighlighted, bool)
{
    ignoreUnused (shouldDrawButtonAsHighlighted);

    auto farbe = button.findColour (button.getToggleState() ? TextButton::textColourOnId
                                                           : TextButton::textColourOffId);

    if (! button.isEnabled())
        farbe = NexusFarben::textGedaempft.withAlpha (0.6f);

    g.setColour (farbe);
    g.setFont (getTextButtonFont (button, button.getHeight()));
    g.drawFittedText (button.getButtonText(), button.getLocalBounds().reduced (6, 0),
                      Justification::centred, 1, 0.8f);
}

void NexusLookAndFeel::drawComboBox (Graphics& g, int width, int height, bool,
                                     int, int, int, int, ComboBox& box)
{
    Rectangle<float> bereich (0.0f, 0.0f, (float) width, (float) height);
    bereich = bereich.reduced (0.5f);
    const auto radius = jmin (8.0f, bereich.getHeight() * 0.28f);

    g.setColour (box.findColour (ComboBox::backgroundColourId));
    g.fillRoundedRectangle (bereich, radius);

    g.setColour (box.hasKeyboardFocus (false) ? NexusFarben::akzent : NexusFarben::rand);
    g.drawRoundedRectangle (bereich, radius, 1.0f);

    // Pfeil
    const auto mitteX = (float) width - 16.0f;
    const auto mitteY = (float) height * 0.5f;
    Path pfeil;
    pfeil.startNewSubPath (mitteX - 4.5f, mitteY - 2.0f);
    pfeil.lineTo (mitteX, mitteY + 3.0f);
    pfeil.lineTo (mitteX + 4.5f, mitteY - 2.0f);

    g.setColour (box.findColour (ComboBox::arrowColourId));
    g.strokePath (pfeil, PathStrokeType (1.6f, PathStrokeType::curved, PathStrokeType::rounded));
}

Font NexusLookAndFeel::getComboBoxFont (ComboBox&)
{
    return schrift (14.0f);
}

void NexusLookAndFeel::positionComboBoxText (ComboBox& box, Label& label)
{
    label.setBounds (10, 1, box.getWidth() - 32, box.getHeight() - 2);
    label.setFont (getComboBoxFont (box));
}

void NexusLookAndFeel::fillTextEditorBackground (Graphics& g, int width, int height, TextEditor& editor)
{
    g.setColour (editor.findColour (TextEditor::backgroundColourId));
    g.fillRoundedRectangle (Rectangle<float> (0.0f, 0.0f, (float) width, (float) height).reduced (0.5f), 8.0f);
}

void NexusLookAndFeel::drawTextEditorOutline (Graphics& g, int width, int height, TextEditor& editor)
{
    if (! editor.isEnabled())
        return;

    g.setColour (editor.hasKeyboardFocus (true) ? NexusFarben::akzent : NexusFarben::rand);
    g.drawRoundedRectangle (Rectangle<float> (0.0f, 0.0f, (float) width, (float) height).reduced (0.5f), 8.0f, 1.0f);
}

void NexusLookAndFeel::drawProgressBar (Graphics& g, ProgressBar& bar, int width, int height,
                                        double progress, const String& textToShow)
{
    Rectangle<float> bereich (0.0f, 0.0f, (float) width, (float) height);
    const auto radius = bereich.getHeight() * 0.5f;

    g.setColour (bar.findColour (ProgressBar::backgroundColourId));
    g.fillRoundedRectangle (bereich, radius);

    if (progress >= 0.0 && progress <= 1.0)
    {
        auto gefuellt = bereich.withWidth (jmax (radius * 2.0f, (float) (bereich.getWidth() * progress)));
        g.setColour (bar.findColour (ProgressBar::foregroundColourId));
        g.fillRoundedRectangle (gefuellt, radius);
    }
    else
    {
        // Unbestimmt: ein wandernder Balken, weil der Dienst keine Restzeit meldet.
        const auto laenge = bereich.getWidth() * 0.28f;
        const auto zeit = (float) (Time::getMillisecondCounter() % 1800) / 1800.0f;
        const auto x = (bereich.getWidth() + laenge) * zeit - laenge;

        g.setColour (bar.findColour (ProgressBar::foregroundColourId).withAlpha (0.85f));
        g.fillRoundedRectangle (bereich.withX (jmax (0.0f, x))
                                       .withWidth (jmin (laenge, bereich.getWidth() - jmax (0.0f, x))),
                                radius);
    }

    if (textToShow.isNotEmpty())
    {
        g.setColour (NexusFarben::text);
        g.setFont (schrift (jmin (12.0f, (float) height * 0.6f)));
        g.drawText (textToShow, bereich.toNearestInt(), Justification::centred, false);
    }
}

int NexusLookAndFeel::getTabButtonBestWidth (TabBarButton& button, int)
{
    const auto breite = (int) std::ceil (GlyphArrangement::getStringWidth (schrift (14.0f, true), button.getButtonText()));
    return jmax (96, breite + 34);
}

void NexusLookAndFeel::drawTabButton (TabBarButton& button, Graphics& g, bool isMouseOver, bool)
{
    auto bereich = button.getLocalBounds().toFloat().reduced (2.0f, 4.0f);
    const auto aktiv = button.getToggleState();

    if (aktiv)
    {
        g.setColour (NexusFarben::flaecheHell);
        g.fillRoundedRectangle (bereich, 8.0f);
    }
    else if (isMouseOver)
    {
        g.setColour (NexusFarben::flaeche);
        g.fillRoundedRectangle (bereich, 8.0f);
    }

    g.setColour (aktiv ? NexusFarben::akzent
                       : (button.isEnabled() ? NexusFarben::textGedaempft
                                             : NexusFarben::textGedaempft.withAlpha (0.45f)));
    g.setFont (schrift (14.0f, aktiv));
    g.drawFittedText (button.getButtonText(), bereich.toNearestInt(), Justification::centred, 1, 0.9f);

    if (aktiv)
    {
        g.setColour (NexusFarben::akzent);
        g.fillRoundedRectangle (bereich.removeFromBottom (2.5f).reduced (10.0f, 0.0f), 1.5f);
    }
}
