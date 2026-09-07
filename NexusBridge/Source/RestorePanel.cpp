#include "RestorePanel.h"

using namespace juce;

//== Kurvenanzeige ===============================================================

void KurvenAnzeige::setReihen (Array<Reihe> neue)
{
    reihen = std::move (neue);
    repaint();
}

void KurvenAnzeige::leeren()
{
    reihen.clearQuick();
    repaint();
}

void KurvenAnzeige::paint (Graphics& g)
{
    auto rahmen = getLocalBounds().toFloat();

    g.setColour (NexusFarben::flaeche);
    g.fillRoundedRectangle (rahmen, 10.0f);
    g.setColour (NexusFarben::rand);
    g.drawRoundedRectangle (rahmen.reduced (0.5f), 10.0f, 1.0f);

    Array<const Reihe*> gueltig;

    for (const auto& reihe : reihen)
        if (reihe.punkte.size() > 1)
            gueltig.add (&reihe);

    if (gueltig.isEmpty())
    {
        g.setColour (NexusFarben::textGedaempft);
        g.setFont (NexusLookAndFeel::schrift (13.0f));
        g.drawFittedText ("Spektrum und angewandte Kurve erscheinen hier",
                          getLocalBounds().reduced (12), Justification::centred, 2);
        return;
    }

    float minHz = 1.0e9f, maxHz = 0.0f, maxDb = 6.0f;

    for (auto* reihe : gueltig)
        for (const auto& punkt : reihe->punkte)
        {
            minHz = jmin (minHz, punkt.x);
            maxHz = jmax (maxHz, punkt.x);
            maxDb = jmax (maxDb, std::abs (punkt.y));
        }

    if (maxHz <= minHz || minHz <= 0.0f)
        return;

    const auto spanne = std::ceil (maxDb);
    auto flaeche = rahmen.reduced (34.0f, 20.0f);
    const auto nullLinie = flaeche.getCentreY();

    auto xVon = [&] (float hz)
    {
        return flaeche.getX() + std::log10 (hz / minHz) / std::log10 (maxHz / minHz) * flaeche.getWidth();
    };
    auto yVon = [&] (float db)
    {
        return nullLinie - (db / spanne) * (flaeche.getHeight() * 0.5f);
    };

    // Frequenzraster
    const int marken[] = { 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000 };
    g.setFont (NexusLookAndFeel::schrift (9.5f));

    for (int hz : marken)
    {
        if ((float) hz < minHz || (float) hz > maxHz)
            continue;

        const auto x = xVon ((float) hz);
        g.setColour (NexusFarben::rand.withAlpha (0.7f));
        g.drawLine (x, flaeche.getY(), x, flaeche.getBottom(), 1.0f);
        g.setColour (NexusFarben::textGedaempft.withAlpha (0.75f));
        g.drawText (hz >= 1000 ? String (hz / 1000) + "k" : String (hz),
                    Rectangle<float> (x - 16.0f, flaeche.getBottom() + 2.0f, 32.0f, 14.0f),
                    Justification::centred, false);
    }

    g.setColour (NexusFarben::textGedaempft.withAlpha (0.6f));
    g.drawLine (flaeche.getX(), nullLinie, flaeche.getRight(), nullLinie, 1.0f);
    g.drawText (String ("±") + String ((int) spanne) + " dB",
                rahmen.removeFromTop (16.0f).reduced (8.0f, 0.0f), Justification::topRight, false);

    float legendeX = flaeche.getX();

    for (auto* reihe : gueltig)
    {
        Path pfad;

        for (int i = 0; i < reihe->punkte.size(); ++i)
        {
            const auto x = xVon (reihe->punkte[i].x);
            const auto y = jlimit (flaeche.getY(), flaeche.getBottom(), yVon (reihe->punkte[i].y));

            if (i == 0)
                pfad.startNewSubPath (x, y);
            else
                pfad.lineTo (x, y);
        }

        g.setColour (reihe->farbe);
        g.strokePath (pfad, PathStrokeType (reihe->staerke, PathStrokeType::curved, PathStrokeType::rounded));

        g.setFont (NexusLookAndFeel::schrift (10.5f, true));
        g.drawText (reihe->titel, Rectangle<float> (legendeX, rahmen.getY() + 4.0f, 130.0f, 14.0f),
                    Justification::topLeft, false);
        legendeX += 130.0f;
    }
}

