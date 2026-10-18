#include "ReaderPanel.h"

using namespace juce;

namespace
{
    constexpr int maximalZeichen = 10000;

    String saubererDateiname (const String& roh)
    {
        auto name = File::createLegalFileName (roh);
        return name.isEmpty() ? String ("nexus_clip.wav") : name;
    }
}

//== Wellenanzeige ===============================================================

WellenAnzeige::WellenAnzeige (NexusBridgeProcessor& p) : prozessor (p)
{
    setMouseCursor (MouseCursor::NormalCursor);
    startTimerHz (25);
}

void WellenAnzeige::neuBerechnen()
{
    huellkurve.clearQuick();
    beschriftung.clear();

    auto clip = prozessor.aktuellerClip();

    if (clip == nullptr)
    {
        repaint();
        return;
    }

    beschriftung = clip->name;

    const auto& puffer = clip->puffer;
    const auto laenge = puffer.getNumSamples();
    const auto kanaele = puffer.getNumChannels();
    const auto spalten = jmax (1, jmin (1200, laenge));
    const auto proSpalte = jmax (1, laenge / spalten);

    for (int spalte = 0; spalte < spalten; ++spalte)
    {
        const auto start = spalte * proSpalte;
        const auto anzahl = jmin (proSpalte, laenge - start);

        if (anzahl <= 0)
            break;

        Range<float> bereich;

        for (int kanal = 0; kanal < kanaele; ++kanal)
            bereich = bereich.getUnionWith (FloatVectorOperations::findMinAndMax (puffer.getReadPointer (kanal) + start, anzahl));

        huellkurve.add (bereich);
    }

    repaint();
}

void WellenAnzeige::timerCallback()
{
    const auto jetzt = prozessor.wiedergabeAnteil();

    if (std::abs (jetzt - letzterAnteil) > 0.001)
    {
        letzterAnteil = jetzt;
        repaint();
    }
}

void WellenAnzeige::paint (Graphics& g)
{
    auto bereich = getLocalBounds().toFloat();

    g.setColour (NexusFarben::flaeche);
    g.fillRoundedRectangle (bereich, 10.0f);

    g.setColour (zeigerDrueber && ! huellkurve.isEmpty() ? NexusFarben::akzent.withAlpha (0.6f) : NexusFarben::rand);
    g.drawRoundedRectangle (bereich.reduced (0.5f), 10.0f, 1.0f);

    if (huellkurve.isEmpty())
    {
        g.setColour (NexusFarben::textGedaempft);
        g.setFont (NexusLookAndFeel::schrift (13.0f));
        g.drawFittedText ("Noch kein Audio - erzeugtes Ergebnis erscheint hier",
                          getLocalBounds().reduced (12), Justification::centred, 2);
        return;
    }

    auto zeichenflaeche = bereich.reduced (10.0f, 12.0f);
    const auto mitte = zeichenflaeche.getCentreY();
    const auto halbe = zeichenflaeche.getHeight() * 0.5f;
    const auto breite = zeichenflaeche.getWidth();
    const auto anzahl = huellkurve.size();

    g.setColour (NexusFarben::akzent.withAlpha (0.85f));

    for (int i = 0; i < anzahl; ++i)
    {
        const auto x = zeichenflaeche.getX() + breite * ((float) i / (float) anzahl);
        const auto bereichI = huellkurve.getReference (i);
        const auto oben = mitte - jlimit (0.0f, halbe, bereichI.getEnd() * halbe);
        const auto unten = mitte - jlimit (-halbe, 0.0f, bereichI.getStart() * halbe);

        g.drawLine (x, oben, x, jmax (unten, oben + 1.0f), jmax (1.0f, breite / (float) anzahl));
    }

    // Abspielzeiger
    const auto anteil = (float) prozessor.wiedergabeAnteil();

    if (anteil > 0.0f && anteil < 1.0f)
    {
        g.setColour (NexusFarben::text.withAlpha (0.8f));
        const auto x = zeichenflaeche.getX() + breite * anteil;
        g.drawLine (x, zeichenflaeche.getY(), x, zeichenflaeche.getBottom(), 1.5f);
    }

    g.setColour (NexusFarben::textGedaempft);
    g.setFont (NexusLookAndFeel::schrift (11.0f));
    g.drawText (zeigerDrueber ? "Ziehen, um die Datei in die Spur zu legen" : beschriftung,
                getLocalBounds().reduced (12, 6), Justification::topLeft, true);
}

