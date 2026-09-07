#include "PluginEditor.h"

using namespace juce;

//== Zugangspanel ================================================================

ZugangsPanel::ZugangsPanel (NexusBridgeProcessor& p) : prozessor (p)
{
    auto beschriftungEinrichten = [this] (Label& label)
    {
        label.setFont (NexusLookAndFeel::schrift (11.0f, true));
        label.setColour (Label::textColourId, NexusFarben::textGedaempft);
        addAndMakeVisible (label);
    };

    titel.setFont (NexusLookAndFeel::schrift (15.0f, true));
    titel.setColour (Label::textColourId, NexusFarben::text);
    addAndMakeVisible (titel);

    beschriftungEinrichten (hostBeschriftung);
    beschriftungEinrichten (portBeschriftung);
    beschriftungEinrichten (benutzerBeschriftung);
    beschriftungEinrichten (passwortBeschriftung);

    for (auto* feld : { &hostFeld, &portFeld, &benutzerFeld, &passwortFeld })
    {
        feld->setFont (NexusLookAndFeel::schrift (13.0f));
        addAndMakeVisible (*feld);
    }

    passwortFeld.setPasswordCharacter ((juce_wchar) 0x2022);

    const auto vorhanden = prozessor.ladeZugang();
    hostFeld.setText (vorhanden.host, false);
    portFeld.setText (String (vorhanden.port), false);
    benutzerFeld.setText (vorhanden.benutzer, false);
    passwortFeld.setText (vorhanden.passwort, false);

    addAndMakeVisible (configKnopf);
    configKnopf.onClick = [this] { ausConfigEnvLaden(); };

    addAndMakeVisible (speichernKnopf);
    speichernKnopf.setColour (TextButton::buttonColourId, NexusFarben::akzentDunkel);
    speichernKnopf.setColour (TextButton::textColourOffId, NexusFarben::hintergrund);
    speichernKnopf.onClick = [this] { speichern(); };

    addAndMakeVisible (schliessenKnopf);
    schliessenKnopf.onClick = [this] { setVisible (false); };

    hinweis.setFont (NexusLookAndFeel::schrift (11.0f));
    hinweis.setColour (Label::textColourId, NexusFarben::textGedaempft);
    hinweis.setJustificationType (Justification::topLeft);
    hinweis.setText (CharPointer_UTF8 ("Das Konto braucht das Recht \"reader\". Die Daten liegen unverschlüsselt "
                                       "in den Plugin-Einstellungen unter Application Support/Metacube."),
                     dontSendNotification);
    addAndMakeVisible (hinweis);
}

void ZugangsPanel::ausConfigEnvLaden()
{
    if (! prozessor.zugangAusConfigEnv())
    {
        hinweis.setColour (Label::textColourId, NexusFarben::warnung);
        hinweis.setText ("config.env nicht gefunden oder ohne MEDIA_AI_AUTH_*-Eintraege.",
                         dontSendNotification);
        return;
    }

    const auto zugang = prozessor.ladeZugang();
    hostFeld.setText (zugang.host, false);
    portFeld.setText (String (zugang.port), false);
    benutzerFeld.setText (zugang.benutzer, false);
    passwortFeld.setText (zugang.passwort, false);

    hinweis.setColour (Label::textColourId, NexusFarben::erfolg);
    hinweis.setText (CharPointer_UTF8 ("Aus config.env übernommen."), dontSendNotification);

    if (beiSpeichern)
        beiSpeichern();
}

void ZugangsPanel::speichern()
{
    ApiZugang zugang;
    zugang.host     = hostFeld.getText().trim();
    zugang.port     = portFeld.getText().getIntValue();
    zugang.benutzer = benutzerFeld.getText().trim();
    zugang.passwort = passwortFeld.getText();

    if (zugang.port <= 0)
        zugang.port = 8013;

    prozessor.speichereZugang (zugang);
    setVisible (false);

    if (beiSpeichern)
        beiSpeichern();
}

void ZugangsPanel::paint (Graphics& g)
{
    g.fillAll (NexusFarben::hintergrund.withAlpha (0.92f));

    auto bereich = getLocalBounds().reduced (28, 40).toFloat();
    g.setColour (NexusFarben::flaeche);
    g.fillRoundedRectangle (bereich, 12.0f);
    g.setColour (NexusFarben::rand);
    g.drawRoundedRectangle (bereich.reduced (0.5f), 12.0f, 1.0f);
}

