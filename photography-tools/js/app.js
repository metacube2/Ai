/* ==================== PhotoPro Tools - Main Application ==================== */
(function() {
'use strict';

/* ==================== QUIZ QUESTIONS (all languages) ==================== */
var quizDB = {
de: [
{q:"Was bedeutet die Abk\u00fcrzung DOF?",o:["Depth of Field","Direction of Focus","Diameter of Filter","Digital Optical Format"],c:0,cat:"basics",e:"DOF steht f\u00fcr Depth of Field (Sch\u00e4rfentiefe) \u2013 den Bereich, der im Bild scharf erscheint."},
{q:"Was passiert, wenn man die Blende weiter \u00f6ffnet (kleinere f-Zahl)?",o:["Mehr Licht, geringere Sch\u00e4rfentiefe","Weniger Licht, gr\u00f6\u00dfere Sch\u00e4rfentiefe","Bild wird dunkler","Keine Ver\u00e4nderung"],c:0,cat:"basics",e:"Eine gr\u00f6\u00dfere Blenden\u00f6ffnung (z.B. f/1.8) l\u00e4sst mehr Licht ein und verringert die Sch\u00e4rfentiefe."},
{q:"Welcher ISO-Wert erzeugt das wenigste Rauschen?",o:["ISO 100","ISO 800","ISO 3200","ISO 6400"],c:0,cat:"basics",e:"Je niedriger der ISO-Wert, desto weniger digitales Rauschen im Bild."},
{q:"Was ist der Crop-Faktor?",o:["Verh\u00e4ltnis der Sensorgr\u00f6\u00dfe zum Vollformat","Maximale Vergr\u00f6\u00dferung","Bildkompression","Farbtiefe"],c:0,cat:"basics",e:"Der Crop-Faktor beschreibt das Verh\u00e4ltnis eines kleineren Sensors zum Vollformat (36x24mm)."},
{q:"Was misst die Brennweite eines Objektivs?",o:["Abstand Linse-Sensor bei Fokus auf Unendlich","L\u00e4nge des Objektivs","Maximale Sch\u00e4rfe","Lichtst\u00e4rke"],c:0,cat:"basics",e:"Die Brennweite ist der Abstand zwischen der optischen Mitte der Linse und dem Sensor, wenn auf unendlich fokussiert wird."},
{q:"Was ist ein Histogramm?",o:["Grafische Darstellung der Helligkeitsverteilung","Farbkreis","Sch\u00e4rfeanzeige","Weissabgleich-Kurve"],c:0,cat:"basics",e:"Das Histogramm zeigt die Verteilung von dunklen bis hellen Pixeln im Bild."},
{q:"Was bedeutet RAW in der Fotografie?",o:["Unkomprimiertes Rohdatenformat","Rote Augen Werkzeug","Schneller Autofokus","Bildstabilisierung"],c:0,cat:"basics",e:"RAW ist ein unkomprimiertes Format, das alle Sensordaten beh\u00e4lt f\u00fcr maximale Nachbearbeitung."},
{q:"Was ist der Wei\u00dfabgleich?",o:["Farbtemperatur-Anpassung","Belichtungskorrektur","Kontrasteinstellung","Sch\u00e4rfeoptimierung"],c:0,cat:"basics",e:"Der Wei\u00dfabgleich passt die Farbtemperatur an, damit Wei\u00df auch unter verschiedenen Lichtquellen wei\u00df erscheint."},
{q:"Was beschreibt die Lichtwert-Skala (EV)?",o:["Kombination aus Blende und Verschlusszeit","Nur die Blende","Nur ISO","Sensorgr\u00f6\u00dfe"],c:0,cat:"basics",e:"EV (Exposure Value) beschreibt \u00e4quivalente Kombinationen von Blende und Verschlusszeit bei gleicher Belichtung."},
{q:"Was ist Bokeh?",o:["\u00c4sthetische Qualit\u00e4t der Unsch\u00e4rfe","Ein Objektivtyp","Kamera-Marke","Bildformat"],c:0,cat:"basics",e:"Bokeh beschreibt die \u00e4sthetische Qualit\u00e4t der unscharfen Bereiche im Bild."},
{q:"Was besagt die Drittel-Regel?",o:["Bild in 9 Felder teilen, Motive auf Linien platzieren","Immer 3 Motive fotografieren","Bild in 3 Farben teilen","3 Fotos pro Motiv machen"],c:0,cat:"composition",e:"Die Drittel-Regel teilt das Bild mit 2 horizontalen und 2 vertikalen Linien. Wichtige Elemente werden auf Linien oder Schnittpunkten platziert."},
{q:"Was ist der Goldene Schnitt?",o:["Teilungsverh\u00e4ltnis von ca. 1:1,618","Exakt 1:2","Bilddiagonale","Farbharmonie"],c:0,cat:"composition",e:"Der Goldene Schnitt teilt eine Strecke so, dass das Verh\u00e4ltnis des Ganzen zum gr\u00f6\u00dferen Teil gleich dem des gr\u00f6\u00dferen zum kleineren Teil ist (~1:1,618)."},
{q:"Was sind f\u00fchrende Linien in der Komposition?",o:["Linien die den Blick zum Motiv lenken","Gerade Horizonte","Rahmenlinien","Textlinien"],c:0,cat:"composition",e:"F\u00fchrende Linien sind visuelle Elemente (Stra\u00dfen, Fl\u00fcsse, Gel\u00e4nder), die den Blick des Betrachters zum Hauptmotiv leiten."},
{q:"Was bewirkt negativer Raum?",o:["Erzeugt Wirkung und Fokus auf das Motiv","Macht das Bild langweilig","F\u00fcllt das Bild","Erh\u00f6ht die S\u00e4ttigung"],c:0,cat:"composition",e:"Negativer Raum gibt dem Motiv 'Luft zum Atmen' und lenkt die Aufmerksamkeit."},
{q:"Welche Farben sind komplement\u00e4r?",o:["Gegen\u00fcberliegend im Farbkreis","Nebeneinander im Farbkreis","Nur Schwarz und Wei\u00df","Nur Prim\u00e4rfarben"],c:0,cat:"composition",e:"Komplement\u00e4rfarben liegen sich im Farbkreis gegen\u00fcber (z.B. Blau-Orange, Rot-Gr\u00fcn) und erzeugen starken Kontrast."},
{q:"Was bewirken Diagonalen in der Bildkomposition?",o:["Dynamik und Spannung","Ruhe und Harmonie","Symmetrie","Tiefenunsch\u00e4rfe"],c:0,cat:"composition",e:"Diagonale Linien erzeugen ein Gef\u00fchl von Bewegung, Dynamik und Energie im Bild."},
{q:"Was ist nat\u00fcrliches Framing?",o:["Motiv durch Szene-Elemente einrahmen","Bilderrahmen kaufen","Kamerageh\u00e4use","Crop-Funktion"],c:0,cat:"composition",e:"Nat\u00fcrliches Framing nutzt vorhandene Elemente (T\u00fcrb\u00f6gen, \u00c4ste, Fenster) um das Motiv einzurahmen."},
{q:"Wann sollte man die Drittel-Regel brechen?",o:["Bei Symmetrie oder bewusstem Stilmittel","Nie","Immer","Nur bei Portr\u00e4ts"],c:0,cat:"composition",e:"Regeln sind Richtlinien. Bei perfekter Symmetrie oder gewolltem k\u00fcnstlerischem Ausdruck kann Zentrieren effektiver sein."},
{q:"Was erzeugt Tiefe im Bild?",o:["Vordergrund, Mittelgrund, Hintergrund","Nur Weitwinkel","Nur Telelinsen","Hohe ISO"],c:0,cat:"composition",e:"Die Schichtung von Vordergrund, Mittelgrund und Hintergrund erzeugt r\u00e4umliche Tiefe im zweidimensionalen Bild."},
{q:"Was ist die Bedeutung von Linien in der Fotografie?",o:["Sie f\u00fchren den Blick und strukturieren das Bild","Nur dekorativ","Technischer Fehler","Belichtungskorrektur"],c:0,cat:"composition",e:"Linien sind eines der st\u00e4rksten Kompositionselemente \u2013 sie f\u00fchren den Blick, teilen den Raum und erzeugen Stimmung."},
{q:"Was ist ein Normalobjektiv?",o:["Ca. 50mm Brennweite (Vollformat)","Jedes Zoom","10mm","200mm"],c:0,cat:"lens",e:"Ein Normalobjektiv (~50mm am Vollformat) entspricht ungef\u00e4hr dem nat\u00fcrlichen Sichtfeld des menschlichen Auges."},
{q:"Welche Brennweite eignet sich f\u00fcr Portr\u00e4ts?",o:["85\u2013135mm","10\u201316mm","400\u2013600mm","Fisheye"],c:0,cat:"lens",e:"85\u2013135mm erzeugt eine schmeichelhafte Perspektive und sch\u00f6nes Bokeh bei Portr\u00e4ts."},
{q:"Was ist ein Weitwinkelobjektiv?",o:["Brennweite unter 35mm","Brennweite \u00fcber 200mm","Genau 50mm","Festbrennweite"],c:0,cat:"lens",e:"Weitwinkelobjektive haben Brennweiten unter ca. 35mm und erfassen einen gro\u00dfen Bildwinkel."},
{q:"Was bedeutet 'Lichtst\u00e4rke' bei einem Objektiv?",o:["Maximale Blenden\u00f6ffnung","Gewicht des Objektivs","Brennweite","Autofokus-Geschwindigkeit"],c:0,cat:"lens",e:"Die Lichtst\u00e4rke gibt die maximale Blenden\u00f6ffnung an (z.B. f/1.4). Je kleiner die Zahl, desto lichtst\u00e4rker."},
{q:"Was ist die hyperfokale Distanz?",o:["Fokuspunkt f\u00fcr maximale Sch\u00e4rfe bis Unendlich","K\u00fcrzeste Fokusdistanz","Maximale Brennweite","Blendenwert"],c:0,cat:"lens",e:"Die hyperfokale Distanz ist die Entfernung, bei der alles von der halben Distanz bis Unendlich scharf ist."},
{q:"Was bewirkt ein Polfilter?",o:["Reduziert Reflexionen und erh\u00f6ht S\u00e4ttigung","Verst\u00e4rkt Reflexionen","Vergr\u00f6\u00dfert das Bild","Reduziert Rauschen"],c:0,cat:"lens",e:"Ein Polarisationsfilter reduziert Reflexionen auf nicht-metallischen Oberfl\u00e4chen und verst\u00e4rkt Farben (besonders Himmel)."},
{q:"Was bedeutet IS/VR/OIS?",o:["Bildstabilisierung","Infrarot-Sensor","Video-Recording","ISO-Regulierung"],c:0,cat:"lens",e:"IS (Canon), VR (Nikon), OIS (andere) bezeichnen Bildstabilisierungssysteme im Objektiv."},
{q:"Was ist der Abbildungsma\u00dfstab 1:1?",o:["Motiv wird in Originalgr\u00f6\u00dfe auf Sensor abgebildet","Doppelte Vergr\u00f6\u00dferung","Halbe Gr\u00f6\u00dfe","Panorama"],c:0,cat:"lens",e:"Bei 1:1 wird das Motiv in seiner tats\u00e4chlichen Gr\u00f6\u00dfe auf dem Sensor abgebildet \u2013 echtes Makro."},
{q:"Wof\u00fcr eignet sich ein Teleobjektiv?",o:["Entfernte Motive, Sport, Wildlife","Innenr\u00e4ume","Makro","Architektur-Weitwinkel"],c:0,cat:"lens",e:"Teleobjektive (>70mm) eignen sich f\u00fcr entfernte Motive, komprimieren die Perspektive und erzeugen Freistellung."},
{q:"Was ist ein Tilt-Shift-Objektiv?",o:["Perspektivkorrektur und Sch\u00e4rfeverlagerung","Zoom-Objektiv","Fischauge","Standard-Festbrennweite"],c:0,cat:"lens",e:"Tilt-Shift-Objektive erm\u00f6glichen Perspektivkorrektur (Shift) und selektive Sch\u00e4rfeverlagerung (Tilt)."},
{q:"Was passiert bei l\u00e4ngerer Verschlusszeit?",o:["Mehr Licht, Bewegungsunsch\u00e4rfe m\u00f6glich","Weniger Licht","H\u00f6here Sch\u00e4rfe","Kleinerer Bildwinkel"],c:0,cat:"exposure",e:"L\u00e4ngere Verschlusszeiten lassen mehr Licht ein, k\u00f6nnen aber Bewegungsunsch\u00e4rfe verursachen."},
{q:"Was ist die Belichtungskorrektur?",o:["Gezielte Über- oder Unterbelichtung","ISO-Einstellung","Wei\u00dfabgleich","Objektivkorrektur"],c:0,cat:"exposure",e:"Die Belichtungskorrektur (\u00b1EV) erlaubt bewusste Abweichung von der gemessenen Belichtung."},
{q:"Was ist Matrixmessung?",o:["Belichtungsmessung \u00fcber das gesamte Bild","Messung nur im Zentrum","Spotmessung","Manuelle Messung"],c:0,cat:"exposure",e:"Die Matrixmessung (Nikon) / Mehrfeldmessung bewertet die Helligkeit des gesamten Bildes f\u00fcr die Belichtung."},
{q:"Wann verwendet man Spotmessung?",o:["Bei starkem Kontrast, Gegenlicht","Bei gleichm\u00e4\u00dfigem Licht","Immer","Nur bei Landschaften"],c:0,cat:"exposure",e:"Spotmessung misst nur einen kleinen Bereich (~2-5%) und eignet sich bei kontrastreichen Szenen."},
{q:"Was ist Belichtungsreihe (Bracketing)?",o:["Mehrere Aufnahmen mit verschiedenen Belichtungen","Serienbildmodus","Einzelaufnahme","Langzeitbelichtung"],c:0,cat:"exposure",e:"Bracketing erstellt mehrere Aufnahmen mit unterschiedlicher Belichtung, oft f\u00fcr HDR verwendet."},
{q:"Was bedeutet 'Blitzen auf den zweiten Verschlussvorhang'?",o:["Blitz z\u00fcndet am Ende der Belichtung","Blitz am Anfang","Dauerlicht","Kein Blitz"],c:0,cat:"exposure",e:"Der Blitz z\u00fcndet kurz vor dem Schlie\u00dfen des Verschlusses, sodass Bewegungsspuren hinter dem Motiv erscheinen."},
{q:"Was ist die Sunny-16-Regel?",o:["Bei Sonne: f/16, Verschlusszeit = 1/ISO","Immer f/16 verwenden","ISO 16","16mm Brennweite"],c:0,cat:"exposure",e:"Die Sunny-16-Regel: Bei hellem Sonnenlicht ergibt f/16 mit 1/ISO als Verschlusszeit eine korrekte Belichtung."},
{q:"Was ist High-Key-Fotografie?",o:["Bewusst helle, \u00fcberbelichtete Bildstimmung","Sehr dunkle Bilder","Nur Schwarz-Wei\u00df","Makrofotografie"],c:0,cat:"exposure",e:"High-Key nutzt bewusst helle T\u00f6ne und \u00dcberbelichtung f\u00fcr eine leichte, luftige Stimmung."},
{q:"Was bewirkt ein ND-Filter?",o:["Reduziert die Lichtmenge f\u00fcr lange Belichtungszeiten","Erh\u00f6ht die Lichtmenge","Polfilter-Effekt","UV-Schutz"],c:0,cat:"exposure",e:"ND-Filter (Neutraldichte) reduzieren das einfallende Licht, erm\u00f6glichen l\u00e4ngere Verschlusszeiten bei Tageslicht."},
{q:"Was ist die Reziprozit\u00e4tsregel?",o:["Verschlusszeit mindestens 1/Brennweite f\u00fcr Freihand","Blende = Brennweite","ISO = Verschlusszeit","Nur f\u00fcr Stativ"],c:0,cat:"exposure",e:"F\u00fcr verwacklungsfreie Freihandaufnahmen sollte die Verschlusszeit mindestens 1/Brennweite betragen."},
{q:"Welche Einstellungen f\u00fcr Portr\u00e4tfotografie?",o:["f/1.4\u2013f/2.8, 85mm, niedriger ISO","f/16, 16mm, ISO 6400","f/22, 200mm, ISO 100","f/8, 24mm, ISO 3200"],c:0,cat:"genres",e:"Portr\u00e4ts: Offene Blende f\u00fcr Hintergrundunsch\u00e4rfe, mittlere Telebrennweite, niedriger ISO."},
{q:"Welche Einstellungen f\u00fcr Landschaftsfotografie?",o:["f/8\u2013f/16, Weitwinkel, Stativ, niedriger ISO","f/1.4, 200mm, ISO 6400","f/2.8, 50mm, hoher ISO","Automatik"],c:0,cat:"genres",e:"Landschaften: Geschlossene Blende f\u00fcr durchgehende Sch\u00e4rfe, Weitwinkel, Stativ."},
{q:"Was ist wichtig bei Sportfotografie?",o:["Schnelle Verschlusszeit, Serienbildmodus","Lange Belichtung, Stativ","Kleine Blende, niedriger ISO","Nur Weitwinkel"],c:0,cat:"genres",e:"Sport erfordert schnelle Verschlusszeiten (1/500+), guten Autofokus und oft Teleobjektive."},
{q:"Welches Objektiv f\u00fcr Architekturfotografie?",o:["Weitwinkel oder Tilt-Shift","Fisheye","500mm Tele","50mm Normal"],c:0,cat:"genres",e:"Weitwinkelobjektive oder Tilt-Shift-Objektive f\u00fcr st\u00fcrzende Linien und gro\u00dfe Geb\u00e4ude."},
{q:"Was ist wichtig bei Nachtfotografie?",o:["Stativ, offene Blende, hoher ISO, lange Belichtung","Blitz verwenden","Kleiner ISO, kurze Belichtung","Nur bei Vollmond"],c:0,cat:"genres",e:"Nachtfotografie erfordert Stativ, offene Blende (f/1.4\u2013f/2.8), erh\u00f6hten ISO und lange Belichtungszeiten."},
{q:"Was zeichnet Makrofotografie aus?",o:["Abbildungsma\u00dfstab 1:1 oder gr\u00f6\u00dfer","Nur Landschaften","Weitwinkelaufnahmen","Schnelle Verschlusszeit"],c:0,cat:"genres",e:"Makrofotografie bildet kleine Motive im Ma\u00dfstab 1:1 oder gr\u00f6\u00dfer ab. Erfordert spezielle Makro-Objektive."},
{q:"Welche Technik f\u00fcr Wildlife-Fotografie?",o:["Langes Tele, schneller AF, Geduld","Weitwinkel, langsamer AF","Blitz, Nahaufnahme","Nur Smartphone"],c:0,cat:"genres",e:"Wildlife erfordert lange Telebrennweiten (200\u2013600mm), schnellen Autofokus und viel Geduld."},
{q:"Was ist Light Painting?",o:["Malen mit Licht bei Langzeitbelichtung","Bilder am Computer bearbeiten","Blitzfotografie","HDR-Technik"],c:0,cat:"genres",e:"Light Painting nutzt Langzeitbelichtung und bewegte Lichtquellen um 'mit Licht zu malen'."},
{q:"Was ist Street Photography?",o:["Spontane Dokumentation des Alltagslebens","Nur Stra\u00dfen fotografieren","Studio-Portr\u00e4ts","Landschaftsfotografie"],c:0,cat:"genres",e:"Street Photography dokumentiert spontan und authentisch das Alltagsleben im \u00f6ffentlichen Raum."},
{q:"Welche Kameraeinstellung f\u00fcr Feuerwerk?",o:["Stativ, f/8\u2013f/11, 2\u20134 Sek., ISO 100","Automatik","f/1.4, 1/4000s, ISO 6400","Nur mit Blitz"],c:0,cat:"genres",e:"Feuerwerk: Stativ, mittlere Blende (f/8\u2013f/11), mehrere Sekunden Belichtung, niedriger ISO."}
],
en: [
{q:"What does the abbreviation DOF stand for?",o:["Depth of Field","Direction of Focus","Diameter of Filter","Digital Optical Format"],c:0,cat:"basics",e:"DOF stands for Depth of Field \u2013 the range that appears sharp in the image."},
{q:"What happens when you open the aperture wider (smaller f-number)?",o:["More light, shallower depth of field","Less light, greater depth of field","Image becomes darker","No change"],c:0,cat:"basics",e:"A larger aperture opening (e.g., f/1.8) lets in more light and reduces depth of field."},
{q:"Which ISO value produces the least noise?",o:["ISO 100","ISO 800","ISO 3200","ISO 6400"],c:0,cat:"basics",e:"The lower the ISO value, the less digital noise in the image."},
{q:"What is the crop factor?",o:["Ratio of sensor size to full frame","Maximum magnification","Image compression","Color depth"],c:0,cat:"basics",e:"The crop factor describes the ratio of a smaller sensor to full frame (36x24mm)."},
{q:"What does the focal length of a lens measure?",o:["Distance from lens to sensor at infinity focus","Length of the lens","Maximum sharpness","Light intensity"],c:0,cat:"basics",e:"Focal length is the distance between the optical center of the lens and the sensor when focused at infinity."},
{q:"What is a histogram?",o:["Graphical display of brightness distribution","Color wheel","Sharpness indicator","White balance curve"],c:0,cat:"basics",e:"The histogram shows the distribution of pixels from dark to bright in the image."},
{q:"What does RAW mean in photography?",o:["Uncompressed raw data format","Red eye tool","Rapid autofocus","Image stabilization"],c:0,cat:"basics",e:"RAW is an uncompressed format that retains all sensor data for maximum post-processing."},
{q:"What is white balance?",o:["Color temperature adjustment","Exposure correction","Contrast setting","Sharpness optimization"],c:0,cat:"basics",e:"White balance adjusts color temperature so white appears white under various light sources."},
{q:"What does the EV (Exposure Value) scale describe?",o:["Combination of aperture and shutter speed","Only aperture","Only ISO","Sensor size"],c:0,cat:"basics",e:"EV describes equivalent combinations of aperture and shutter speed at the same exposure."},
{q:"What is bokeh?",o:["Aesthetic quality of blur","A lens type","Camera brand","Image format"],c:0,cat:"basics",e:"Bokeh describes the aesthetic quality of the out-of-focus areas in an image."},
{q:"What does the Rule of Thirds state?",o:["Divide image into 9 parts, place subjects on lines","Always photograph 3 subjects","Divide image into 3 colors","Take 3 photos per subject"],c:0,cat:"composition",e:"The Rule of Thirds divides the image with 2 horizontal and 2 vertical lines. Key elements are placed on lines or intersections."},
{q:"What is the Golden Ratio?",o:["Division ratio of approximately 1:1.618","Exactly 1:2","Image diagonal","Color harmony"],c:0,cat:"composition",e:"The Golden Ratio divides a line so the ratio of the whole to the larger part equals the larger to the smaller (~1:1.618)."},
{q:"What are leading lines in composition?",o:["Lines that guide the eye to the subject","Straight horizons","Frame lines","Text lines"],c:0,cat:"composition",e:"Leading lines are visual elements (roads, rivers, railings) that guide the viewer's eye to the main subject."},
{q:"What does negative space achieve?",o:["Creates impact and focus on the subject","Makes the image boring","Fills the image","Increases saturation"],c:0,cat:"composition",e:"Negative space gives the subject 'room to breathe' and draws attention."},
{q:"Which colors are complementary?",o:["Opposite on the color wheel","Adjacent on the color wheel","Only black and white","Only primary colors"],c:0,cat:"composition",e:"Complementary colors are opposite each other on the color wheel (e.g., blue-orange) and create strong contrast."},
{q:"What do diagonals create in composition?",o:["Dynamics and tension","Calm and harmony","Symmetry","Depth of field"],c:0,cat:"composition",e:"Diagonal lines create a sense of movement, dynamism and energy in the image."},
{q:"What is natural framing?",o:["Frame subject with scene elements","Buy picture frames","Camera body","Crop function"],c:0,cat:"composition",e:"Natural framing uses existing elements (archways, branches, windows) to frame the subject."},
{q:"When should you break the Rule of Thirds?",o:["For symmetry or deliberate artistic effect","Never","Always","Only for portraits"],c:0,cat:"composition",e:"Rules are guidelines. With perfect symmetry or intentional artistic expression, centering can be more effective."},
{q:"What creates depth in an image?",o:["Foreground, middle ground, background","Only wide-angle","Only telephoto","High ISO"],c:0,cat:"composition",e:"Layering foreground, middle ground and background creates spatial depth in the two-dimensional image."},
{q:"What is the importance of lines in photography?",o:["They guide the eye and structure the image","Only decorative","Technical error","Exposure correction"],c:0,cat:"composition",e:"Lines are one of the strongest compositional elements \u2013 they guide the eye, divide space and create mood."},
{q:"What is a normal lens?",o:["Approximately 50mm focal length (full frame)","Any zoom","10mm","200mm"],c:0,cat:"lens",e:"A normal lens (~50mm on full frame) roughly matches the natural field of view of the human eye."},
{q:"Which focal length is best for portraits?",o:["85\u2013135mm","10\u201316mm","400\u2013600mm","Fisheye"],c:0,cat:"lens",e:"85\u2013135mm creates a flattering perspective and beautiful bokeh for portraits."},
{q:"What is a wide-angle lens?",o:["Focal length under 35mm","Focal length over 200mm","Exactly 50mm","Prime lens"],c:0,cat:"lens",e:"Wide-angle lenses have focal lengths below about 35mm and capture a wide field of view."},
{q:"What does 'fast lens' mean?",o:["Large maximum aperture","Lightweight lens","Fast autofocus","Short focal length"],c:0,cat:"lens",e:"A fast lens has a large maximum aperture (e.g., f/1.4). The smaller the number, the faster the lens."},
{q:"What is hyperfocal distance?",o:["Focus point for maximum sharpness to infinity","Shortest focus distance","Maximum focal length","Aperture value"],c:0,cat:"lens",e:"Hyperfocal distance is where everything from half that distance to infinity appears sharp."},
{q:"What does a polarizing filter do?",o:["Reduces reflections and increases saturation","Increases reflections","Magnifies the image","Reduces noise"],c:0,cat:"lens",e:"A polarizing filter reduces reflections on non-metallic surfaces and enhances colors (especially sky)."},
{q:"What does IS/VR/OIS mean?",o:["Image stabilization","Infrared sensor","Video recording","ISO regulation"],c:0,cat:"lens",e:"IS (Canon), VR (Nikon), OIS (others) denote image stabilization systems in lenses."},
{q:"What is 1:1 magnification ratio?",o:["Subject projected at actual size on sensor","Double magnification","Half size","Panorama"],c:0,cat:"lens",e:"At 1:1, the subject is reproduced at its actual size on the sensor \u2013 true macro."},
{q:"What is a telephoto lens best for?",o:["Distant subjects, sports, wildlife","Interiors","Macro","Wide-angle architecture"],c:0,cat:"lens",e:"Telephoto lenses (>70mm) suit distant subjects, compress perspective and create subject isolation."},
{q:"What is a tilt-shift lens?",o:["Perspective correction and focus plane control","Zoom lens","Fisheye","Standard prime"],c:0,cat:"lens",e:"Tilt-shift lenses allow perspective correction (shift) and selective focus plane control (tilt)."},
{q:"What happens with longer shutter speed?",o:["More light, possible motion blur","Less light","Higher sharpness","Smaller field of view"],c:0,cat:"exposure",e:"Longer shutter speeds let in more light but can cause motion blur."},
{q:"What is exposure compensation?",o:["Deliberate over- or underexposure","ISO setting","White balance","Lens correction"],c:0,cat:"exposure",e:"Exposure compensation (\u00b1EV) allows deliberate deviation from the metered exposure."},
{q:"What is matrix metering?",o:["Exposure metering across the entire image","Center-only metering","Spot metering","Manual metering"],c:0,cat:"exposure",e:"Matrix/evaluative metering evaluates brightness across the entire image for exposure."},
{q:"When should you use spot metering?",o:["In high contrast or backlit situations","In even lighting","Always","Only for landscapes"],c:0,cat:"exposure",e:"Spot metering measures only a small area (~2-5%) and suits high-contrast scenes."},
{q:"What is exposure bracketing?",o:["Multiple shots at different exposures","Burst mode","Single shot","Long exposure"],c:0,cat:"exposure",e:"Bracketing takes multiple shots at different exposures, often used for HDR."},
{q:"What is rear-curtain sync flash?",o:["Flash fires at end of exposure","Flash at beginning","Continuous light","No flash"],c:0,cat:"exposure",e:"Flash fires just before the shutter closes, so motion trails appear behind the subject."},
{q:"What is the Sunny 16 rule?",o:["In sun: f/16, shutter = 1/ISO","Always use f/16","ISO 16","16mm focal length"],c:0,cat:"exposure",e:"Sunny 16: In bright sunlight, f/16 with 1/ISO as shutter speed gives correct exposure."},
{q:"What is high-key photography?",o:["Deliberately bright, overexposed mood","Very dark images","Only B&W","Macro photography"],c:0,cat:"exposure",e:"High-key uses deliberately bright tones and overexposure for a light, airy mood."},
{q:"What does an ND filter do?",o:["Reduces light for longer exposures","Increases light","Polarizer effect","UV protection"],c:0,cat:"exposure",e:"ND (neutral density) filters reduce incoming light, enabling longer shutter speeds in daylight."},
{q:"What is the reciprocal rule?",o:["Shutter speed at least 1/focal length for handheld","Aperture = focal length","ISO = shutter speed","Tripod only"],c:0,cat:"exposure",e:"For sharp handheld shots, shutter speed should be at least 1/focal length."},
{q:"What settings for portrait photography?",o:["f/1.4\u2013f/2.8, 85mm, low ISO","f/16, 16mm, ISO 6400","f/22, 200mm, ISO 100","f/8, 24mm, ISO 3200"],c:0,cat:"genres",e:"Portraits: Wide aperture for background blur, medium telephoto, low ISO."},
{q:"What settings for landscape photography?",o:["f/8\u2013f/16, wide-angle, tripod, low ISO","f/1.4, 200mm, ISO 6400","f/2.8, 50mm, high ISO","Auto mode"],c:0,cat:"genres",e:"Landscapes: Narrow aperture for front-to-back sharpness, wide-angle, tripod."},
{q:"What matters in sports photography?",o:["Fast shutter speed, burst mode","Long exposure, tripod","Small aperture, low ISO","Only wide-angle"],c:0,cat:"genres",e:"Sports require fast shutter speeds (1/500+), good autofocus and often telephoto lenses."},
{q:"Which lens for architecture?",o:["Wide-angle or tilt-shift","Fisheye","500mm telephoto","50mm normal"],c:0,cat:"genres",e:"Wide-angle or tilt-shift lenses for correcting converging lines and capturing large buildings."},
{q:"What matters in night photography?",o:["Tripod, wide aperture, high ISO, long exposure","Use flash","Low ISO, short exposure","Only at full moon"],c:0,cat:"genres",e:"Night photography needs tripod, wide aperture (f/1.4\u2013f/2.8), higher ISO and long exposures."},
{q:"What characterizes macro photography?",o:["Magnification ratio 1:1 or greater","Only landscapes","Wide-angle shots","Fast shutter"],c:0,cat:"genres",e:"Macro photography reproduces small subjects at 1:1 or greater. Requires dedicated macro lenses."},
{q:"What technique for wildlife photography?",o:["Long telephoto, fast AF, patience","Wide-angle, slow AF","Flash, close-up","Smartphone only"],c:0,cat:"genres",e:"Wildlife requires long telephotos (200\u2013600mm), fast autofocus and lots of patience."},
{q:"What is light painting?",o:["Painting with light during long exposure","Editing on computer","Flash photography","HDR technique"],c:0,cat:"genres",e:"Light painting uses long exposure and moving light sources to 'paint with light'."},
{q:"What is street photography?",o:["Spontaneous documentation of everyday life","Only photographing streets","Studio portraits","Landscape photography"],c:0,cat:"genres",e:"Street photography spontaneously and authentically documents everyday life in public spaces."},
{q:"What camera settings for fireworks?",o:["Tripod, f/8\u2013f/11, 2\u20134 sec, ISO 100","Auto mode","f/1.4, 1/4000s, ISO 6400","Only with flash"],c:0,cat:"genres",e:"Fireworks: Tripod, medium aperture (f/8\u2013f/11), several seconds exposure, low ISO."}
]
};
// Copy EN to other langs as fallback (quiz content stays same structure)
quizDB.fr = quizDB.en; quizDB.it = quizDB.en; quizDB.sr = quizDB.en; quizDB.sq = quizDB.en;

/* ==================== GLOBALS ==================== */
var quizState = { questions: [], current: 0, score: 0, answers: [], timer: null, seconds: 0 };
var RULES = ['thirds','golden','leading','symmetry','framing','negative','diagonal','color'];
var MOTIFS = [
  {id:'portrait',icon:'\ud83d\udc64',lens:'85mm',aperture:'f/1.4\u2013f/2.8',iso:'100\u2013400',shutter:'1/125\u20131/250'},
  {id:'landscape',icon:'\ud83c\udfd4\ufe0f',lens:'16\u201335mm',aperture:'f/8\u2013f/16',iso:'100',shutter:'1/30\u2013s'},
  {id:'street',icon:'\ud83c\udfd9\ufe0f',lens:'35\u201350mm',aperture:'f/5.6\u2013f/8',iso:'400\u20131600',shutter:'1/125\u20131/500'},
  {id:'macro',icon:'\ud83d\udd0d',lens:'90\u2013105mm',aperture:'f/8\u2013f/16',iso:'100\u2013400',shutter:'1/200+'},
  {id:'night',icon:'\ud83c\udf19',lens:'14\u201324mm',aperture:'f/1.4\u2013f/2.8',iso:'1600\u20136400',shutter:'15\u201330s'},
  {id:'sport',icon:'\u26a1',lens:'70\u2013200mm',aperture:'f/2.8\u2013f/4',iso:'800\u20133200',shutter:'1/500\u20131/2000'},
  {id:'architecture',icon:'\ud83c\udfdb\ufe0f',lens:'14\u201324mm',aperture:'f/8\u2013f/11',iso:'100',shutter:'1/60\u2013s'},
  {id:'wildlife',icon:'\ud83e\udd81',lens:'200\u2013600mm',aperture:'f/4\u2013f/5.6',iso:'400\u20133200',shutter:'1/500\u20131/2000'}
];

/* ==================== INIT ==================== */
document.addEventListener('DOMContentLoaded', function() {
    initNav();
    initLangSwitcher();
    setLanguage(window.currentLang || 'de');
    initParticles();
    initCalcTabs();
    initRangeSync();
    initCounters();
    renderRules();
    renderMotifs();
    renderQuizCategories();
    drawExposureTriangle();
    initExposureControls();
    setTimeout(function(){ drawAllRuleDemos(); }, 300);
    document.addEventListener('languageChanged', function() {
        renderRules();
        renderMotifs();
        renderQuizCategories();
        setTimeout(drawAllRuleDemos, 200);
        drawExposureTriangle();
    });
});

/* ==================== NAVIGATION ==================== */
function initNav() {
    window.addEventListener('scroll', function() {
        var nb = document.getElementById('navbar');
        if (nb) nb.classList.toggle('scrolled', window.scrollY > 50);
    });
    var toggle = document.getElementById('navToggle');
    var menu = document.getElementById('navMenu');
    if (toggle && menu) {
        toggle.addEventListener('click', function() { menu.classList.toggle('open'); });
        menu.querySelectorAll('.nav-link').forEach(function(a) {
            a.addEventListener('click', function() { menu.classList.remove('open'); });
        });
    }
}

function initLangSwitcher() {
    var sw = document.getElementById('langSwitcher');
    var cur = document.getElementById('langCurrent');
    if (!sw || !cur) return;
    cur.addEventListener('click', function(e) { e.stopPropagation(); sw.classList.toggle('open'); });
    sw.querySelectorAll('.lang-option').forEach(function(btn) {
        btn.addEventListener('click', function() {
            setLanguage(btn.getAttribute('data-lang'));
            sw.classList.remove('open');
        });
    });
    document.addEventListener('click', function() { sw.classList.remove('open'); });
}

/* ==================== PARTICLES ==================== */
function initParticles() {
    var container = document.getElementById('heroParticles');
    if (!container) return;
    for (var i = 0; i < 30; i++) {
        var p = document.createElement('div');
        p.className = 'particle';
        p.style.left = Math.random() * 100 + '%';
        p.style.animationDelay = Math.random() * 8 + 's';
        p.style.animationDuration = (6 + Math.random() * 6) + 's';
        container.appendChild(p);
    }
}

/* ==================== COUNTERS ==================== */
function initCounters() {
    var stats = document.querySelectorAll('.stat-number');
    if (!stats.length) return;
    var observed = false;
    var obs = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting && !observed) {
                observed = true;
                stats.forEach(function(el) { animateCounter(el); });
            }
        });
    }, { threshold: 0.5 });
    stats.forEach(function(el) { obs.observe(el); });
}
function animateCounter(el) {
    var target = parseInt(el.getAttribute('data-target')) || 0;
    var duration = 1500, start = 0, startTime = null;
    function step(ts) {
        if (!startTime) startTime = ts;
        var progress = Math.min((ts - startTime) / duration, 1);
        el.textContent = Math.floor(progress * target);
        if (progress < 1) requestAnimationFrame(step);
        else el.textContent = target;
    }
    requestAnimationFrame(step);
}

