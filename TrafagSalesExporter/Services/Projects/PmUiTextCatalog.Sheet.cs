namespace TrafagSalesExporter.Services.Projects;

// Texte: Smartsheet-Ansicht (Raster und Gantt). Format: new("Deutsch", es, it, hi, sq, tr, tlh, gsw).
// Woerter, die schon im allgemeinen Katalog stehen (Heute, Status, Start, Ende, Dauer ...), fehlen hier absichtlich,
// damit ihre bestehenden Uebersetzungen nicht ueberschrieben werden.
internal static partial class PmUiTextCatalog
{
    private static Entry[] SheetEntries =>
    [
        // Werkzeugleiste
        new("Zeile unter der markierten einfügen", "Insertar fila debajo de la seleccionada", "Inserisci riga sotto quella selezionata", "चयनित पंक्ति के नीचे पंक्ति जोड़ें", "Shto rresht nën atë të zgjedhurin", "Seçili satırın altına satır ekle", "wIvlu'pu'bogh tlhegh bIng tlhegh chu' yIlan", "Zile unger dr markierte iifüege"),
        new("Zeile", "Fila", "Riga", "पंक्ति", "Rresht", "Satır", "tlhegh", "Zile"),
        new("Einrücken", "Aumentar sangría", "Aumenta rientro", "इंडेंट करें", "Fut brenda", "Girinti ekle", "tlhegh yIjuchmoH", "Iirücke"),
        new("Ausrücken", "Reducir sangría", "Riduci rientro", "इंडेंट हटाएँ", "Nxirr jashtë", "Girintiyi azalt", "tlhegh yIpupmoH", "Usrücke"),
        new("Zeile nach oben", "Subir fila", "Sposta riga in alto", "पंक्ति ऊपर ले जाएँ", "Lëviz rreshtin lart", "Satırı yukarı taşı", "tlhegh Dung yIvIH", "Zile ufe"),
        new("Zeile nach unten", "Bajar fila", "Sposta riga in basso", "पंक्ति नीचे ले जाएँ", "Lëviz rreshtin poshtë", "Satırı aşağı taşı", "tlhegh bIng yIvIH", "Zile abe"),
        new("Alle aufklappen", "Expandir todo", "Espandi tutto", "सभी खोलें", "Hap të gjitha", "Tümünü genişlet", "Hoch yIpoSmoH", "Aui ufklappe"),
        new("Alle zuklappen", "Contraer todo", "Comprimi tutto", "सभी बंद करें", "Mbyll të gjitha", "Tümünü daralt", "Hoch yISoQmoH", "Aui zueklappe"),
        new("Zuklappen", "Contraer", "Comprimi", "बंद करें", "Mbyll", "Daralt", "yISoQmoH", "Zueklappe"),
        new("Zeitskala", "Escala de tiempo", "Scala temporale", "समय पैमाना", "Shkalla kohore", "Zaman ölçeği", "poH juv", "Zytskala"),
        new("Zum heutigen Tag springen", "Ir a hoy", "Vai a oggi", "आज पर जाएँ", "Shko te dita e sotme", "Bugüne git", "DaHjaj yIghoS", "Zum hüttige Tag gumpe"),
        new("Kritischen Pfad rot hervorheben", "Resaltar en rojo la ruta crítica", "Evidenzia in rosso il percorso critico", "क्रिटिकल पाथ को लाल रंग में दिखाएँ", "Thekso me të kuqe rrugën kritike", "Kritik yolu kırmızıyla vurgula", "He Qatlh Doq yIwovmoH", "Kritische Pfad rot hervorhebe"),
        new("Kritischer Pfad", "Ruta crítica", "Percorso critico", "क्रिटिकल पाथ", "Rruga kritike", "Kritik yol", "He Qatlh", "Kritische Pfad"),
        new("Basisplan unter den Balken anzeigen", "Mostrar la línea base bajo las barras", "Mostra la baseline sotto le barre", "बार के नीचे बेसलाइन दिखाएँ", "Shfaq planin bazë nën shiritat", "Çubukların altında temel planı göster", "ghItlhmey bIngDaq nab Hal yIcha'", "Basisplan unger de Balke zeige"),
        new("Basisplan", "Línea base", "Baseline", "बेसलाइन", "Plani bazë", "Temel plan", "nab Hal", "Basisplan"),
        new("Basisplan wirklich löschen?", "¿Eliminar realmente la línea base?", "Eliminare davvero la baseline?", "क्या सच में बेसलाइन हटाएँ?", "Të fshihet vërtet plani bazë?", "Temel plan gerçekten silinsin mi?", "nab Hal DaQaw'qu' DaneH'a'?", "Basisplan würklech lösche?"),
        new("Aktuelle Termine als Basisplan einfrieren", "Congelar las fechas actuales como línea base", "Congela le date attuali come baseline", "वर्तमान तिथियों को बेसलाइन के रूप में स्थिर करें", "Ngrij datat aktuale si plan bazë", "Mevcut tarihleri temel plan olarak dondur", "DaH poHmey nab Hal yIchenmoH", "Aktuelli Termine als Basisplan iifrüüre"),
        new("Basisplan setzen", "Fijar línea base", "Imposta baseline", "बेसलाइन सेट करें", "Vendos planin bazë", "Temel planı belirle", "nab Hal yIlan", "Basisplan setze"),
        new("Basisplan löschen", "Eliminar línea base", "Elimina baseline", "बेसलाइन हटाएँ", "Fshi planin bazë", "Temel planı sil", "nab Hal yIQaw'", "Basisplan lösche"),
        new("Zeilen suchen", "Buscar filas", "Cerca righe", "पंक्तियाँ खोजें", "Kërko rreshta", "Satır ara", "tlheghmey yInej", "Zile sueche"),
        new("Nach Person filtern", "Filtrar por persona", "Filtra per persona", "व्यक्ति के अनुसार फ़िल्टर करें", "Filtro sipas personit", "Kişiye göre filtrele", "ghot yIwIv", "Nach Person filtere"),
        new("Alle Personen", "Todas las personas", "Tutte le persone", "सभी व्यक्ति", "Të gjithë personat", "Tüm kişiler", "ghot Hoch", "Aui Persone"),
        new("Spalten ein- und ausblenden", "Mostrar y ocultar columnas", "Mostra e nascondi colonne", "कॉलम दिखाएँ और छिपाएँ", "Shfaq dhe fshih kolonat", "Sütunları göster ve gizle", "tut'mey yIcha' 'ej yISo'", "Spalte ii- und usblände"),
        new("Neue Zeile", "Nueva fila", "Nuova riga", "नई पंक्ति", "Rresht i ri", "Yeni satır", "tlhegh chu'", "Neui Zile"),

        // Raster
        new("Das Projekt ist nicht verfügbar.", "El proyecto no está disponible.", "Il progetto non è disponibile.", "परियोजना उपलब्ध नहीं है।", "Projekti nuk është i disponueshëm.", "Proje kullanılamıyor.", "jInmol tu'lu'be'.", "S Projekt isch nid verfüegbar."),
        new("Raster wird geladen …", "Cargando la cuadrícula …", "Caricamento griglia …", "ग्रिड लोड हो रहा है …", "Rrjeta po ngarkohet …", "Tablo yükleniyor …", "mut luHoSmoHlI' …", "Raster wird glade …"),
        new("Nummer für die Spalte Vorgänger", "Número para la columna Predecesoras", "Numero per la colonna Predecessori", "पूर्ववर्ती कॉलम के लिए संख्या", "Numri për kolonën Paraardhësit", "Öncüller sütunu için numara", "ret Qu'mey tut'vaD mI'", "Nummere für d Spalte Vorgänger"),
        new("Meilenstein", "Hito", "Milestone", "माइलस्टोन", "Pikë kyçe", "Kilometre taşı", "Qu' Hut'In", "Meilestei"),
        new("z. B. 3, 5+2", "p. ej. 3, 5+2", "ad es. 3, 5+2", "उदा. 3, 5+2", "p.sh. 3, 5+2", "örn. 3, 5+2", "mu'mey: 3, 5+2", "z. B. 3, 5+2"),
        new("{0} aktive Risiken", "{0} riesgos activos", "{0} rischi attivi", "{0} सक्रिय जोखिम", "{0} rreziqe aktive", "{0} etkin risk", "{0} Qobmey ghajlu'", "{0} aktivi Risike"),
        new("Auf dem kritischen Pfad: jede Verspätung verschiebt das Projektende", "En la ruta crítica: cualquier retraso mueve el final del proyecto", "Sul percorso critico: ogni ritardo sposta la fine del progetto", "क्रिटिकल पाथ पर: हर देरी परियोजना की समाप्ति को आगे बढ़ाती है", "Në rrugën kritike: çdo vonesë e shtyn fundin e projektit", "Kritik yolda: her gecikme proje bitişini kaydırır", "He Qatlh Daq: mIm Hoch jInmol van nup", "Uf em kritische Pfad: jedi Verspätig verschiebt ds Projektänd"),
        new("{0} Arbeitstage Puffer", "{0} días laborables de holgura", "{0} giorni lavorativi di margine", "{0} कार्यदिवस का बफ़र", "{0} ditë pune rezervë", "{0} iş günü bolluk", "{0} Qu' jaj ratlh", "{0} Schaffstage Puffer"),
        new("Noch keine Zeilen. Füge oben eine Zeile ein.", "Aún no hay filas. Inserta una fila arriba.", "Ancora nessuna riga. Inserisci una riga in alto.", "अभी कोई पंक्ति नहीं है। ऊपर एक पंक्ति जोड़ें।", "Ende nuk ka rreshta. Shto një rresht më lart.", "Henüz satır yok. Yukarıdan bir satır ekle.", "tlhegh pagh. Dung tlhegh yIlan.", "No kei Zile. Füeg obe e Zile ii."),
        new("Keine Zeile passt zum Filter.", "Ninguna fila coincide con el filtro.", "Nessuna riga corrisponde al filtro.", "कोई पंक्ति फ़िल्टर से मेल नहीं खाती।", "Asnjë rresht nuk përputhet me filtrin.", "Filtreye uyan satır yok.", "wIvlu'pu'bogh tlhegh tu'lu'be'.", "Kei Zile passt zum Filter."),
        new("Vorgänger", "Predecesoras", "Predecessori", "पूर्ववर्ती", "Paraardhësit", "Öncüller", "ret Qu'mey", "Vorgänger"),
        new("% erledigt", "% completado", "% completato", "% पूरा", "% e kryer", "% tamamlandı", "% rInpu'", "% erledigt"),
        new("Dauer in Arbeitstagen; 0 macht die Zeile zum Meilenstein", "Duración en días laborables; 0 convierte la fila en un hito", "Durata in giorni lavorativi; 0 trasforma la riga in una milestone", "कार्यदिवसों में अवधि; 0 पंक्ति को माइलस्टोन बना देता है", "Kohëzgjatja në ditë pune; 0 e bën rreshtin pikë kyçe", "İş günü cinsinden süre; 0 satırı kilometre taşı yapar", "Qu' jajmey poH; 0 tlhegh Qu' Hut'In moj", "Duur i Schaffstage; 0 macht d Zile zum Meilestei"),
        new("Nummern der Vorgänger mit Komma, Versatz in Arbeitstagen, z. B. 3, 5+2", "Números de las predecesoras separados por comas, desfase en días laborables, p. ej. 3, 5+2", "Numeri dei predecessori separati da virgole, ritardo in giorni lavorativi, ad es. 3, 5+2", "पूर्ववर्तियों की संख्याएँ अल्पविराम से, अंतराल कार्यदिवसों में, उदा. 3, 5+2", "Numrat e paraardhësve me presje, zhvendosja në ditë pune, p.sh. 3, 5+2", "Öncül numaraları virgülle, gecikme iş günü olarak, örn. 3, 5+2", "ret Qu'mey mI'mey, Qu' jajmey mIm, mu'mey: 3, 5+2", "Nummere vo de Vorgänger mit Komma, Versatz i Schaffstage, z. B. 3, 5+2"),
        new("Auf dem kritischen Pfad, sonst Puffer in Arbeitstagen", "En la ruta crítica; si no, holgura en días laborables", "Sul percorso critico, altrimenti margine in giorni lavorativi", "क्रिटिकल पाथ पर, अन्यथा कार्यदिवसों में बफ़र", "Në rrugën kritike, përndryshe rezervë në ditë pune", "Kritik yolda, değilse iş günü cinsinden bolluk", "He Qatlh Daq, pagh Qu' jajmey ratlh", "Uf em kritische Pfad, süsch Puffer i Schaffstage"),

        // Gantt
        new("Breite ziehen", "Arrastrar para cambiar el ancho", "Trascina per ridimensionare", "चौड़ाई बदलने के लिए खींचें", "Tërhiq për të ndryshuar gjerësinë", "Genişliği sürükle", "juch yIlum", "Breiti zieh"),
        new("Auf einen anderen Balken ziehen: legt eine Abhängigkeit an", "Arrastra sobre otra barra: crea una dependencia", "Trascina su un'altra barra: crea una dipendenza", "किसी दूसरे बार पर खींचें: निर्भरता बनाता है", "Tërhiqe mbi një shirit tjetër: krijon një varësi", "Başka bir çubuğa sürükle: bağımlılık oluşturur", "latlh ghItlh yIlum: rarwI' chenmoH", "Uf ne angere Balke zieh: leit e Abhängigkeit aa"),
        new("Sammelzeile", "Fila de resumen", "Riga di riepilogo", "सारांश पंक्ति", "Rresht përmbledhës", "Özet satırı", "naQ tlhegh", "Sammelzile"),
        new("{0} Zeilen, {1} auf dem kritischen Pfad", "{0} filas, {1} en la ruta crítica", "{0} righe, {1} sul percorso critico", "{0} पंक्तियाँ, {1} क्रिटिकल पाथ पर", "{0} rreshta, {1} në rrugën kritike", "{0} satır, {1} tanesi kritik yolda", "{0} tlheghmey, {1} He Qatlh Daq", "{0} Zile, {1} uf em kritische Pfad"),
        new("Balken ziehen verschiebt, die Ränder ändern Start und Ende, der Punkt rechts legt eine Abhängigkeit an. Doppelklick öffnet die Aufgabe.", "Arrastra una barra para moverla, sus bordes cambian el inicio y el fin, el punto de la derecha crea una dependencia. Doble clic abre la tarea.", "Trascina una barra per spostarla, i bordi cambiano inizio e fine, il punto a destra crea una dipendenza. Il doppio clic apre l'attività.", "बार को खिसकाने के लिए खींचें, उसके किनारे शुरुआत और अंत बदलते हैं, दाईं ओर का बिंदु निर्भरता बनाता है। डबल-क्लिक कार्य खोलता है।", "Tërhiq një shirit për ta lëvizur, skajet ndryshojnë fillimin dhe fundin, pika djathtas krijon një varësi. Klikimi i dyfishtë hap detyrën.", "Bir çubuğu taşımak için sürükle, kenarları başlangıcı ve bitişi değiştirir, sağdaki nokta bağımlılık oluşturur. Çift tıklama görevi açar.", "ghItlh yIlum vaj vIHlu'. HeHmey tagh nup je choHmoH. nIH Doq rarwI' chenmoH. cha'logh yI'uy vaj Qu' poSmoH.", "Balke zieh verschiebt ne, d Ränder ändere Start und Änd, dr Punkt rächts leit e Abhängigkeit aa. Doppelklick tuet d Ufgab uf."),
        new("{0} Arbeitstage, {1} % erledigt", "{0} días laborables, {1} % completado", "{0} giorni lavorativi, {1} % completato", "{0} कार्यदिवस, {1} % पूरा", "{0} ditë pune, {1} % e kryer", "{0} iş günü, {1} % tamamlandı", "{0} Qu' jaj, {1} vatlhvI' rInpu'", "{0} Schaffstage, {1} % erledigt"),
        new("Feiertag {0}", "Festivo {0}", "Festività {0}", "अवकाश {0}", "Festë {0}", "Resmî tatil {0}", "lop jaj {0}", "Fyrtig {0}"),
        new("KW {0}", "Sem. {0}", "Sett. {0}", "सप्ताह {0}", "Java {0}", "Hf. {0}", "Hogh {0}", "KW {0}"),
    ];
}
