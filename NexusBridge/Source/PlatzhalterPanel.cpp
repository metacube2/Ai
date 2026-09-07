#include "PlatzhalterPanel.h"
#include "NexusLookAndFeel.h"

using namespace juce;

PlatzhalterPanel::PlatzhalterPanel (String titel, String text, String hinweis)
    : ueberschrift (std::move (titel)), koerper (std::move (text)), fussnote (std::move (hinweis))
{
}

void PlatzhalterPanel::paint (Graphics& g)
{
    g.fillAll (NexusFarben::hintergrund);

    auto bereich = getLocalBounds().reduced (24, 28);

    g.setColour (NexusFarben::flaeche);
    g.fillRoundedRectangle (bereich.toFloat(), 12.0f);
    g.setColour (NexusFarben::rand);
    g.drawRoundedRectangle (bereich.toFloat().reduced (0.5f), 12.0f, 1.0f);

    auto innen = bereich.reduced (22, 24);

    g.setColour (NexusFarben::text);
    g.setFont (NexusLookAndFeel::schrift (17.0f, true));
    g.drawText (ueberschrift, innen.removeFromTop (26), Justification::topLeft, false);

    innen.removeFromTop (8);

    g.setColour (NexusFarben::textGedaempft);
    g.setFont (NexusLookAndFeel::schrift (13.0f));
    g.drawFittedText (koerper, innen.removeFromTop (jmax (60, innen.getHeight() - 60)),
                      Justification::topLeft, 12, 1.0f);

    g.setColour (NexusFarben::akzent.withAlpha (0.85f));
    g.setFont (NexusLookAndFeel::schrift (12.0f, true));
    g.drawFittedText (fussnote, innen.removeFromBottom (48), Justification::bottomLeft, 3, 1.0f);
}