/* ==================== CALC TABS ==================== */
function initCalcTabs() {
    document.querySelectorAll('.calc-tab').forEach(function(tab) {
        tab.addEventListener('click', function() {
            document.querySelectorAll('.calc-tab').forEach(function(t) { t.classList.remove('active'); });
            document.querySelectorAll('.calc-panel').forEach(function(p) { p.classList.remove('active'); });
            tab.classList.add('active');
            var panel = document.getElementById('calc-' + tab.getAttribute('data-calc'));
            if (panel) panel.classList.add('active');
        });
    });
}

/* ==================== RANGE SYNC ==================== */
function initRangeSync() {
    var pairs = [
        ['dof-focal','dof-focal-range'],['dof-aperture','dof-aperture-range'],['dof-distance','dof-distance-range'],
        ['fov-focal','fov-focal-range']
    ];
    pairs.forEach(function(p) {
        var num = document.getElementById(p[0]), rng = document.getElementById(p[1]);
        if (!num || !rng) return;
        num.addEventListener('input', function() { rng.value = num.value; });
        rng.addEventListener('input', function() { num.value = rng.value; });
    });
}

/* ==================== DOF CALCULATOR ==================== */
window.calculateDOF = function() {
    var f = parseFloat(document.getElementById('dof-focal').value);
    var N = parseFloat(document.getElementById('dof-aperture').value);
    var s = parseFloat(document.getElementById('dof-distance').value) * 1000;
    var cocDiag = parseFloat(document.getElementById('dof-sensor').value);
    var CoC = cocDiag / 1.5;
    var H = (f * f) / (N * CoC) + f;
    var nearDist = (s * (H - f)) / (H + s - 2 * f);
    var farDist = (H > s) ? (s * (H - f)) / (H - s) : Infinity;
    var dofTotal = farDist - nearDist;
    var fmt = function(v) { return isFinite(v) ? (v/1000).toFixed(2) + ' m' : '\u221e'; };
    document.getElementById('dof-total').textContent = fmt(dofTotal);
    document.getElementById('dof-near').textContent = fmt(nearDist);
    document.getElementById('dof-far').textContent = fmt(farDist);
    document.getElementById('dof-coc').textContent = CoC.toFixed(4) + ' mm';
    // Visualization
    var maxRange = Math.min(farDist * 1.5, s * 3);
    if (!isFinite(maxRange)) maxRange = s * 3;
    var bar = document.getElementById('dof-sharp-zone');
    var marker = document.getElementById('dof-subject-marker');
    var leftPct = Math.max(0, (nearDist / maxRange) * 100);
    var rightPct = isFinite(farDist) ? Math.min(100, (farDist / maxRange) * 100) : 100;
    bar.style.left = leftPct + '%'; bar.style.width = (rightPct - leftPct) + '%';
    marker.style.left = ((s / maxRange) * 100) + '%';
    document.getElementById('dof-label-near').textContent = fmt(nearDist);
    document.getElementById('dof-label-subject').textContent = (s/1000).toFixed(1) + 'm';
    document.getElementById('dof-label-far').textContent = fmt(farDist);
};