void WellenAnzeige::mouseEnter (const MouseEvent&)
{
    zeigerDrueber = true;
    repaint();
}

void WellenAnzeige::mouseExit (const MouseEvent&)
{
    zeigerDrueber = false;
    repaint();
}

void WellenAnzeige::mouseDrag (const MouseEvent&)
{
    if (ziehtSchon)
        return;

    const auto datei = prozessor.aktuelleClipDatei();

    if (! datei.existsAsFile())
        return;

    // Die DAW uebernimmt die Datei selbst; false heisst: nicht verschieben.
    ziehtSchon = true;
    DragAndDropContainer::performExternalDragDropOfFiles ({ datei.getFullPathName() }, false, this);
    ziehtSchon = false;
}

//== Ziehknopf ===================================================================

ZiehKnopf::ZiehKnopf (NexusBridgeProcessor& p) : prozessor (p)
{
    setMouseCursor (MouseCursor::DraggingHandCursor);
}

void ZiehKnopf::paint (Graphics& g)
{
    const auto bereit = prozessor.aktuelleClipDatei().existsAsFile();
    auto bereich = getLocalBounds().toFloat().reduced (0.5f);

    g.setColour (bereit ? (zeigerDrueber ? NexusFarben::akzent.withAlpha (0.22f)
                                         : NexusFarben::akzent.withAlpha (0.12f))
                        : NexusFarben::flaeche);
    g.fillRoundedRectangle (bereich, 8.0f);

    g.setColour (bereit ? NexusFarben::akzent : NexusFarben::rand);

    // Gestrichelter Rand: der Griff sieht damit nach Ablage aus, nicht nach
    // einem Knopf, den ein Klick allein ausloesen wuerde.
    Path rahmen, striche;
    rahmen.addRoundedRectangle (bereich, 8.0f);
    const float muster[] = { 5.0f, 4.0f };
    PathStrokeType (1.2f).createDashedStroke (striche, rahmen, muster, 2);
    g.fillPath (striche);

    auto inhalt = getLocalBounds().reduced (10, 0);

    // Pfeil nach unten als Hinweis auf die Ablage in der Spur
    if (bereit)
    {
        auto symbol = inhalt.removeFromLeft (18).toFloat().reduced (2.0f, (float) inhalt.getHeight() * 0.32f);
        Path pfeil;
        pfeil.startNewSubPath (symbol.getCentreX(), symbol.getY());
        pfeil.lineTo (symbol.getCentreX(), symbol.getBottom());
        pfeil.startNewSubPath (symbol.getX(), symbol.getBottom() - symbol.getWidth() * 0.5f);
        pfeil.lineTo (symbol.getCentreX(), symbol.getBottom());
        pfeil.lineTo (symbol.getRight(), symbol.getBottom() - symbol.getWidth() * 0.5f);
        g.strokePath (pfeil, PathStrokeType (1.6f, PathStrokeType::curved, PathStrokeType::rounded));
    }

    g.setColour (bereit ? NexusFarben::akzent : NexusFarben::textGedaempft.withAlpha (0.6f));
    g.setFont (NexusLookAndFeel::schrift (13.0f, true));
    g.drawFittedText (bereit ? "In die Spur ziehen" : "Noch nichts zum Ziehen",
                      inhalt, Justification::centredLeft, 1, 0.9f);
}