//== Restaurieren-Tab ============================================================

RestorePanel::RestorePanel (NexusBridgeProcessor& p) : prozessor (p)
{
    auto beschriftungEinrichten = [this] (Label& label)
    {
        label.setFont (NexusLookAndFeel::schrift (11.0f, true));
        label.setColour (Label::textColourId, NexusFarben::textGedaempft);
        addAndMakeVisible (label);
    };

    beschriftungEinrichten (quelleBeschriftung);
    beschriftungEinrichten (referenzBeschriftung);
    beschriftungEinrichten (limitBeschriftung);
    beschriftungEinrichten (staerkeBeschriftung);
    beschriftungEinrichten (schritteBeschriftung);

    addAndMakeVisible (quelleWahl);
    quelleWahl.setTextWhenNothingSelected ("noch nichts hochgeladen");
    addAndMakeVisible (referenzWahl);
    referenzWahl.setTextWhenNothingSelected (CharPointer_UTF8 ("— keine —"));

    addAndMakeVisible (hochladenKnopf);
    hochladenKnopf.onClick = [this] { hochladen (false); };
    addAndMakeVisible (referenzKnopf);
    referenzKnopf.onClick = [this] { hochladen (true); };
    addAndMakeVisible (clipKnopf);
    clipKnopf.onClick = [this] { clipHochladen(); };
    addAndMakeVisible (aufnahmeKnopf);
    aufnahmeKnopf.setTooltip ("Nimmt auf, was in das Plugin hineinlaeuft - also die Spur, auf der es liegt.");
    aufnahmeKnopf.onClick = [this] { aufnahmeUmschalten(); };

    for (auto* regler : { &limitRegler, &staerkeRegler, &schritteRegler })
    {
        regler->setSliderStyle (Slider::LinearHorizontal);
        regler->setTextBoxStyle (Slider::TextBoxRight, false, 54, 22);
        regler->setColour (Slider::trackColourId, NexusFarben::akzentDunkel);
        regler->setColour (Slider::thumbColourId, NexusFarben::akzent);
        regler->setColour (Slider::textBoxTextColourId, NexusFarben::text);
        regler->setColour (Slider::textBoxOutlineColourId, NexusFarben::rand);
        addAndMakeVisible (*regler);
    }

    limitRegler.setRange (1.0, 24.0, 1.0);
    limitRegler.setValue (12.0, dontSendNotification);
    limitRegler.setTextValueSuffix (" dB");
    staerkeRegler.setRange (0.05, 1.0, 0.05);
    staerkeRegler.setValue (1.0, dontSendNotification);
    schritteRegler.setRange (10.0, 200.0, 5.0);
    schritteRegler.setValue (50.0, dontSendNotification);

    addAndMakeVisible (entrauschenKnopf);
    entrauschenKnopf.setColour (ToggleButton::textColourId, NexusFarben::textGedaempft);
    entrauschenKnopf.setColour (ToggleButton::tickColourId, NexusFarben::akzent);
    entrauschenKnopf.setColour (ToggleButton::tickDisabledColourId, NexusFarben::rand);

    addAndMakeVisible (superresKnopf);
    superresKnopf.setColour (ToggleButton::textColourId, NexusFarben::textGedaempft);
    superresKnopf.setColour (ToggleButton::tickColourId, NexusFarben::akzent);
    superresKnopf.setColour (ToggleButton::tickDisabledColourId, NexusFarben::rand);
    superresKnopf.setTooltip ("Rechnet Inhalt oberhalb der Bandgrenze neu. Ohne CUDA auf der CPU - "
                              "ein Vielfaches der Spieldauer.");

    addAndMakeVisible (srModellWahl);
    srModellWahl.addItem ("speech", 1);
    srModellWahl.addItem ("basic", 2);
    srModellWahl.setSelectedId (1, dontSendNotification);

    addAndMakeVisible (startKnopf);
    startKnopf.setColour (TextButton::buttonColourId, NexusFarben::akzentDunkel);
    startKnopf.setColour (TextButton::textColourOffId, NexusFarben::hintergrund);
    startKnopf.onClick = [this] { starten(); };

    addAndMakeVisible (analyseKnopf);
    analyseKnopf.onClick = [this] { analysieren(); };

    addAndMakeVisible (statusZeile);
    statusZeile.setFont (NexusLookAndFeel::schrift (12.0f));
    statusZeile.setColour (Label::textColourId, NexusFarben::textGedaempft);

    addChildComponent (balken);
    balken.setPercentageDisplay (false);

    addAndMakeVisible (kurve);

    startTimerHz (4);
    statusAbrufen();
}