/* ==================== FOV CALCULATOR ==================== */
window.calculateFOV = function() {
    var f = parseFloat(document.getElementById('fov-focal').value);
    var dims = document.getElementById('fov-sensor').value.split('x');
    var w = parseFloat(dims[0]), h = parseFloat(dims[1]);
    var d = Math.sqrt(w*w + h*h);
    var hAngle = 2 * Math.atan(w / (2 * f)) * 180 / Math.PI;
    var vAngle = 2 * Math.atan(h / (2 * f)) * 180 / Math.PI;
    var dAngle = 2 * Math.atan(d / (2 * f)) * 180 / Math.PI;
    document.getElementById('fov-horizontal').textContent = hAngle.toFixed(1) + '\u00b0';
    document.getElementById('fov-vertical').textContent = vAngle.toFixed(1) + '\u00b0';
    document.getElementById('fov-diagonal').textContent = dAngle.toFixed(1) + '\u00b0';
    var type;
    if (f < 24) type = t('lenstype.superwide');
    else if (f < 35) type = t('lenstype.wide');
    else if (f < 70) type = t('lenstype.normal');
    else if (f < 200) type = t('lenstype.tele');
    else type = t('lenstype.supertele');
    document.getElementById('fov-type').textContent = type;
    // Draw FOV cone
    var canvas = document.getElementById('fov-canvas');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, 400, 300);
    var cx = 200, cy = 280, angle = hAngle * Math.PI / 180;
    ctx.fillStyle = 'rgba(108,92,231,0.15)';
    ctx.beginPath(); ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, 250, -Math.PI/2 - angle/2, -Math.PI/2 + angle/2);
    ctx.closePath(); ctx.fill();
    ctx.strokeStyle = '#a29bfe'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(cx, cy);
    ctx.lineTo(cx + 250 * Math.sin(-angle/2), cy - 250 * Math.cos(angle/2));
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx + 250 * Math.sin(angle/2), cy - 250 * Math.cos(angle/2));
    ctx.stroke();
    ctx.fillStyle = '#a29bfe'; ctx.font = '14px Inter';
    ctx.textAlign = 'center';
    ctx.fillText(hAngle.toFixed(1) + '\u00b0', cx, cy - 260 + 20);
};