void ZiehKnopf::mouseEnter (const MouseEvent&) { zeigerDrueber = true;  repaint(); }
void ZiehKnopf::mouseExit  (const MouseEvent&) { zeigerDrueber = false; repaint(); }
void ZiehKnopf::mouseDown  (const MouseEvent&) { ziehtSchon = false; }
void ZiehKnopf::mouseUp    (const MouseEvent&) { ziehtSchon = false; }

void ZiehKnopf::mouseDrag (const MouseEvent&)
{
    // mouseDrag kommt waehrend einer Geste mehrfach; ein zweiter nativer
    // Ziehvorgang wuerde den ersten stoeren.
    if (ziehtSchon)
        return;

    const auto datei = prozessor.aktuelleClipDatei();

    if (! datei.existsAsFile())
        return;

    ziehtSchon = true;
    DragAndDropContainer::performExternalDragDropOfFiles ({ datei.getFullPathName() }, false, this);
    ziehtSchon = false;
}

//== Vorleser-Tab ================================================================

ReaderPanel::ReaderPanel (NexusBridgeProcessor& p)
    : prozessor (p), welle (p), ziehKnopf (p)
{
    auto beschriftungEinrichten = [this] (Label& label)
    {
        label.setFont (NexusLookAndFeel::schrift (11.0f, true));
        label.setColour (Label::textColourId, NexusFarben::textGedaempft);
        addAndMakeVisible (label);
    };

    beschriftungEinrichten (engineBeschriftung);
    beschriftungEinrichten (stimmeBeschriftung);
    beschriftungEinrichten (textBeschriftung);
    beschriftungEinrichten (ausgabenBeschriftung);

    addAndMakeVisible (engineWahl);
    engineWahl.setTextWhenNothingSelected ("noch nicht geladen");
    engineWahl.onChange = [this] { engineGewechselt(); };

    addAndMakeVisible (stimmenWahl);
    stimmenWahl.setTextWhenNothingSelected ("noch nicht geladen");

    addAndMakeVisible (aktualisierenKnopf);
    aktualisierenKnopf.onClick = [this] { statusAbrufen(); };

    addAndMakeVisible (textFeld);
    textFeld.setMultiLine (true, true);
    textFeld.setReturnKeyStartsNewLine (true);
    textFeld.setFont (NexusLookAndFeel::schrift (14.0f));
    textFeld.setTextToShowWhenEmpty (CharPointer_UTF8 ("Text eingeben - der Dienst spricht ihn mit der gewählten Stimme."),
                                     NexusFarben::textGedaempft.withAlpha (0.7f));
    textFeld.setText (prozessor.letzterAuftrag, false);
    textFeld.onTextChange = [this]
    {
        prozessor.letzterAuftrag = textFeld.getText();
        zaehler.setText (String (textFeld.getText().length()) + " / " + String (maximalZeichen),
                         dontSendNotification);
    };

    addAndMakeVisible (zaehler);
    zaehler.setFont (NexusLookAndFeel::schrift (11.0f));
    zaehler.setColour (Label::textColourId, NexusFarben::textGedaempft);
    zaehler.setJustificationType (Justification::centredRight);
    zaehler.setText ("0 / " + String (maximalZeichen), dontSendNotification);

    addAndMakeVisible (erzeugenKnopf);
    erzeugenKnopf.setColour (TextButton::buttonColourId, NexusFarben::akzentDunkel);
    erzeugenKnopf.setColour (TextButton::textColourOffId, NexusFarben::hintergrund);
    erzeugenKnopf.onClick = [this] { erzeugen(); };

    addAndMakeVisible (abspielenKnopf);
    abspielenKnopf.onClick = [this]
    {
        if (prozessor.laeuftWiedergabe())
            prozessor.haltWiedergabe();
        else
            prozessor.starteWiedergabe();
    };

    addAndMakeVisible (statusZeile);
    statusZeile.setFont (NexusLookAndFeel::schrift (12.0f));
    statusZeile.setColour (Label::textColourId, NexusFarben::textGedaempft);

    addChildComponent (balken);
    balken.setPercentageDisplay (false);

    addAndMakeVisible (ausgabenWahl);
    ausgabenWahl.setTextWhenNothingSelected ("keine");
    ausgabenWahl.onChange = [this] { ausgabeGewaehlt(); };

    addAndMakeVisible (welle);
    addAndMakeVisible (ziehKnopf);

    addAndMakeVisible (sichernKnopf);
    sichernKnopf.onClick = [this] { sichernUnter(); };

    addAndMakeVisible (transportKnopf);
    transportKnopf.setColour (ToggleButton::textColourId, NexusFarben::textGedaempft);
    transportKnopf.setColour (ToggleButton::tickColourId, NexusFarben::akzent);
    transportKnopf.setColour (ToggleButton::tickDisabledColourId, NexusFarben::rand);
    transportKnopf.setTooltip ("Startet den Clip mit der DAW-Wiedergabe - so nimmt ihn ein Bounce oder "
                               "Freeze der Spur auf.");
    transportKnopf.setToggleState (prozessor.transportKopplung(), dontSendNotification);
    transportKnopf.onClick = [this] { prozessor.setTransportKopplung (transportKnopf.getToggleState()); };

    startTimerHz (4);
    statusAbrufen();
}