RestorePanel::~RestorePanel()
{
    stopTimer();
}

void RestorePanel::paint (Graphics& g)
{
    g.fillAll (NexusFarben::hintergrund);
}

void RestorePanel::resized()
{
    auto bereich = getLocalBounds().reduced (16);

    auto zeile = bereich.removeFromTop (46);
    auto links = zeile.removeFromLeft (zeile.getWidth() / 2 - 6);
    quelleBeschriftung.setBounds (links.removeFromTop (16));
    quelleWahl.setBounds (links.reduced (0, 1));
    zeile.removeFromLeft (12);
    referenzBeschriftung.setBounds (zeile.removeFromTop (16));
    referenzWahl.setBounds (zeile.reduced (0, 1));

    bereich.removeFromTop (8);
    auto knoepfe = bereich.removeFromTop (32);
    hochladenKnopf.setBounds (knoepfe.removeFromLeft (150));
    knoepfe.removeFromLeft (8);
    referenzKnopf.setBounds (knoepfe.removeFromLeft (150));
    knoepfe.removeFromLeft (8);
    clipKnopf.setBounds (knoepfe.removeFromLeft (190));

    bereich.removeFromTop (8);
    auto zweite = bereich.removeFromTop (32);
    aufnahmeKnopf.setBounds (zweite.removeFromLeft (230));

    bereich.removeFromTop (12);
    auto regler = bereich.removeFromTop (26);
    limitBeschriftung.setBounds (regler.removeFromLeft (72));
    limitRegler.setBounds (regler.removeFromLeft (jmax (140, regler.getWidth() / 2 - 40)));
    regler.removeFromLeft (12);
    staerkeBeschriftung.setBounds (regler.removeFromLeft (54));
    staerkeRegler.setBounds (regler);

    bereich.removeFromTop (6);
    entrauschenKnopf.setBounds (bereich.removeFromTop (26));

    bereich.removeFromTop (4);
    auto srZeile = bereich.removeFromTop (26);
    superresKnopf.setBounds (srZeile.removeFromLeft (250));
    srModellWahl.setBounds (srZeile.removeFromLeft (110).reduced (0, 1));
    srZeile.removeFromLeft (12);
    schritteBeschriftung.setBounds (srZeile.removeFromLeft (54));
    schritteRegler.setBounds (srZeile);

    bereich.removeFromTop (10);
    auto steuerung = bereich.removeFromTop (36);
    startKnopf.setBounds (steuerung.removeFromLeft (150).reduced (0, 2));
    steuerung.removeFromLeft (10);
    analyseKnopf.setBounds (steuerung.removeFromLeft (130).reduced (0, 2));
    steuerung.removeFromLeft (14);
    balken.setBounds (steuerung.removeFromBottom (10).reduced (0, 2));
    statusZeile.setBounds (steuerung);

    bereich.removeFromTop (10);
    kurve.setBounds (bereich);
}

void RestorePanel::meldung (const String& text, Colour farbe)
{
    statusZeile.setColour (Label::textColourId, farbe);
    statusZeile.setText (text, dontSendNotification);
}

void RestorePanel::arbeitAnzeigen (bool laeuft)
{
    arbeitLaeuft = laeuft;
    fortschritt = laeuft ? -1.0 : 0.0;
    balken.setVisible (laeuft);

    for (auto* knopf : { &startKnopf, &analyseKnopf, &hochladenKnopf, &referenzKnopf, &clipKnopf })
        knopf->setEnabled (! laeuft);

    quelleWahl.setEnabled (! laeuft);
    referenzWahl.setEnabled (! laeuft);
}

void RestorePanel::timerCallback()
{
    clipKnopf.setEnabled (! arbeitLaeuft && prozessor.aktuelleClipDatei().existsAsFile());

    const auto laeuft = prozessor.nimmtAuf();
    aufnahmeKnopf.setEnabled (! arbeitLaeuft);
    aufnahmeKnopf.setButtonText (laeuft
        ? "Aufnahme stoppen  ·  " + String (prozessor.aufnahmeSekunden(), 1) + " s"
        : String ("Von der Spur aufnehmen"));
    aufnahmeKnopf.setColour (TextButton::buttonColourId,
                             laeuft ? NexusFarben::fehler.withAlpha (0.35f) : NexusFarben::flaecheHell);
}