/* ==================== CROP CALCULATOR ==================== */
window.calculateCrop = function() {
    var f = parseFloat(document.getElementById('crop-focal').value);
    var a = parseFloat(document.getElementById('crop-aperture').value);
    var crop = parseFloat(document.getElementById('crop-sensor').value);
    var ef = f * crop, ea = a * crop;
    document.getElementById('crop-equiv-focal').textContent = ef.toFixed(0) + ' mm';
    document.getElementById('crop-equiv-aperture').textContent = 'f/' + ea.toFixed(1);
    document.getElementById('crop-factor-result').textContent = crop.toFixed(1) + 'x';
    var overlay = document.getElementById('crop-overlay');
    if (overlay) {
        var pct = 100 / crop;
        overlay.style.width = pct + '%'; overlay.style.height = pct + '%';
        overlay.style.top = ((100 - pct) / 2) + '%'; overlay.style.left = ((100 - pct) / 2) + '%';
    }
};

/* ==================== HYPERFOCAL CALCULATOR ==================== */
window.calculateHyperfocal = function() {
    var f = parseFloat(document.getElementById('hyper-focal').value);
    var N = parseFloat(document.getElementById('hyper-aperture').value);
    var CoC = parseFloat(document.getElementById('hyper-sensor').value);
    var H = (f * f) / (N * CoC) + f;
    document.getElementById('hyper-distance').textContent = (H / 1000).toFixed(2) + ' m';
    document.getElementById('hyper-near').textContent = (H / 2000).toFixed(2) + ' m';
};