void ZugangsPanel::resized()
{
    auto bereich = getLocalBounds().reduced (28, 40).reduced (22, 20);

    titel.setBounds (bereich.removeFromTop (24));
    bereich.removeFromTop (10);

    auto zeile = [&bereich] (Label& label, Component& feld, int breite)
    {
        auto platz = bereich.removeFromTop (44);
        auto links = platz.removeFromLeft (breite);
        label.setBounds (links.removeFromTop (16));
        feld.setBounds (links.reduced (0, 1));
        return platz;
    };

    auto rest = zeile (hostBeschriftung, hostFeld, bereich.getWidth() - 96);
    rest.removeFromLeft (10);
    portBeschriftung.setBounds (rest.removeFromTop (16));
    portFeld.setBounds (rest.reduced (0, 1));

    bereich.removeFromTop (6);
    zeile (benutzerBeschriftung, benutzerFeld, bereich.getWidth());
    bereich.removeFromTop (6);
    zeile (passwortBeschriftung, passwortFeld, bereich.getWidth());

    bereich.removeFromTop (12);
    auto knoepfe = bereich.removeFromTop (34);
    configKnopf.setBounds (knoepfe.removeFromLeft (130));
    knoepfe.removeFromLeft (10);
    speichernKnopf.setBounds (knoepfe.removeFromLeft (130));
    knoepfe.removeFromLeft (10);
    schliessenKnopf.setBounds (knoepfe.removeFromLeft (110));

    bereich.removeFromTop (14);
    hinweis.setBounds (bereich.removeFromTop (48));
}

//== Editor ======================================================================

NexusBridgeEditor::NexusBridgeEditor (NexusBridgeProcessor& p)
    : AudioProcessorEditor (&p), prozessor (p), zugang (p)
{
    setLookAndFeel (&aussehen);

    auto* reader = new ReaderPanel (prozessor);
    readerTab = reader;

    tabs.setTabBarDepth (38);
    tabs.setOutline (0);
    tabs.addTab ("Vorleser", NexusFarben::hintergrund, reader, true);
    tabs.addTab ("Song", NexusFarben::hintergrund,
                 new PlatzhalterPanel ("Song Studio (ACE-Step 1.5 Turbo)",
                                       "Der Endpunkt /song/generate ist vorhanden und kennt Stilbeschreibung, "
                                       "Text, Laenge, BPM, Tonart, Schritte, CFG, LoRAs, Referenzspur und "
                                       "Parameterreihen. Angebunden wird er, sobald der Vorleser-Weg steht.\n\n"
                                       "Wichtig fuer den Betrieb: Song-Auftraege laufen ueber die gemeinsame "
                                       "Warteschlange mit Bild und Video. Ein Auftrag kann deshalb warten, "
                                       "bevor er ueberhaupt rechnet - der Tab wird Warteposition und Zustand "
                                       "aus /song/status und /song/queue anzeigen.",
                                       "Konto braucht das Recht \"song\" - das Konto aus config.env hat es nicht."),
                 true);
    auto* restore = new RestorePanel (prozessor);
    restoreTab = restore;
    tabs.addTab (CharPointer_UTF8 ("EQ & Restaurieren"), NexusFarben::hintergrund, restore, true);
    addAndMakeVisible (tabs);

    addAndMakeVisible (verbindungKnopf);
    verbindungKnopf.onClick = [this] { zugang.setVisible (! zugang.isVisible()); };

    addChildComponent (zugang);
    zugang.beiSpeichern = [this]
    {
        if (readerTab != nullptr) readerTab->statusAbrufen();
        if (restoreTab != nullptr) restoreTab->statusAbrufen();
    };

    setResizable (true, true);
    setResizeLimits (600, 620, 1400, 1100);
    setSize (760, 720);
}

NexusBridgeEditor::~NexusBridgeEditor()
{
    setLookAndFeel (nullptr);
}

void NexusBridgeEditor::paint (Graphics& g)
{
    g.fillAll (NexusFarben::hintergrund);

    auto kopf = getLocalBounds().removeFromTop (58);

    g.setColour (NexusFarben::flaeche);
    g.fillRect (kopf);
    g.setColour (NexusFarben::rand);
    g.fillRect (kopf.removeFromBottom (1));

    auto text = kopf.reduced (18, 0);

    g.setColour (NexusFarben::text);
    g.setFont (NexusLookAndFeel::schrift (18.0f, true));
    g.drawText ("NEXUS BRIDGE", text.removeFromTop (34).withTrimmedTop (8), Justification::topLeft, false);

    const auto z = prozessor.api()->getZugang();

    g.setColour (NexusFarben::textGedaempft);
    g.setFont (NexusLookAndFeel::schrift (11.0f));
    g.drawText (String (CharPointer_UTF8 ("Media-AI  ·  ")) + z.host + ":" + String (z.port)
                    + (z.benutzer.isNotEmpty() ? String (CharPointer_UTF8 ("  ·  ")) + z.benutzer : String()),
                text.removeFromTop (16), Justification::topLeft, false);
}

void NexusBridgeEditor::resized()
{
    auto bereich = getLocalBounds();

    auto kopf = bereich.removeFromTop (58);
    verbindungKnopf.setBounds (kopf.removeFromRight (128).reduced (14, 14));

    tabs.setBounds (bereich);
    zugang.setBounds (getLocalBounds());
}