ReaderPanel::~ReaderPanel()
{
    stopTimer();
}

void ReaderPanel::paint (Graphics& g)
{
    g.fillAll (NexusFarben::hintergrund);
}

void ReaderPanel::resized()
{
    auto bereich = getLocalBounds().reduced (16);

    auto kopf = bereich.removeFromTop (46);
    auto links = kopf.removeFromLeft (kopf.getWidth() / 2 - 6);
    engineBeschriftung.setBounds (links.removeFromTop (16));
    engineWahl.setBounds (links.reduced (0, 1));

    kopf.removeFromLeft (12);
    auto knopfPlatz = kopf.removeFromRight (100);
    knopfPlatz.removeFromTop (16);
    aktualisierenKnopf.setBounds (knopfPlatz.reduced (0, 1));
    kopf.removeFromRight (10);
    stimmeBeschriftung.setBounds (kopf.removeFromTop (16));
    stimmenWahl.setBounds (kopf.reduced (0, 1));

    bereich.removeFromTop (14);

    auto unten = bereich.removeFromBottom (196);
    auto steuerung = bereich.removeFromBottom (44);

    textBeschriftung.setBounds (bereich.removeFromTop (16));
    auto zaehlerPlatz = bereich.removeFromBottom (16);
    zaehler.setBounds (zaehlerPlatz);
    textFeld.setBounds (bereich.reduced (0, 2));

    steuerung.removeFromTop (6);
    erzeugenKnopf.setBounds (steuerung.removeFromLeft (170).reduced (0, 2));
    steuerung.removeFromLeft (10);
    abspielenKnopf.setBounds (steuerung.removeFromLeft (110).reduced (0, 2));
    steuerung.removeFromLeft (14);
    balken.setBounds (steuerung.removeFromBottom (10).reduced (0, 2));
    statusZeile.setBounds (steuerung);

    unten.removeFromTop (10);
    auto ausgaben = unten.removeFromBottom (44);
    auto uebergabe = unten.removeFromBottom (40);
    welle.setBounds (unten);

    uebergabe.removeFromTop (8);
    ziehKnopf.setBounds (uebergabe.removeFromLeft (188));
    uebergabe.removeFromLeft (10);
    sichernKnopf.setBounds (uebergabe.removeFromLeft (130));
    uebergabe.removeFromLeft (12);
    transportKnopf.setBounds (uebergabe);

    ausgaben.removeFromTop (4);
    ausgabenBeschriftung.setBounds (ausgaben.removeFromTop (16));
    ausgabenWahl.setBounds (ausgaben.reduced (0, 1));
}

void ReaderPanel::meldung (const String& text, Colour farbe)
{
    statusZeile.setColour (Label::textColourId, farbe);
    statusZeile.setText (text, dontSendNotification);
}