/* ==================== FLASH CALCULATOR ==================== */
window.calculateFlash = function() {
    var gn = parseFloat(document.getElementById('flash-gn').value);
    var a = parseFloat(document.getElementById('flash-aperture').value);
    var iso = parseFloat(document.getElementById('flash-iso').value);
    var range = (gn / a) * Math.sqrt(iso / 100);
    var range100 = gn / a;
    document.getElementById('flash-range').textContent = range.toFixed(1) + ' m';
    document.getElementById('flash-range-100').textContent = range100.toFixed(1) + ' m';
};

/* ==================== MAGNIFICATION CALCULATOR ==================== */
window.calculateMagnification = function() {
    var f = parseFloat(document.getElementById('mag-focal').value);
    var minD = parseFloat(document.getElementById('mag-min-focus').value) * 10;
    var sw = parseFloat(document.getElementById('mag-sensor-w').value);
    var mag = f / (minD - f);
    var fieldW = sw / mag;
    document.getElementById('mag-ratio').textContent = mag >= 1 ? '1:1' : '1:' + (1/mag).toFixed(1);
    document.getElementById('mag-field').textContent = fieldW.toFixed(1) + ' mm';
    var label;
    if (mag >= 1) label = t('macro.true');
    else if (mag >= 0.5) label = t('macro.half');
    else if (mag >= 0.25) label = t('macro.close');
    else label = t('macro.no');
    document.getElementById('mag-macro').textContent = label;
};

