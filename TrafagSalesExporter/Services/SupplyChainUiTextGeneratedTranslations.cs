namespace TrafagSalesExporter.Services;

// Reihenfolge entspricht SupplyChainUiTextCatalog.All. Eigener additiver Katalog, damit die
// vorhandenen allgemeinen, Einkaufs- und Logistik-Kataloge nicht ueberschrieben werden.
internal static class SupplyChainUiTextGeneratedTranslations
{
    internal static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = Map([
                "Material, texto o proveedor", "Planificador", "Grupo de productos", "Solo requiere acciÃ³n",
                "Prioridades en el filtro actual", "Estado y alcance de los datos", "Materiales en el alcance de origen",
                "Materiales despuÃ©s del filtro", "Fecha real de entrada de mercancÃ­as", "Planificador / grupo de productos",
                "Existencias / consumo", "Seguridad / punto de pedido", "Entradas fijas / planificadas", "Faltante / CHF",
                "Impacto", "Abierto / vencido", "PrÃ³xima fecha", "Proveedor principal", "ParticipaciÃ³n principal",
                "Materiales superiores", "Tarea de revisiÃ³n", "MRP / aprovisionamiento", "TamaÃ±o de lote", "Estado / LZ",
                "Fecha planificada", "Valor abierto CHF", "Entrada real de mercancÃ­as", "Falta la fuente",
                "No hay resultados en el filtro actual.",
                "La tabla detallada se limita a los 1.000 resultados de mayor prioridad; los KPI y las barras de prioridad utilizan todo el alcance filtrado.",
                "No se pudo cargar el anÃ¡lisis: ", "LogÃ­stica", "PlanificaciÃ³n de materiales y faltantes",
                "Prioriza las brechas de cobertura, las existencias crÃ­ticas y el impacto en los productos terminados.",
                "Necesidad de compra y cobertura", "Conecta el stock final de SAP con pedidos abiertos, fechas planificadas y proveedores.",
                "Dependencia de proveedor/material", "Muestra riesgos observados de fuente Ãºnica y concentraciÃ³n hasta el nÃºmero de material.",
                "RevisiÃ³n de parÃ¡metros de planificaciÃ³n", "Crea tareas de revisiÃ³n separadas para parÃ¡metros MARC/MARA faltantes o llamativos.",
                "Rendimiento de entrega y estado de datos", "Muestra el retraso fiable de fechas planificadas de EKET y lo separa de la puntualidad real aÃºn no medible."
            ]),
            ["it"] = Map([
                "Materiale, testo o fornitore", "Pianificatore", "Gruppo di prodotti", "Solo azioni richieste",
                "PrioritÃ  nel filtro corrente", "Stato e ambito dei dati", "Materiali nell'ambito sorgente",
                "Materiali dopo il filtro", "Data effettiva di entrata merci", "Pianificatore / gruppo di prodotti",
                "Scorte / consumo", "Sicurezza / punto di riordino", "Entrate fisse / pianificate", "Carenza / CHF",
                "Impatto", "Aperto / scaduto", "Prossima data", "Fornitore principale", "Quota principale",
                "Materiali padre", "AttivitÃ  di verifica", "MRP / approvvigionamento", "Dimensione lotto", "Stato / LZ",
                "Data pianificata", "Valore aperto CHF", "Entrata merci effettiva", "Fonte mancante",
                "Nessun risultato nel filtro corrente.",
                "La tabella dettagliata Ã¨ limitata ai 1.000 risultati con prioritÃ  piÃ¹ alta; KPI e barre di prioritÃ  usano l'intero ambito filtrato.",
                "Impossibile caricare l'analisi: ", "Logistica", "Pianificazione materiali e carenze",
                "DÃ  prioritÃ  a lacune di copertura, scorte critiche e impatto sui prodotti finiti.",
                "Fabbisogno d'acquisto e copertura", "Collega lo stock finale SAP con ordini aperti, date pianificate e fornitori.",
                "Dipendenza fornitore/materiale", "Mostra i rischi osservati di fonte unica e concentrazione fino al numero materiale.",
                "Verifica parametri di pianificazione", "Crea attivitÃ  di verifica separate per parametri MARC/MARA mancanti o anomali.",
                "Prestazioni di consegna e stato dati", "Mostra l'arretrato affidabile delle date pianificate EKET e lo separa dalla puntualitÃ  effettiva non ancora misurabile."
            ]),
            ["hi"] = Map([
                "à¤¸à¤¾à¤®à¤—à¥à¤°à¥€, à¤ªà¤¾à¤  à¤¯à¤¾ à¤†à¤ªà¥‚à¤°à¥à¤¤à¤¿à¤•à¤°à¥à¤¤à¤¾", "à¤¯à¥‹à¤œà¤¨à¤¾à¤•à¤¾à¤°", "à¤‰à¤¤à¥à¤ªà¤¾à¤¦ à¤¸à¤®à¥‚à¤¹", "à¤•à¥‡à¤µà¤² à¤•à¤¾à¤°à¥à¤°à¤µà¤¾à¤ˆ à¤†à¤µà¤¶à¥à¤¯à¤•",
                "à¤µà¤°à¥à¤¤à¤®à¤¾à¤¨ à¤«à¤¼à¤¿à¤²à¥à¤Ÿà¤° à¤®à¥‡à¤‚ à¤ªà¥à¤°à¤¾à¤¥à¤®à¤¿à¤•à¤¤à¤¾à¤à¤", "à¤¡à¥‡à¤Ÿà¤¾ à¤¸à¥à¤¥à¤¿à¤¤à¤¿ à¤”à¤° à¤¦à¤¾à¤¯à¤°à¤¾", "à¤¸à¥à¤°à¥‹à¤¤ à¤¦à¤¾à¤¯à¤°à¥‡ à¤®à¥‡à¤‚ à¤¸à¤¾à¤®à¤—à¥à¤°à¥€",
                "à¤«à¤¼à¤¿à¤²à¥à¤Ÿà¤° à¤•à¥‡ à¤¬à¤¾à¤¦ à¤¸à¤¾à¤®à¤—à¥à¤°à¥€", "à¤µà¤¾à¤¸à¥à¤¤à¤µà¤¿à¤• à¤®à¤¾à¤² à¤ªà¥à¤°à¤¾à¤ªà¥à¤¤à¤¿ à¤¤à¤¿à¤¥à¤¿", "à¤¯à¥‹à¤œà¤¨à¤¾à¤•à¤¾à¤° / à¤‰à¤¤à¥à¤ªà¤¾à¤¦ à¤¸à¤®à¥‚à¤¹",
                "à¤¸à¥à¤Ÿà¥‰à¤• / à¤–à¤ªà¤¤", "à¤¸à¥à¤°à¤•à¥à¤·à¤¾ / à¤ªà¥à¤¨à¤ƒà¤†à¤¦à¥‡à¤¶", "à¤¨à¤¿à¤¶à¥à¤šà¤¿à¤¤ / à¤¨à¤¿à¤¯à¥‹à¤œà¤¿à¤¤ à¤ªà¥à¤°à¤¾à¤ªà¥à¤¤à¤¿", "à¤•à¤®à¥€ / CHF",
                "à¤ªà¥à¤°à¤­à¤¾à¤µ", "à¤–à¥à¤²à¤¾ / à¤…à¤¤à¤¿à¤¦à¥‡à¤¯", "à¤…à¤—à¤²à¥€ à¤¤à¤¿à¤¥à¤¿", "à¤¶à¥€à¤°à¥à¤· à¤†à¤ªà¥‚à¤°à¥à¤¤à¤¿à¤•à¤°à¥à¤¤à¤¾", "à¤¶à¥€à¤°à¥à¤· à¤¹à¤¿à¤¸à¥à¤¸à¤¾",
                "à¤®à¥‚à¤² à¤¸à¤¾à¤®à¤—à¥à¤°à¥€", "à¤¸à¤®à¥€à¤•à¥à¤·à¤¾ à¤•à¤¾à¤°à¥à¤¯", "MRP / à¤–à¤°à¥€à¤¦", "à¤²à¥‰à¤Ÿ à¤†à¤•à¤¾à¤°", "à¤¸à¥à¤¥à¤¿à¤¤à¤¿ / LZ",
                "à¤¨à¤¿à¤¯à¥‹à¤œà¤¿à¤¤ à¤¤à¤¿à¤¥à¤¿", "à¤–à¥à¤²à¤¾ à¤®à¥‚à¤²à¥à¤¯ CHF", "à¤µà¤¾à¤¸à¥à¤¤à¤µà¤¿à¤• à¤®à¤¾à¤² à¤ªà¥à¤°à¤¾à¤ªà¥à¤¤à¤¿", "à¤¸à¥à¤°à¥‹à¤¤ à¤—à¤¾à¤¯à¤¬",
                "à¤µà¤°à¥à¤¤à¤®à¤¾à¤¨ à¤«à¤¼à¤¿à¤²à¥à¤Ÿà¤° à¤®à¥‡à¤‚ à¤•à¥‹à¤ˆ à¤ªà¤°à¤¿à¤£à¤¾à¤® à¤¨à¤¹à¥€à¤‚à¥¤",
                "à¤µà¤¿à¤¸à¥à¤¤à¥ƒà¤¤ à¤¤à¤¾à¤²à¤¿à¤•à¤¾ 1,000 à¤¸à¤¬à¤¸à¥‡ à¤‰à¤šà¥à¤š à¤ªà¥à¤°à¤¾à¤¥à¤®à¤¿à¤•à¤¤à¤¾ à¤µà¤¾à¤²à¥‡ à¤ªà¤°à¤¿à¤£à¤¾à¤®à¥‹à¤‚ à¤¤à¤• à¤¸à¥€à¤®à¤¿à¤¤ à¤¹à¥ˆ; KPI à¤”à¤° à¤¬à¤¾à¤° à¤ªà¥‚à¤°à¥‡ à¤«à¤¼à¤¿à¤²à¥à¤Ÿà¤° à¤¦à¤¾à¤¯à¤°à¥‡ à¤•à¤¾ à¤‰à¤ªà¤¯à¥‹à¤— à¤•à¤°à¤¤à¥‡ à¤¹à¥ˆà¤‚à¥¤",
                "à¤µà¤¿à¤¶à¥à¤²à¥‡à¤·à¤£ à¤²à¥‹à¤¡ à¤¨à¤¹à¥€à¤‚ à¤¹à¥‹ à¤¸à¤•à¤¾: ", "à¤²à¥‰à¤œà¤¿à¤¸à¥à¤Ÿà¤¿à¤•à¥à¤¸", "à¤¸à¤¾à¤®à¤—à¥à¤°à¥€ à¤¯à¥‹à¤œà¤¨à¤¾ à¤”à¤° à¤•à¤®à¥€",
                "à¤•à¤µà¤°à¥‡à¤œ à¤…à¤‚à¤¤à¤°à¤¾à¤², à¤®à¤¹à¤¤à¥à¤µà¤ªà¥‚à¤°à¥à¤£ à¤¸à¥à¤Ÿà¥‰à¤• à¤”à¤° à¤¤à¥ˆà¤¯à¤¾à¤° à¤‰à¤¤à¥à¤ªà¤¾à¤¦à¥‹à¤‚ à¤ªà¤° à¤ªà¥à¤°à¤­à¤¾à¤µ à¤•à¥‹ à¤ªà¥à¤°à¤¾à¤¥à¤®à¤¿à¤•à¤¤à¤¾ à¤¦à¥‡à¤¤à¤¾ à¤¹à¥ˆà¥¤",
                "à¤–à¤°à¥€à¤¦ à¤†à¤µà¤¶à¥à¤¯à¤•à¤¤à¤¾ à¤”à¤° à¤•à¤µà¤°à¥‡à¤œ", "SAP à¤…à¤‚à¤¤à¤¿à¤® à¤¸à¥à¤Ÿà¥‰à¤• à¤•à¥‹ à¤–à¥à¤²à¥‡ à¤†à¤¦à¥‡à¤¶à¥‹à¤‚, à¤¨à¤¿à¤¯à¥‹à¤œà¤¿à¤¤ à¤¤à¤¿à¤¥à¤¿à¤¯à¥‹à¤‚ à¤”à¤° à¤†à¤ªà¥‚à¤°à¥à¤¤à¤¿à¤•à¤°à¥à¤¤à¤¾à¤“à¤‚ à¤¸à¥‡ à¤œà¥‹à¤¡à¤¼à¤¤à¤¾ à¤¹à¥ˆà¥¤",
                "à¤†à¤ªà¥‚à¤°à¥à¤¤à¤¿à¤•à¤°à¥à¤¤à¤¾/à¤¸à¤¾à¤®à¤—à¥à¤°à¥€ à¤¨à¤¿à¤°à¥à¤­à¤°à¤¤à¤¾", "à¤¸à¤¾à¤®à¤—à¥à¤°à¥€ à¤¸à¤‚à¤–à¥à¤¯à¤¾ à¤¤à¤• à¤¦à¥‡à¤–à¥‡ à¤—à¤ à¤à¤•à¤²-à¤¸à¥à¤°à¥‹à¤¤ à¤”à¤° à¤¸à¤‚à¤•à¥‡à¤‚à¤¦à¥à¤°à¤£ à¤œà¥‹à¤–à¤¿à¤® à¤¦à¤¿à¤–à¤¾à¤¤à¤¾ à¤¹à¥ˆà¥¤",
                "à¤¯à¥‹à¤œà¤¨à¤¾ à¤ªà¥ˆà¤°à¤¾à¤®à¥€à¤Ÿà¤° à¤¸à¤®à¥€à¤•à¥à¤·à¤¾", "à¤—à¤¾à¤¯à¤¬ à¤¯à¤¾ à¤…à¤¸à¤¾à¤®à¤¾à¤¨à¥à¤¯ MARC/MARA à¤ªà¥ˆà¤°à¤¾à¤®à¥€à¤Ÿà¤°à¥‹à¤‚ à¤•à¥‡ à¤²à¤¿à¤ à¤…à¤²à¤— à¤¸à¤®à¥€à¤•à¥à¤·à¤¾ à¤•à¤¾à¤°à¥à¤¯ à¤¬à¤¨à¤¾à¤¤à¤¾ à¤¹à¥ˆà¥¤",
                "à¤µà¤¿à¤¤à¤°à¤£ à¤ªà¥à¤°à¤¦à¤°à¥à¤¶à¤¨ à¤”à¤° à¤¡à¥‡à¤Ÿà¤¾ à¤¸à¥à¤¥à¤¿à¤¤à¤¿", "à¤µà¤¿à¤¶à¥à¤µà¤¸à¤¨à¥€à¤¯ EKET à¤¨à¤¿à¤¯à¥‹à¤œà¤¿à¤¤-à¤¤à¤¿à¤¥à¤¿ à¤¬à¥ˆà¤•à¤²à¥‰à¤— à¤¦à¤¿à¤–à¤¾à¤¤à¤¾ à¤¹à¥ˆ à¤”à¤° à¤‰à¤¸à¥‡ à¤…à¤­à¥€ à¤¨ à¤®à¤¾à¤ªà¥€ à¤œà¤¾ à¤¸à¤•à¤¨à¥‡ à¤µà¤¾à¤²à¥€ à¤µà¤¾à¤¸à¥à¤¤à¤µà¤¿à¤• à¤¸à¤®à¤¯à¤ªà¤¾à¤²à¤¨ à¤¸à¥‡ à¤…à¤²à¤— à¤°à¤–à¤¤à¤¾ à¤¹à¥ˆà¥¤"
            ]),
            ["sq"] = Map([
                "Material, tekst ose furnitor", "Planifikues", "Grup produktesh", "VetÃ«m rastet qÃ« kÃ«rkojnÃ« veprim",
                "Prioritetet nÃ« filtrin aktual", "Gjendja dhe kufiri i tÃ« dhÃ«nave", "Materialet nÃ« burim",
                "Materialet pas filtrit", "Data reale e pranimit tÃ« mallit", "Planifikues / grup produktesh",
                "Stok / konsum", "Siguri / riporositje", "Hyrje fikse / tÃ« planifikuara", "MungesÃ« / CHF",
                "Ndikim", "Hapur / me vonesÃ«", "Data e ardhshme", "Furnitori kryesor", "Pjesa kryesore",
                "Materialet prind", "DetyrÃ« verifikimi", "MRP / furnizim", "MadhÃ«sia e lotit", "Status / LZ",
                "Data e planifikuar", "Vlera e hapur CHF", "Pranimi real i mallit", "Burimi mungon",
                "Nuk ka rezultate nÃ« filtrin aktual.",
                "Tabela e detajuar kufizohet nÃ« 1.000 rezultatet me pÃ«rparÃ«si mÃ« tÃ« lartÃ«; KPI-tÃ« dhe shiritat pÃ«rdorin tÃ« gjithÃ« filtrin.",
                "Analiza nuk mund tÃ« ngarkohej: ", "LogjistikÃ«", "Planifikimi i materialeve dhe mungesat",
                "Jep pÃ«rparÃ«si boshllÃ«qeve tÃ« mbulimit, stokut kritik dhe ndikimit te produktet e gatshme.",
                "Nevoja pÃ«r blerje dhe mbulimi", "Lidh stokun pÃ«rfundimtar SAP me porositÃ« e hapura, datat e planifikuara dhe furnitorÃ«t.",
                "VarÃ«sia furnitor/material", "Tregon rreziqet e vÃ«zhguara nga njÃ« burim dhe pÃ«rqendrimi deri te numri i materialit.",
                "Kontrolli i parametrave tÃ« planifikimit", "Krijon detyra tÃ« ndara kontrolli pÃ«r parametra MARC/MARA qÃ« mungojnÃ« ose janÃ« tÃ« pazakontÃ«.",
                "Performanca e dorÃ«zimit dhe gjendja e tÃ« dhÃ«nave", "Tregon vonesat e besueshme tÃ« datave EKET dhe i ndan nga pÃ«rpikÃ«ria reale qÃ« ende nuk matet."
            ]),
            ["tr"] = Map([
                "Malzeme, metin veya tedarikÃ§i", "PlanlayÄ±cÄ±", "ÃœrÃ¼n grubu", "YalnÄ±zca iÅŸlem gerekenler",
                "GeÃ§erli filtredeki Ã¶ncelikler", "Veri durumu ve kapsamÄ±", "Kaynak kapsamÄ±ndaki malzemeler",
                "Filtre sonrasÄ± malzemeler", "GerÃ§ek mal kabul tarihi", "PlanlayÄ±cÄ± / Ã¼rÃ¼n grubu",
                "Stok / tÃ¼ketim", "Emniyet / yeniden sipariÅŸ", "Sabit / planlÄ± giriÅŸler", "Eksik miktar / CHF",
                "Etki", "AÃ§Ä±k / gecikmiÅŸ", "Sonraki tarih", "En bÃ¼yÃ¼k tedarikÃ§i", "En bÃ¼yÃ¼k pay",
                "Ãœst malzemeler", "Kontrol gÃ¶revi", "MRP / tedarik", "Parti bÃ¼yÃ¼klÃ¼ÄŸÃ¼", "Durum / LZ",
                "PlanlÄ± tarih", "AÃ§Ä±k deÄŸer CHF", "GerÃ§ek mal kabulÃ¼", "Kaynak eksik",
                "GeÃ§erli filtrede sonuÃ§ yok.",
                "AyrÄ±ntÄ± tablosu en yÃ¼ksek Ã¶ncelikli 1.000 sonuÃ§la sÄ±nÄ±rlÄ±dÄ±r; KPI'lar ve Ã¶ncelik Ã§ubuklarÄ± tÃ¼m filtre kapsamÄ±nÄ± kullanÄ±r.",
                "Analiz yÃ¼klenemedi: ", "Lojistik", "Malzeme planlama ve eksikler",
                "Kapsama aÃ§Ä±klarÄ±na, kritik stoklara ve bitmiÅŸ Ã¼rÃ¼nler Ã¼zerindeki etkiye Ã¶ncelik verir.",
                "SatÄ±n alma ihtiyacÄ± ve kapsam", "SAP nihai stokunu aÃ§Ä±k sipariÅŸler, planlÄ± tarihler ve tedarikÃ§ilerle baÄŸlar.",
                "TedarikÃ§i/malzeme baÄŸÄ±mlÄ±lÄ±ÄŸÄ±", "Malzeme numarasÄ±na kadar gÃ¶zlenen tek kaynak ve yoÄŸunlaÅŸma risklerini gÃ¶sterir.",
                "Planlama parametresi kontrolÃ¼", "Eksik veya dikkat Ã§eken MARC/MARA parametreleri iÃ§in ayrÄ± kontrol gÃ¶revleri oluÅŸturur.",
                "Teslimat performansÄ± ve veri durumu", "GÃ¼venilir EKET planlÄ± tarih gecikmesini gÃ¶sterir ve henÃ¼z Ã¶lÃ§Ã¼lemeyen gerÃ§ek zamanÄ±nda teslimattan ayÄ±rÄ±r."
            ]),
            ["tlh"] = Map([
                "Hap, ghItlh ghap ngevwI'", "nabwI'", "Doch ghom", "vangnISbogh neH",
                "Daqvam wa'DIch pat", "De' Dotlh mIch je", "mung mIch Hapmey", "filter qa'DI' Hapmey",
                "Hap Hevlu'bej jaj", "nabwI' / Doch ghom", "polbogh mI' / lo'", "Hung / raQqa'",
                "pawbej / paw nab", "ngebbogh mI' / CHF", "vangDI' qaD", "poS / paSqu'", "jaj veb",
                "ngevwI' wa'DIch", "wav wa'DIch", "Hap qup", "nuD Qu'", "MRP / Suq", "ghom'a' mI'", "Dotlh / LZ",
                "jaj nab", "poS Huch CHF", "Hap Hevlu'bej", "mung tu'be'lu'", "filtervamDaq pagh tu'lu'.",
                "wa'SaD Qob wa'DIch neH 'ang De' tetlh; Hoch filter lo' KPI pat je.", "poj laadlaHbe': ",
                "Hap lup", "Hap nab ngebbogh Hap je", "Hap Hutlhghach, polbogh Hap Qob, Doch rInmoHbogh qaD je wa'DIch cher.",
                "je'nISghach polghach je", "SAP polbogh mI' Qav, je' poS, jaj nab, ngevwI' je rar.",
                "ngevwI' Hap je rarghach", "wa' mung, boSghach Qob je Hap mI' Daq 'ang.",
                "nab De' nuD", "MARC MARA De' Hutlhbogh pagh motlhbe'bogh nuD Qu'mey chev chenmoH.",
                "Hap pawlaHghach De' Dotlh je", "EKET jaj nab paSghach 'ang 'ej jaj teH wej juSlu'bogh chev."
            ])
        };

    private static IReadOnlyDictionary<string, string> Map(IReadOnlyList<string> translations)
    {
        if (translations.Count != SupplyChainUiTextCatalog.All.Count)
            throw new InvalidOperationException($"Supply-chain translation count {translations.Count} does not match catalog count {SupplyChainUiTextCatalog.All.Count}.");

        return SupplyChainUiTextCatalog.All
            .Select((pair, index) => (pair.German, Translation: translations[index]))
            .ToDictionary(pair => pair.German, pair => pair.Translation, StringComparer.OrdinalIgnoreCase);
    }
}