void ReaderPanel::arbeitAnzeigen (bool laeuft)
{
    arbeitLaeuft = laeuft;
    fortschritt = laeuft ? -1.0 : 0.0;
    balken.setVisible (laeuft);
    erzeugenKnopf.setEnabled (! laeuft);
    aktualisierenKnopf.setEnabled (! laeuft);
    ausgabenWahl.setEnabled (! laeuft);
}

void ReaderPanel::timerCallback()
{
    abspielenKnopf.setButtonText (prozessor.laeuftWiedergabe() ? "Anhalten" : "Abspielen");
    abspielenKnopf.setEnabled (prozessor.aktuellerClip() != nullptr);
    sichernKnopf.setEnabled (prozessor.aktuelleClipDatei().existsAsFile());
    ziehKnopf.repaint();
}

void ReaderPanel::statusAbrufen()
{
    meldung (CharPointer_UTF8 ("Dienst wird abgefragt …"), NexusFarben::textGedaempft);

    auto api = prozessor.api();

    starteAuftrag<StatusErgebnis> (prozessor.waechter(),
        [api]
        {
            StatusErgebnis ergebnis;
            const auto antwort = api->get ("/reader/status");
            ergebnis.ok = antwort.ok;
            ergebnis.fehler = antwort.fehler;
            ergebnis.daten = antwort.daten;
            return ergebnis;
        },
        [this] (const StatusErgebnis& ergebnis)
        {
            if (! ergebnis.ok)
            {
                meldung (ergebnis.fehler, NexusFarben::fehler);
                return;
            }

            engineDaten = ergebnis.daten.getProperty ("engines", var());
            ausgabenDaten = ergebnis.daten.getProperty ("outputs", var());

            const auto vorherigeEngine = prozessor.einstellungen().getValue ("reader_engine", "qwen3");

            engineWahl.clear (dontSendNotification);

            if (auto* liste = engineDaten.getArray())
            {
                for (int i = 0; i < liste->size(); ++i)
                {
                    const auto& eintrag = liste->getReference (i);
                    auto beschriftung = eintrag.getProperty ("label", "").toString();

                    if (! static_cast<bool> (eintrag.getProperty ("installed", false)))
                        beschriftung += "  (nicht installiert)";

                    engineWahl.addItem (beschriftung, i + 1);

                    if (eintrag.getProperty ("value", "").toString() == vorherigeEngine)
                        engineWahl.setSelectedId (i + 1, dontSendNotification);
                }
            }

            if (engineWahl.getSelectedId() == 0 && engineWahl.getNumItems() > 0)
                engineWahl.setSelectedId (1, dontSendNotification);

            engineGewechselt();

            ausgabenWahl.clear (dontSendNotification);

            if (auto* liste = ausgabenDaten.getArray())
                for (int i = 0; i < liste->size(); ++i)
                    ausgabenWahl.addItem (liste->getReference (i).getProperty ("name", "").toString(), i + 1);

            const auto beschaeftigt = static_cast<bool> (ergebnis.daten.getProperty ("busy", false));
            meldung (beschaeftigt ? "Dienst erreichbar - rechnet gerade an einem anderen Auftrag"
                                  : "Dienst erreichbar",
                     beschaeftigt ? NexusFarben::warnung : NexusFarben::erfolg);
        });
}