/* ==================== COMPOSITION RULES ==================== */
function renderRules() {
    var grid = document.getElementById('rulesGrid');
    if (!grid) return;
    grid.innerHTML = '';
    RULES.forEach(function(rule, i) {
        var num = String(i + 1).padStart(2, '0');
        var card = document.createElement('div');
        card.className = 'rule-card';
        card.setAttribute('data-rule', rule);
        card.innerHTML =
            '<div class="rule-canvas-container">' +
                '<canvas class="rule-canvas" id="canvas-' + rule + '" width="400" height="300"></canvas>' +
                '<div class="rule-overlay"><span class="rule-number">' + num + '</span></div>' +
            '</div>' +
            '<div class="rule-content">' +
                '<h3>' + t('rule.' + rule + '.name') + '</h3>' +
                '<p>' + t('rule.' + rule + '.desc') + '</p>' +
                '<ul class="rule-tips">' +
                    '<li>' + t('rule.' + rule + '.tip1') + '</li>' +
                    '<li>' + t('rule.' + rule + '.tip2') + '</li>' +
                    '<li>' + t('rule.' + rule + '.tip3') + '</li>' +
                '</ul>' +
                '<button class="btn-demo" onclick="drawRuleDemo(\'' + rule + '\')">' + t('comp.demo') + '</button>' +
            '</div>';
        grid.appendChild(card);
    });
}

function drawAllRuleDemos() { RULES.forEach(function(r) { drawRuleDemo(r); }); }