String RestorePanel::gewaehlt (const ComboBox& box) const
{
    // 1000 ist der Eintrag "keine" der Referenzliste.
    if (box.getSelectedId() == 1000)
        return {};

    const auto index = box.getSelectedId() - 1;

    if (auto* liste = eingaben.getArray())
        if (index >= 0 && index < liste->size())
            return liste->getReference (index).getProperty ("name", "").toString();

    return {};
}

Array<Point<float>> RestorePanel::punkteAus (const var& liste) const
{
    Array<Point<float>> punkte;

    if (auto* eintraege = liste.getArray())
        for (const auto& eintrag : *eintraege)
            punkte.add ({ (float) (double) eintrag.getProperty ("hz", 0.0),
                          (float) (double) eintrag.getProperty ("db", 0.0) });

    return punkte;
}

void RestorePanel::listenFuellen (const var& eintraege)
{
    eingaben = eintraege;

    const auto vorherQuelle = quelleWahl.getText();
    const auto vorherReferenz = referenzWahl.getText();

    quelleWahl.clear (dontSendNotification);
    referenzWahl.clear (dontSendNotification);
    referenzWahl.addItem (CharPointer_UTF8 ("— keine —"), 1000);

    if (auto* liste = eingaben.getArray())
    {
        for (int i = 0; i < liste->size(); ++i)
        {
            const auto name = liste->getReference (i).getProperty ("name", "").toString();
            quelleWahl.addItem (name, i + 1);
            referenzWahl.addItem (name, i + 1);

            if (name == vorherQuelle)
                quelleWahl.setSelectedId (i + 1, dontSendNotification);

            if (name == vorherReferenz)
                referenzWahl.setSelectedId (i + 1, dontSendNotification);
        }
    }

    if (quelleWahl.getSelectedId() == 0 && quelleWahl.getNumItems() > 0)
        quelleWahl.setSelectedId (1, dontSendNotification);

    if (referenzWahl.getSelectedId() == 0)
        referenzWahl.setSelectedId (1000, dontSendNotification);
}

void RestorePanel::statusAbrufen()
{
    auto api = prozessor.api();

    starteAuftrag<Antwort> (prozessor.waechter(),
        [api]
        {
            Antwort antwort;
            const auto ergebnis = api->get ("/restore/status");
            antwort.ok = ergebnis.ok;
            antwort.fehler = ergebnis.fehler;
            antwort.daten = ergebnis.daten;
            return antwort;
        },
        [this] (const Antwort& antwort)
        {
            if (! antwort.ok)
            {
                meldung (antwort.fehler == "bereich_nicht_freigegeben"
                             ? CharPointer_UTF8 ("Konto ohne Recht \"EQ & Restaurieren\"")
                             : antwort.fehler,
                         NexusFarben::fehler);
                return;
            }

            listenFuellen (antwort.daten.getProperty ("inputs", var()));

            const auto werkzeuge = antwort.daten.getProperty ("tools", var());
            const auto entrauschenDa = static_cast<bool> (werkzeuge.getProperty ("denoise", false));
            entrauschenKnopf.setEnabled (entrauschenDa);

            if (! entrauschenDa)
                entrauschenKnopf.setToggleState (false, dontSendNotification);

            const auto superresDa = static_cast<bool> (werkzeuge.getProperty ("superres", false));
            superresKnopf.setEnabled (superresDa);
            srModellWahl.setEnabled (superresDa);
            schritteRegler.setEnabled (superresDa);

            if (! superresDa)
                superresKnopf.setToggleState (false, dontSendNotification);

            meldung (static_cast<bool> (antwort.daten.getProperty ("busy", false))
                         ? CharPointer_UTF8 ("Dienst rechnet gerade an einem anderen Auftrag")
                         : CharPointer_UTF8 ("Bereit"),
                     NexusFarben::erfolg);
        });
}