void ReaderPanel::engineGewechselt()
{
    stimmenWahl.clear (dontSendNotification);

    auto* liste = engineDaten.getArray();
    const auto index = engineWahl.getSelectedId() - 1;

    if (liste == nullptr || index < 0 || index >= liste->size())
        return;

    const auto& eintrag = liste->getReference (index);
    prozessor.einstellungen().setValue ("reader_engine", eintrag.getProperty ("value", "").toString());

    const auto vorherigeStimme = prozessor.einstellungen().getValue ("reader_stimme", "");

    if (auto* stimmen = eintrag.getProperty ("voices", var()).getArray())
    {
        for (int i = 0; i < stimmen->size(); ++i)
        {
            const auto& stimme = stimmen->getReference (i);
            stimmenWahl.addItem (stimme.getProperty ("label", "").toString(), i + 1);

            if (stimme.getProperty ("value", "").toString() == vorherigeStimme)
                stimmenWahl.setSelectedId (i + 1, dontSendNotification);
        }
    }

    if (stimmenWahl.getSelectedId() == 0 && stimmenWahl.getNumItems() > 0)
        stimmenWahl.setSelectedId (1, dontSendNotification);

    if (stimmenWahl.getNumItems() == 0)
        stimmenWahl.setTextWhenNothingSelected ("keine Stimme vorhanden");
}

void ReaderPanel::erzeugen()
{
    if (arbeitLaeuft)
        return;

    const auto text = textFeld.getText().trim();

    if (text.isEmpty())
    {
        meldung ("Kein Text eingegeben", NexusFarben::warnung);
        return;
    }

    if (text.length() > maximalZeichen)
    {
        meldung (String (CharPointer_UTF8 ("Text zu lang - der Dienst nimmt höchstens ")) + String (maximalZeichen) + " Zeichen",
                 NexusFarben::warnung);
        return;
    }

    auto* engines = engineDaten.getArray();
    const auto engineIndex = engineWahl.getSelectedId() - 1;

    if (engines == nullptr || engineIndex < 0 || engineIndex >= engines->size())
    {
        meldung (CharPointer_UTF8 ("Keine Stimmerzeugung gewählt"), NexusFarben::warnung);
        return;
    }

    const auto& engineEintrag = engines->getReference (engineIndex);
    const auto engine = engineEintrag.getProperty ("value", "").toString();

    String stimme;

    if (auto* stimmen = engineEintrag.getProperty ("voices", var()).getArray())
    {
        const auto stimmIndex = stimmenWahl.getSelectedId() - 1;

        if (stimmIndex >= 0 && stimmIndex < stimmen->size())
            stimme = stimmen->getReference (stimmIndex).getProperty ("value", "").toString();
    }

    if (stimme.isEmpty())
    {
        meldung (CharPointer_UTF8 ("Keine Stimme gewählt"), NexusFarben::warnung);
        return;
    }

    prozessor.einstellungen().setValue ("reader_stimme", stimme);
    prozessor.einstellungen().saveIfNeeded();

    DynamicObject::Ptr koerper (new DynamicObject());
    koerper->setProperty ("text", text);
    koerper->setProperty ("engine", engine);
    koerper->setProperty ("voice", stimme);

    auto api = prozessor.api();
    const var anfrage (koerper.get());
    const auto ziel = NexusBridgeProcessor::arbeitsOrdner();

    arbeitAnzeigen (true);
    meldung (engine == "qwen3" ? String (CharPointer_UTF8 ("Qwen3-TTS rechnet - das dauert, die Zeile bleibt stehen …"))
                               : String (CharPointer_UTF8 ("macOS-Stimme rechnet …")),
             NexusFarben::akzent);

    starteAuftrag<ErzeugErgebnis> (prozessor.waechter(),
        [api, anfrage, ziel]
        {
            ErzeugErgebnis ergebnis;
            const auto antwort = api->post ("/reader/generate", anfrage);

            if (! antwort.ok)
            {
                ergebnis.fehler = antwort.fehler;
                return ergebnis;
            }

            const auto quellPfad = ApiClient::feldText (antwort.daten, "output");
            const auto readerUrl = ApiClient::feldText (antwort.daten, "reader_url");
            ergebnis.sekunden = (double) antwort.daten.getProperty ("elapsed_seconds", 0.0);
            ergebnis.name = File (quellPfad).getFileName();

            if (ergebnis.name.isEmpty())
                ergebnis.name = "nexus_vorleser.wav";

            const auto datei = ziel.getChildFile (saubererDateiname (ergebnis.name));

            if (! api->holeDatei (readerUrl, quellPfad, datei, ergebnis.fehler))
                return ergebnis;

            ergebnis.dateiPfad = datei.getFullPathName();
            ergebnis.ok = true;
            return ergebnis;
        },
        [this] (const ErzeugErgebnis& ergebnis)
        {
            arbeitAnzeigen (false);

            if (! ergebnis.ok)
            {
                meldung (ergebnis.fehler.isEmpty() ? "Erzeugung fehlgeschlagen" : ergebnis.fehler,
                         NexusFarben::fehler);
                return;
            }

            String fehler;

            if (! prozessor.ladeClip (File (ergebnis.dateiPfad), ergebnis.name, fehler))
            {
                meldung (fehler, NexusFarben::fehler);
                return;
            }

            welle.neuBerechnen();
            meldung ("Fertig in " + String (ergebnis.sekunden, 1) + " s - abspielen oder in die Spur ziehen",
                     NexusFarben::erfolg);
            statusAbrufen();
        });
}