window.drawRuleDemo = function(rule) {
    var canvas = document.getElementById('canvas-' + rule);
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    var W = canvas.width, H = canvas.height;
    // Background gradient
    var bg = ctx.createLinearGradient(0, 0, W, H);
    bg.addColorStop(0, '#1a1a2e'); bg.addColorStop(1, '#16213e');
    ctx.fillStyle = bg; ctx.fillRect(0, 0, W, H);
    ctx.lineWidth = 1.5;

    switch(rule) {
        case 'thirds':
            ctx.strokeStyle = 'rgba(162,155,254,0.6)';
            for (var i = 1; i <= 2; i++) {
                ctx.beginPath(); ctx.moveTo(W*i/3, 0); ctx.lineTo(W*i/3, H); ctx.stroke();
                ctx.beginPath(); ctx.moveTo(0, H*i/3); ctx.lineTo(W, H*i/3); ctx.stroke();
            }
            ctx.fillStyle = '#ff6b6b';
            for (var x = 1; x <= 2; x++) for (var y = 1; y <= 2; y++) {
                ctx.beginPath(); ctx.arc(W*x/3, H*y/3, 6, 0, Math.PI*2); ctx.fill();
            }
            break;
        case 'golden':
            var phi = 0.618;
            ctx.strokeStyle = 'rgba(253,203,110,0.7)';
            [phi, 1-phi].forEach(function(r) {
                ctx.beginPath(); ctx.moveTo(W*r, 0); ctx.lineTo(W*r, H); ctx.stroke();
                ctx.beginPath(); ctx.moveTo(0, H*r); ctx.lineTo(W, H*r); ctx.stroke();
            });
            ctx.fillStyle = '#fdcb6e';
            ctx.beginPath(); ctx.arc(W*phi, H*phi, 8, 0, Math.PI*2); ctx.fill();
            // Spiral hint
            ctx.strokeStyle = 'rgba(253,203,110,0.4)'; ctx.lineWidth = 2;
            ctx.beginPath();
            var cx=W*phi, cy=H*phi, rad=80;
            for (var a=0; a<Math.PI*3; a+=0.05) { rad -= 0.4;
                ctx.lineTo(cx+Math.cos(a)*rad, cy+Math.sin(a)*rad);
            } ctx.stroke();
            break;
        case 'leading':
            ctx.strokeStyle = 'rgba(0,206,201,0.6)'; ctx.lineWidth = 2;
            var vx = W*0.5, vy = H*0.25;
            [[0,H],[W,H],[W*0.15,H],[W*0.85,H],[W*0.3,H],[W*0.7,H]].forEach(function(pt) {
                ctx.beginPath(); ctx.moveTo(pt[0],pt[1]); ctx.lineTo(vx,vy); ctx.stroke();
            });
            ctx.fillStyle = '#00cec9'; ctx.beginPath(); ctx.arc(vx,vy,8,0,Math.PI*2); ctx.fill();
            break;
        case 'symmetry':
            ctx.strokeStyle = 'rgba(162,155,254,0.5)'; ctx.setLineDash([5,5]);
            ctx.beginPath(); ctx.moveTo(W/2,0); ctx.lineTo(W/2,H); ctx.stroke(); ctx.setLineDash([]);
            ctx.fillStyle = 'rgba(108,92,231,0.3)';
            [[W*0.25,H*0.35,40],[W*0.75,H*0.35,40],[W*0.3,H*0.65,30],[W*0.7,H*0.65,30]].forEach(function(c) {
                ctx.beginPath(); ctx.arc(c[0],c[1],c[2],0,Math.PI*2); ctx.fill();
            });
            ctx.strokeStyle = '#a29bfe'; ctx.lineWidth = 1;
            [[W*0.25,H*0.35,40],[W*0.75,H*0.35,40],[W*0.3,H*0.65,30],[W*0.7,H*0.65,30]].forEach(function(c) {
                ctx.beginPath(); ctx.arc(c[0],c[1],c[2],0,Math.PI*2); ctx.stroke();
            });
            break;
        case 'framing':
            ctx.fillStyle = 'rgba(40,40,60,0.8)';
            ctx.fillRect(0,0,W,H*0.15); ctx.fillRect(0,H*0.85,W,H*0.15);
            ctx.fillRect(0,0,W*0.12,H); ctx.fillRect(W*0.88,0,W*0.12,H);
            // Arch
            ctx.beginPath(); ctx.ellipse(W/2, H*0.15, W*0.38, H*0.15, 0, Math.PI, 0);
            ctx.lineTo(W*0.12,H*0.85); ctx.lineTo(W*0.88,H*0.85); ctx.closePath();
            ctx.fillStyle = 'rgba(40,40,60,0.6)'; ctx.fill();
            ctx.strokeStyle = 'rgba(162,155,254,0.5)'; ctx.stroke();
            ctx.fillStyle = '#a29bfe'; ctx.beginPath(); ctx.arc(W/2,H*0.5,12,0,Math.PI*2); ctx.fill();
            break;
        case 'negative':
            ctx.fillStyle = '#6c5ce7'; ctx.beginPath();
            ctx.arc(W*0.75, H*0.7, 25, 0, Math.PI*2); ctx.fill();
            ctx.strokeStyle = 'rgba(162,155,254,0.3)'; ctx.lineWidth = 1;
            ctx.beginPath(); ctx.arc(W*0.75, H*0.7, 50, 0, Math.PI*2); ctx.stroke();
            ctx.font = '12px Inter'; ctx.fillStyle = 'rgba(162,155,254,0.3)'; ctx.textAlign = 'center';
            ctx.fillText('negative space', W*0.35, H*0.35);
            break;
        case 'diagonal':
            ctx.strokeStyle = 'rgba(255,107,107,0.6)'; ctx.lineWidth = 2;
            ctx.beginPath(); ctx.moveTo(0,0); ctx.lineTo(W,H); ctx.stroke();
            ctx.beginPath(); ctx.moveTo(W,0); ctx.lineTo(0,H); ctx.stroke();
            ctx.fillStyle = 'rgba(255,107,107,0.3)';
            ctx.beginPath(); ctx.moveTo(W/2,0); ctx.lineTo(W,H/2); ctx.lineTo(W/2,H); ctx.lineTo(0,H/2); ctx.closePath(); ctx.fill();
            break;
        case 'color':
            var colors = ['#e74c3c','#e67e22','#f1c40f','#2ecc71','#3498db','#9b59b6'];
            var sliceAngle = Math.PI*2 / colors.length;
            var ccx = W/2, ccy = H/2, rad = Math.min(W,H)*0.38;
            colors.forEach(function(col, i) {
                ctx.beginPath(); ctx.moveTo(ccx,ccy);
                ctx.arc(ccx,ccy,rad, i*sliceAngle-Math.PI/2, (i+1)*sliceAngle-Math.PI/2);
                ctx.closePath(); ctx.fillStyle = col; ctx.globalAlpha = 0.7; ctx.fill();
            });
            ctx.globalAlpha = 1;
            ctx.strokeStyle = '#1a1a2e'; ctx.lineWidth = 3;
            colors.forEach(function(col, i) {
                ctx.beginPath(); ctx.moveTo(ccx,ccy); ctx.arc(ccx,ccy,rad,i*sliceAngle-Math.PI/2,(i+1)*sliceAngle-Math.PI/2);
                ctx.closePath(); ctx.stroke();
            });
            // Complementary line
            ctx.strokeStyle = '#fff'; ctx.lineWidth = 2; ctx.setLineDash([4,4]);
            ctx.beginPath(); ctx.moveTo(ccx,ccy-rad); ctx.lineTo(ccx,ccy+rad); ctx.stroke();
            ctx.setLineDash([]);
            break;
    }
};

/* ==================== MOTIF RECOGNITION ==================== */
function renderMotifs() {
    // Filter buttons
    var filterContainer = document.getElementById('motifFilter');
    if (filterContainer) {
        filterContainer.innerHTML = '';
        var allBtn = document.createElement('button');
        allBtn.className = 'motif-filter-btn active'; allBtn.setAttribute('data-filter', 'all');
        allBtn.textContent = t('motif.all');
        allBtn.addEventListener('click', function() { filterMotifs('all'); });
        filterContainer.appendChild(allBtn);
        MOTIFS.forEach(function(m) {
            var btn = document.createElement('button');
            btn.className = 'motif-filter-btn'; btn.setAttribute('data-filter', m.id);
            btn.textContent = t('motif.' + m.id);
            btn.addEventListener('click', function() { filterMotifs(m.id); });
            filterContainer.appendChild(btn);
        });
    }
    // Cards
    var grid = document.getElementById('motifGrid');
    if (!grid) return;
    grid.innerHTML = '';
    MOTIFS.forEach(function(m) {
        var card = document.createElement('div');
        card.className = 'motif-card'; card.setAttribute('data-category', m.id);
        card.innerHTML =
            '<div class="motif-icon">' + m.icon + '</div>' +
            '<div class="motif-info">' +
                '<h3>' + t('motif.' + m.id + '.title') + '</h3>' +
                '<p>' + t('motif.' + m.id + '.desc') + '</p>' +
                '<div class="motif-settings">' +
                    '<div class="motif-setting"><span class="motif-setting-label">' + t('motif.setting.lens') + '</span><span class="motif-setting-value">' + m.lens + '</span></div>' +
                    '<div class="motif-setting"><span class="motif-setting-label">' + t('motif.setting.aperture') + '</span><span class="motif-setting-value">' + m.aperture + '</span></div>' +
                    '<div class="motif-setting"><span class="motif-setting-label">' + t('motif.setting.iso') + '</span><span class="motif-setting-value">' + m.iso + '</span></div>' +
                    '<div class="motif-setting"><span class="motif-setting-label">' + t('motif.setting.shutter') + '</span><span class="motif-setting-value">' + m.shutter + '</span></div>' +
                '</div>' +
                '<div class="motif-tags"><span class="motif-tag">' + t('motif.' + m.id) + '</span></div>' +
            '</div>';
        grid.appendChild(card);
    });
}

function filterMotifs(cat) {
    document.querySelectorAll('.motif-filter-btn').forEach(function(b) {
        b.classList.toggle('active', b.getAttribute('data-filter') === cat);
    });
    document.querySelectorAll('.motif-card').forEach(function(card) {
        if (cat === 'all') card.classList.remove('hidden');
        else card.classList.toggle('hidden', card.getAttribute('data-category') !== cat);
    });
}

/* ==================== EXPOSURE TRIANGLE ==================== */
var apertureVals = [1.4,2,2.8,4,5.6,8,11,16,22,32];
var shutterLabels = ['30s','15s','1s','1/4','1/30','1/125','1/250','1/500','1/1000','1/2000','1/4000','1/8000'];
var isoVals = [100,200,400,800,1600,3200,6400,12800,25600];

function drawExposureTriangle() {
    var canvas = document.getElementById('exposure-canvas');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    var W = canvas.width, H = canvas.height;
    ctx.clearRect(0, 0, W, H);
    var cx = W/2, topY = 40, botY = H - 60;
    var leftX = 60, rightX = W - 60;
    // Triangle
    ctx.beginPath(); ctx.moveTo(cx, topY); ctx.lineTo(leftX, botY); ctx.lineTo(rightX, botY); ctx.closePath();
    ctx.fillStyle = 'rgba(108,92,231,0.08)'; ctx.fill();
    ctx.strokeStyle = 'rgba(108,92,231,0.4)'; ctx.lineWidth = 2; ctx.stroke();
    // Labels
    ctx.font = '600 14px Inter'; ctx.fillStyle = '#a29bfe'; ctx.textAlign = 'center';
    ctx.fillText(t('exp.aperture').split(' ')[0] || 'Aperture', cx, topY - 12);
    ctx.fillText('ISO', rightX + 10, botY + 30);
    ctx.fillText(t('exp.shutter').split(' ')[0] || 'Shutter', leftX - 10, botY + 30);
    // Icons in triangle
    ctx.font = '28px serif'; ctx.textAlign = 'center';
    ctx.fillText('\ud83d\udd73\ufe0f', cx, topY + 55);
    ctx.fillText('\u23f1\ufe0f', leftX + 50, botY - 25);
    ctx.fillText('\ud83c\udf1f', rightX - 50, botY - 25);
}