void RestorePanel::hochladen (bool alsReferenz)
{
    dateiwahl = std::make_unique<FileChooser> (alsReferenz ? "Referenzaufnahme waehlen" : "Quelldatei waehlen",
                                               File::getSpecialLocation (File::userMusicDirectory),
                                               "*.wav;*.aif;*.aiff;*.mp3;*.m4a;*.flac;*.ogg;*.mp4;*.mov");

    dateiwahl->launchAsync (FileBrowserComponent::openMode | FileBrowserComponent::canSelectFiles,
        [this, alsReferenz] (const FileChooser& wahl)
        {
            const auto datei = wahl.getResult();

            if (datei == File())
                return;

            dateiHochladen (datei, alsReferenz, CharPointer_UTF8 ("Datei wird übertragen …"));
        });
}

void RestorePanel::dateiHochladen (const File& datei, bool alsReferenz, const String& hinweis)
{
    auto api = prozessor.api();
    StringPairArray felder;
    felder.set ("role", alsReferenz ? "reference" : "source");

    arbeitAnzeigen (true);
    meldung (hinweis, NexusFarben::akzent);

    starteAuftrag<Antwort> (prozessor.waechter(),
        [api, datei, felder]
        {
            Antwort antwort;
            const auto ergebnis = api->dateiHochladen ("/restore/upload", datei, "audio", felder);
            antwort.ok = ergebnis.ok;
            antwort.fehler = ergebnis.fehler;
            antwort.name = ApiClient::feldText (ergebnis.daten, "name");
            return antwort;
        },
        [this] (const Antwort& antwort)
        {
            arbeitAnzeigen (false);

            if (! antwort.ok)
            {
                meldung (antwort.fehler, NexusFarben::fehler);
                return;
            }

            meldung (antwort.name + " gespeichert", NexusFarben::erfolg);
            statusAbrufen();
        });
}

void RestorePanel::clipHochladen()
{
    const auto datei = prozessor.aktuelleClipDatei();

    if (! datei.existsAsFile())
    {
        meldung ("Kein Clip geladen", NexusFarben::warnung);
        return;
    }

    dateiHochladen (datei, false, CharPointer_UTF8 ("Clip wird übertragen …"));
}

void RestorePanel::aufnahmeUmschalten()
{
    if (prozessor.nimmtAuf())
    {
        const auto datei = prozessor.stoppAufnahme();

        if (! datei.existsAsFile() || datei.getSize() < 1024)
        {
            meldung (CharPointer_UTF8 ("Nichts aufgenommen - lief die Spur überhaupt?"), NexusFarben::warnung);
            return;
        }

        dateiHochladen (datei, false, CharPointer_UTF8 ("Aufnahme wird übertragen …"));
        return;
    }

    String fehler;

    if (! prozessor.starteAufnahme (fehler))
    {
        meldung (fehler, NexusFarben::fehler);
        return;
    }

    meldung (CharPointer_UTF8 ("Aufnahme läuft - jetzt die Spur abspielen, dann erneut drücken"),
             NexusFarben::akzent);
}

void RestorePanel::analysieren()
{
    const auto quelle = gewaehlt (quelleWahl);

    if (quelle.isEmpty())
    {
        meldung (CharPointer_UTF8 ("Keine Quelle gewählt"), NexusFarben::warnung);
        return;
    }

    DynamicObject::Ptr koerper (new DynamicObject());
    koerper->setProperty ("name", quelle);
    const var anfrage (koerper.get());
    auto api = prozessor.api();

    arbeitAnzeigen (true);
    meldung (CharPointer_UTF8 ("Spektrum wird berechnet …"), NexusFarben::akzent);

    starteAuftrag<Antwort> (prozessor.waechter(),
        [api, anfrage]
        {
            Antwort antwort;
            const auto ergebnis = api->post ("/restore/analyse", anfrage);
            antwort.ok = ergebnis.ok;
            antwort.fehler = ergebnis.fehler;
            antwort.daten = ergebnis.daten;
            return antwort;
        },
        [this] (const Antwort& antwort)
        {
            arbeitAnzeigen (false);

            if (! antwort.ok)
            {
                meldung (antwort.fehler, NexusFarben::fehler);
                return;
            }

            KurvenAnzeige::Reihe reihe;
            reihe.titel = "Quelle";
            reihe.farbe = NexusFarben::akzent;
            reihe.punkte = punkteAus (antwort.daten.getProperty ("spectrum", var()));
            kurve.setReihen ({ reihe });

            meldung (antwort.daten.getProperty ("sample_rate", 0).toString() + " Hz  ·  Signal bis etwa "
                         + antwort.daten.getProperty ("edge_hz", 0).toString() + " Hz",
                     NexusFarben::erfolg);
        });
}