void ReaderPanel::ausgabeGewaehlt()
{
    auto* liste = ausgabenDaten.getArray();
    const auto index = ausgabenWahl.getSelectedId() - 1;

    if (liste == nullptr || index < 0 || index >= liste->size())
        return;

    const auto& eintrag = liste->getReference (index);
    clipLaden (eintrag.getProperty ("path", "").toString(),
               eintrag.getProperty ("reader_url", "").toString(),
               eintrag.getProperty ("name", "").toString());
}

void ReaderPanel::sichernUnter()
{
    const auto quelle = prozessor.aktuelleClipDatei();

    if (! quelle.existsAsFile())
        return;

    dateiwahl = std::make_unique<FileChooser> ("Gesprochenes Audio sichern",
                                               File::getSpecialLocation (File::userMusicDirectory)
                                                   .getChildFile (quelle.getFileName()),
                                               "*.wav");

    dateiwahl->launchAsync (FileBrowserComponent::saveMode | FileBrowserComponent::canSelectFiles
                                | FileBrowserComponent::warnAboutOverwriting,
        [this, quelle] (const FileChooser& wahl)
        {
            const auto ziel = wahl.getResult();

            if (ziel == File())
                return;

            ziel.deleteFile();

            if (quelle.copyFileTo (ziel))
                meldung ("Gesichert: " + ziel.getFileName(), NexusFarben::erfolg);
            else
                meldung ("Sichern fehlgeschlagen", NexusFarben::fehler);
        });
}

void ReaderPanel::clipLaden (const String& lokalerPfad, const String& url, const String& name)
{
    auto api = prozessor.api();
    const auto datei = NexusBridgeProcessor::arbeitsOrdner().getChildFile (saubererDateiname (name));

    arbeitAnzeigen (true);
    meldung (CharPointer_UTF8 ("Ausgabe wird geholt …"), NexusFarben::akzent);

    starteAuftrag<ErzeugErgebnis> (prozessor.waechter(),
        [api, lokalerPfad, url, datei, name]
        {
            ErzeugErgebnis ergebnis;
            ergebnis.name = name;

            if (api->holeDatei (url, lokalerPfad, datei, ergebnis.fehler))
            {
                ergebnis.ok = true;
                ergebnis.dateiPfad = datei.getFullPathName();
            }

            return ergebnis;
        },
        [this] (const ErzeugErgebnis& ergebnis)
        {
            arbeitAnzeigen (false);

            if (! ergebnis.ok)
            {
                meldung (ergebnis.fehler, NexusFarben::fehler);
                return;
            }

            String fehler;

            if (! prozessor.ladeClip (File (ergebnis.dateiPfad), ergebnis.name, fehler))
            {
                meldung (fehler, NexusFarben::fehler);
                return;
            }

            welle.neuBerechnen();
            meldung ("Geladen: " + ergebnis.name, NexusFarben::erfolg);
        });
}