function initExposureControls() {
    ['exp-aperture','exp-shutter','exp-iso'].forEach(function(id) {
        var el = document.getElementById(id);
        if (el) el.addEventListener('input', updateExposure);
    });
    updateExposure();
}

function updateExposure() {
    var ai = parseInt(document.getElementById('exp-aperture').value);
    var si = parseInt(document.getElementById('exp-shutter').value);
    var ii = parseInt(document.getElementById('exp-iso').value);
    document.getElementById('exp-aperture-val').textContent = 'f/' + apertureVals[ai];
    document.getElementById('exp-shutter-val').textContent = shutterLabels[si];
    document.getElementById('exp-iso-val').textContent = 'ISO ' + isoVals[ii];
    // EV offset (simplified)
    var ev = (ai - 4) + (si - 5) - (ii - 2);
    var pct = 50 + ev * 8.33;
    pct = Math.max(0, Math.min(100, pct));
    document.getElementById('ev-indicator').style.left = pct + '%';
    var evText = document.getElementById('ev-text');
    if (Math.abs(ev) <= 0.5) { evText.textContent = t('exp.correct'); evText.style.color = '#00cec9'; }
    else if (ev < -2) { evText.textContent = t('exp.under'); evText.style.color = '#0984e3'; }
    else if (ev < 0) { evText.textContent = t('exp.slightunder'); evText.style.color = '#74b9ff'; }
    else if (ev > 2) { evText.textContent = t('exp.over'); evText.style.color = '#d63031'; }
    else { evText.textContent = t('exp.slightover'); evText.style.color = '#e17055'; }
}

/* ==================== QUIZ CATEGORIES ==================== */
function renderQuizCategories() {
    var container = document.getElementById('quizCategories');
    if (!container) return;
    container.innerHTML = '';
    var cats = [
        {id:'all',key:'quiz.all'},{id:'basics',key:'quiz.basics'},{id:'composition',key:'quiz.composition'},
        {id:'lens',key:'quiz.lenses'},{id:'exposure',key:'quiz.exposure'},{id:'genres',key:'quiz.genres'}
    ];
    cats.forEach(function(c, i) {
        var btn = document.createElement('button');
        btn.className = 'quiz-cat-btn' + (i === 0 ? ' active' : '');
        btn.setAttribute('data-category', c.id);
        btn.textContent = t(c.key);
        btn.addEventListener('click', function() { startQuiz(c.id); });
        container.appendChild(btn);
    });
}

/* ==================== QUIZ SYSTEM ==================== */
window.startQuiz = function(category) {
    var lang = window.currentLang || 'de';
    var questions = quizDB[lang] || quizDB.de;
    if (category && category !== 'all') questions = questions.filter(function(q) { return q.cat === category; });
    // Shuffle & pick 10
    questions = questions.slice().sort(function() { return Math.random() - 0.5; }).slice(0, 10);
    quizState.questions = questions;
    quizState.current = 0; quizState.score = 0; quizState.answers = [];
    quizState.seconds = 0;
    // Update UI
    document.querySelectorAll('.quiz-cat-btn').forEach(function(b) {
        b.classList.toggle('active', b.getAttribute('data-category') === category);
    });
    document.getElementById('quizStart').style.display = 'none';
    document.getElementById('quizResults').style.display = 'none';
    document.getElementById('quizActive').style.display = 'block';
    // Timer
    if (quizState.timer) clearInterval(quizState.timer);
    quizState.timer = setInterval(function() {
        quizState.seconds++;
        var m = Math.floor(quizState.seconds / 60), s = quizState.seconds % 60;
        document.getElementById('quizTimer').textContent = String(m).padStart(2,'0') + ':' + String(s).padStart(2,'0');
    }, 1000);
    showQuestion(0);
};

function showQuestion(idx) {
    var q = quizState.questions[idx];
    if (!q) return;
    document.getElementById('quizProgress').style.width = ((idx + 1) / quizState.questions.length * 100) + '%';
    document.getElementById('quizProgressText').textContent = (idx + 1) + ' / ' + quizState.questions.length;
    document.getElementById('quizScore').textContent = t('quiz.score') + ': ' + quizState.score;
    var catMap = {basics:'quiz.basics',composition:'quiz.composition',lens:'quiz.lenses',exposure:'quiz.exposure',genres:'quiz.genres'};
    document.getElementById('quizCategoryBadge').textContent = t(catMap[q.cat] || q.cat);
    document.getElementById('quizQuestion').textContent = q.q;
    var optContainer = document.getElementById('quizOptions');
    optContainer.innerHTML = '';
    var letters = ['A','B','C','D'];
    q.o.forEach(function(opt, i) {
        var div = document.createElement('div');
        div.className = 'quiz-option';
        div.innerHTML = '<span class="option-letter">' + letters[i] + '</span><span>' + opt + '</span>';
        div.addEventListener('click', function() { selectAnswer(i); });
        optContainer.appendChild(div);
    });
    document.getElementById('quizExplanation').style.display = 'none';
    document.getElementById('quizNextBtn').style.display = 'none';
}

function selectAnswer(idx) {
    var q = quizState.questions[quizState.current];
    var options = document.querySelectorAll('#quizOptions .quiz-option');
    options.forEach(function(o, i) {
        o.classList.add('disabled');
        o.style.pointerEvents = 'none';
        if (i === q.c) o.classList.add('correct');
        if (i === idx && idx !== q.c) o.classList.add('wrong');
    });
    if (idx === q.c) quizState.score++;
    quizState.answers.push({ question: q, selected: idx, correct: q.c });
    document.getElementById('quizScore').textContent = t('quiz.score') + ': ' + quizState.score;
    document.getElementById('quizExplanationText').textContent = q.e;
    document.getElementById('quizExplanation').style.display = 'block';
    document.getElementById('quizNextBtn').style.display = 'block';
}

window.nextQuestion = function() {
    quizState.current++;
    if (quizState.current < quizState.questions.length) {
        showQuestion(quizState.current);
    } else {
        showResults();
    }
};

function showResults() {
    if (quizState.timer) clearInterval(quizState.timer);
    document.getElementById('quizActive').style.display = 'none';
    document.getElementById('quizResults').style.display = 'block';
    var total = quizState.questions.length;
    var pct = Math.round((quizState.score / total) * 100);
    document.getElementById('resultsPercent').textContent = pct + '%';
    // Animate circle
    var circle = document.getElementById('resultsCircle');
    var circumference = 2 * Math.PI * 54;
    circle.style.strokeDasharray = circumference;
    circle.style.strokeDashoffset = circumference;
    setTimeout(function() {
        circle.style.strokeDashoffset = circumference - (circumference * pct / 100);
    }, 100);
    // Color based on score
    if (pct >= 80) circle.style.stroke = '#00cec9';
    else if (pct >= 50) circle.style.stroke = '#fdcb6e';
    else circle.style.stroke = '#ff6b6b';
    var resultText = t('quiz.resulttext').replace('{0}', quizState.score).replace('{1}', total);
    document.getElementById('resultsText').textContent = resultText;
    var feedbackKey;
    if (pct >= 90) feedbackKey = 'quiz.excellent';
    else if (pct >= 70) feedbackKey = 'quiz.good';
    else if (pct >= 50) feedbackKey = 'quiz.ok';
    else feedbackKey = 'quiz.needwork';
    document.getElementById('resultsTitle').textContent = t(feedbackKey);
}

window.reviewAnswers = function() {
    var bd = document.getElementById('resultsBreakdown');
    if (!bd) return;
    bd.innerHTML = '';
    quizState.answers.forEach(function(a, i) {
        var div = document.createElement('div');
        div.style.cssText = 'text-align:left;padding:0.8rem;margin:0.5rem 0;background:var(--bg-input);border-radius:8px;border-left:3px solid ' + (a.selected === a.correct ? '#00cec9' : '#ff6b6b');
        div.innerHTML = '<strong>' + (i+1) + '. ' + a.question.q + '</strong><br>' +
            '<span style="color:' + (a.selected === a.correct ? '#00cec9' : '#ff6b6b') + '">' +
            (a.selected === a.correct ? '\u2713' : '\u2717') + ' ' + a.question.o[a.selected] + '</span>' +
            (a.selected !== a.correct ? '<br><span style="color:#00cec9">\u2713 ' + a.question.o[a.correct] + '</span>' : '');
        bd.appendChild(div);
    });
};

})();