void RestorePanel::starten()
{
    const auto quelle = gewaehlt (quelleWahl);
    const auto referenz = gewaehlt (referenzWahl);
    const auto entrauschen = entrauschenKnopf.getToggleState();
    const auto superres = superresKnopf.getToggleState();

    if (quelle.isEmpty())
    {
        meldung (CharPointer_UTF8 ("Keine Quelle gewählt"), NexusFarben::warnung);
        return;
    }

    if (referenz.isEmpty() && ! entrauschen && ! superres)
    {
        meldung (CharPointer_UTF8 ("Ohne Referenz, ohne Entrauschen und ohne Superresolution "
                                   "gibt es nichts zu tun"), NexusFarben::warnung);
        return;
    }

    DynamicObject::Ptr koerper (new DynamicObject());
    koerper->setProperty ("source", quelle);
    koerper->setProperty ("reference", referenz);
    koerper->setProperty ("denoise", entrauschen);
    koerper->setProperty ("superres", superres);
    koerper->setProperty ("sr_model", srModellWahl.getText());
    koerper->setProperty ("ddim_steps", (int) schritteRegler.getValue());
    koerper->setProperty ("limit_db", limitRegler.getValue());
    koerper->setProperty ("strength", staerkeRegler.getValue());
    const var anfrage (koerper.get());

    auto api = prozessor.api();
    const auto ordner = NexusBridgeProcessor::arbeitsOrdner();

    arbeitAnzeigen (true);
    meldung (superres ? CharPointer_UTF8 ("AudioSR rechnet auf der CPU - das dauert deutlich länger "
                                          "als die Spieldauer …")
                      : (entrauschen ? CharPointer_UTF8 ("DeepFilterNet rechnet …")
                                     : CharPointer_UTF8 ("Frequenzgang wird übertragen …")),
             NexusFarben::akzent);

    starteAuftrag<Antwort> (prozessor.waechter(),
        [api, anfrage, ordner]
        {
            Antwort antwort;
            const auto ergebnis = api->post ("/restore/run", anfrage);

            if (! ergebnis.ok)
            {
                antwort.fehler = ergebnis.fehler;
                return antwort;
            }

            antwort.daten = ergebnis.daten;
            antwort.name = ApiClient::feldText (ergebnis.daten, "name");

            const auto quellPfad = ApiClient::feldText (ergebnis.daten, "output");
            const auto url = ApiClient::feldText (ergebnis.daten, "url");
            const auto ziel = ordner.getChildFile (File::createLegalFileName (antwort.name));

            if (! api->holeDatei (url, quellPfad, ziel, antwort.fehler))
                return antwort;

            antwort.dateiPfad = ziel.getFullPathName();
            antwort.ok = true;
            return antwort;
        },
        [this] (const Antwort& antwort)
        {
            arbeitAnzeigen (false);

            if (! antwort.ok)
            {
                meldung (antwort.fehler, NexusFarben::fehler);
                return;
            }

            String fehler;

            if (! prozessor.ladeClip (File (antwort.dateiPfad), antwort.name, fehler))
            {
                meldung (fehler, NexusFarben::fehler);
                return;
            }

            KurvenAnzeige::Reihe q, r, k;
            q.titel = "Quelle";   q.farbe = NexusFarben::textGedaempft; q.staerke = 1.4f;
            q.punkte = punkteAus (antwort.daten.getProperty ("source_spectrum", var()));
            r.titel = "Referenz"; r.farbe = NexusFarben::warnung;       r.staerke = 1.4f;
            r.punkte = punkteAus (antwort.daten.getProperty ("reference_spectrum", var()));
            k.titel = "Kurve";    k.farbe = NexusFarben::akzent;        k.staerke = 2.4f;
            k.punkte = punkteAus (antwort.daten.getProperty ("curve", var()));
            kurve.setReihen ({ q, r, k });

            meldung ("Fertig in " + antwort.daten.getProperty ("elapsed_seconds", 0).toString()
                         + " s  ·  im Vorleser-Tab abspielen und in die Spur ziehen",
                     NexusFarben::erfolg);
            statusAbrufen();
        });
}
