// === PromoMaster - Product Promotion Tool ===
// Vollautomatisches Produkt-Promotion-Tool in Deutsch und Englisch

(function () {
    'use strict';

    // --- State ---
    let currentLang = 'de';
    let selectedStyle = 'professional';
    let generatedData = null;

    // --- Language System ---
    function setLanguage(lang) {
        currentLang = lang;
        document.documentElement.setAttribute('data-lang', lang);
        document.getElementById('btn-de').classList.toggle('active', lang === 'de');
        document.getElementById('btn-en').classList.toggle('active', lang === 'en');

        document.querySelectorAll('[data-de]').forEach(function (el) {
            el.textContent = el.getAttribute('data-' + lang);
        });

        document.querySelectorAll('[data-de-placeholder]').forEach(function (el) {
            el.placeholder = el.getAttribute('data-' + lang + '-placeholder');
        });

        document.querySelectorAll('select option').forEach(function (opt) {
            var val = opt.getAttribute('data-' + lang);
            if (val) opt.textContent = val;
        });
    }

    window.setLanguage = setLanguage;

    // --- Style Selection ---
    function selectStyle(el) {
        document.querySelectorAll('.style-option').forEach(function (s) {
            s.classList.remove('selected');
        });
        el.classList.add('selected');
        selectedStyle = el.getAttribute('data-style');
    }

    window.selectStyle = selectStyle;

    // --- Character Counter ---
    var descInput = document.getElementById('productDescription');
    var charCount = document.getElementById('charCount');
    if (descInput && charCount) {
        descInput.addEventListener('input', function () {
            charCount.textContent = descInput.value.length;
        });
    }

    // --- Text Templates ---
    var templates = {
        professional: {
            twitter: {
                de: function (p) {
                    return 'Entdecken Sie ' + p.name + ' \u2013 ' + p.desc + (p.features.length ? '\n\n\u2705 ' + p.features.slice(0, 3).join('\n\u2705 ') : '') + (p.url ? '\n\n\ud83d\udc49 ' + p.url : '') + (p.price ? '\n\ud83d\udcb0 ' + p.price : '') + '\n\n' + p.hashtags.slice(0, 4).join(' ');
                },
                en: function (p) {
                    return 'Discover ' + p.name + ' \u2013 ' + p.descEn + (p.features.length ? '\n\n\u2705 ' + p.featuresEn.slice(0, 3).join('\n\u2705 ') : '') + (p.url ? '\n\n\ud83d\udc49 ' + p.url : '') + (p.price ? '\n\ud83d\udcb0 ' + p.price : '') + '\n\n' + p.hashtagsEn.slice(0, 4).join(' ');
                }
            },
            instagram: {
                de: function (p) {
                    return '\u2728 ' + p.name + ' \u2013 Die Zukunft beginnt jetzt!\n\n' + p.desc + '\n\n' + (p.features.length ? 'Was ' + p.name + ' besonders macht:\n' + p.features.map(function (f) { return '\ud83d\udd39 ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? '\ud83c\udfaf Perfekt f\u00fcr: ' + p.audience + '\n\n' : '') + (p.price ? '\ud83d\udcb0 Jetzt f\u00fcr nur ' + p.price + '\n\n' : '') + (p.url ? '\ud83d\udd17 Link in Bio \u2b06\ufe0f\n\n' : '') + p.hashtags.join(' ');
                },
                en: function (p) {
                    return '\u2728 ' + p.name + ' \u2013 The future starts now!\n\n' + p.descEn + '\n\n' + (p.features.length ? 'What makes ' + p.name + ' special:\n' + p.featuresEn.map(function (f) { return '\ud83d\udd39 ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? '\ud83c\udfaf Perfect for: ' + p.audienceEn + '\n\n' : '') + (p.price ? '\ud83d\udcb0 Now only ' + p.price + '\n\n' : '') + (p.url ? '\ud83d\udd17 Link in bio \u2b06\ufe0f\n\n' : '') + p.hashtagsEn.join(' ');
                }
            },
            facebook: {
                de: function (p) {
                    return '\ud83d\ude80 Neu: ' + p.name + '!\n\n' + p.desc + '\n\n' + (p.features.length ? '\ud83d\udc47 Das sind die Highlights:\n\n' + p.features.map(function (f) { return '\u2714\ufe0f ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? 'Perfekt f\u00fcr alle, die ' + p.audience + ' sind.\n\n' : '') + (p.price ? '\ud83d\udcb5 Preis: ' + p.price + '\n\n' : '') + (p.url ? '\ud83c\udf10 Mehr erfahren: ' + p.url + '\n\n' : '') + 'Was denkt ihr? Lasst es uns in den Kommentaren wissen! \ud83d\udc47\n\n' + p.hashtags.slice(0, 5).join(' ');
                },
                en: function (p) {
                    return '\ud83d\ude80 New: ' + p.name + '!\n\n' + p.descEn + '\n\n' + (p.features.length ? '\ud83d\udc47 Here are the highlights:\n\n' + p.featuresEn.map(function (f) { return '\u2714\ufe0f ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? 'Perfect for everyone who is ' + p.audienceEn + '.\n\n' : '') + (p.price ? '\ud83d\udcb5 Price: ' + p.price + '\n\n' : '') + (p.url ? '\ud83c\udf10 Learn more: ' + p.url + '\n\n' : '') + 'What do you think? Let us know in the comments! \ud83d\udc47\n\n' + p.hashtagsEn.slice(0, 5).join(' ');
                }
            },
            linkedin: {
                de: function (p) {
                    return '\ud83d\udca1 ' + p.name + ' \u2013 Innovation trifft Effizienz\n\nIch freue mich, Ihnen ' + p.name + ' vorzustellen.\n\n' + p.desc + '\n\n' + (p.features.length ? 'Die wichtigsten Vorteile:\n\n' + p.features.map(function (f) { return '\u2192 ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? 'Zielgruppe: ' + p.audience + '\n\n' : '') + (p.price ? 'Investition: ' + p.price + '\n\n' : '') + (p.url ? 'Erfahren Sie mehr: ' + p.url + '\n\n' : '') + '#Innovation #Business ' + p.hashtags.slice(0, 3).join(' ');
                },
                en: function (p) {
                    return '\ud83d\udca1 ' + p.name + ' \u2013 Innovation meets Efficiency\n\nI\'m excited to introduce ' + p.name + ' to you.\n\n' + p.descEn + '\n\n' + (p.features.length ? 'Key benefits:\n\n' + p.featuresEn.map(function (f) { return '\u2192 ' + f; }).join('\n') + '\n\n' : '') + (p.audience ? 'Target audience: ' + p.audienceEn + '\n\n' : '') + (p.price ? 'Investment: ' + p.price + '\n\n' : '') + (p.url ? 'Learn more: ' + p.url + '\n\n' : '') + '#Innovation #Business ' + p.hashtagsEn.slice(0, 3).join(' ');
                }
            },
            tiktok: {
                de: function (p) {
                    return '\ud83d\udd25 POV: Du entdeckst gerade ' + p.name + '!\n\n' + p.desc + '\n\n' + (p.features.length ? p.features.slice(0, 3).map(function (f) { return '\u2728 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? '\ud83d\udcb0 ' + p.price + '\n\n' : '') + (p.url ? '\ud83d\udd17 Link in Bio!\n\n' : '') + p.hashtags.join(' ') + ' #fyp #viral #musthave';
                },
                en: function (p) {
                    return '\ud83d\udd25 POV: You just discovered ' + p.name + '!\n\n' + p.descEn + '\n\n' + (p.features.length ? p.featuresEn.slice(0, 3).map(function (f) { return '\u2728 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? '\ud83d\udcb0 ' + p.price + '\n\n' : '') + (p.url ? '\ud83d\udd17 Link in bio!\n\n' : '') + p.hashtagsEn.join(' ') + ' #fyp #viral #musthave';
                }
            }
        },
        casual: {
            twitter: {
                de: function (p) { return 'Hey Leute! \ud83d\udc4b Kennt ihr schon ' + p.name + '? ' + p.desc + (p.features.length ? '\n\nDas Beste daran:\n' + p.features.slice(0, 3).map(function (f) { return '\ud83d\udc4d ' + f; }).join('\n') : '') + (p.url ? '\n\nSchaut mal vorbei: ' + p.url : '') + '\n\n' + p.hashtags.slice(0, 4).join(' '); },
                en: function (p) { return 'Hey everyone! \ud83d\udc4b Have you heard of ' + p.name + '? ' + p.descEn + (p.features.length ? '\n\nBest thing about it:\n' + p.featuresEn.slice(0, 3).map(function (f) { return '\ud83d\udc4d ' + f; }).join('\n') : '') + (p.url ? '\n\nCheck it out: ' + p.url : '') + '\n\n' + p.hashtagsEn.slice(0, 4).join(' '); }
            },
            instagram: {
                de: function (p) { return 'Schaut mal was ich gefunden habe! \ud83e\udd29\n\n' + p.name + ' \u2013 ' + p.desc + '\n\n' + (p.features.length ? 'Warum ich es liebe:\n' + p.features.map(function (f) { return '\u2764\ufe0f ' + f; }).join('\n') + '\n\n' : '') + 'Wer will es auch haben? \ud83d\ude4b\u200d\u2640\ufe0f\n\n' + p.hashtags.join(' '); },
                en: function (p) { return 'Look what I found! \ud83e\udd29\n\n' + p.name + ' \u2013 ' + p.descEn + '\n\n' + (p.features.length ? 'Why I love it:\n' + p.featuresEn.map(function (f) { return '\u2764\ufe0f ' + f; }).join('\n') + '\n\n' : '') + 'Who else wants this? \ud83d\ude4b\u200d\u2640\ufe0f\n\n' + p.hashtagsEn.join(' '); }
            },
            facebook: {
                de: function (p) { return 'Hey Freunde! \ud83d\udc4b\n\nIch muss euch unbedingt von ' + p.name + ' erz\u00e4hlen!\n\n' + p.desc + '\n\n' + (p.features.length ? 'Was mich \u00fcberzeugt hat:\n' + p.features.map(function (f) { return '\ud83d\udc49 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? 'Und das f\u00fcr ' + p.price + ' \u2013 richtig fair!\n\n' : '') + (p.url ? 'Hier gehts lang: ' + p.url + '\n\n' : '') + 'Kennt ihr das schon? \ud83d\ude0d'; },
                en: function (p) { return 'Hey friends! \ud83d\udc4b\n\nI have to tell you about ' + p.name + '!\n\n' + p.descEn + '\n\n' + (p.features.length ? 'What convinced me:\n' + p.featuresEn.map(function (f) { return '\ud83d\udc49 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? 'And all that for ' + p.price + ' \u2013 such a great deal!\n\n' : '') + (p.url ? 'Check it here: ' + p.url + '\n\n' : '') + 'Have you heard of this? \ud83d\ude0d'; }
            },
            linkedin: {
                de: function (p) { return 'Moin zusammen! \ud83d\ude4c\n\nDarf ich vorstellen: ' + p.name + '!\n\n' + p.desc + '\n\n' + (p.features.length ? 'Das macht es so cool:\n' + p.features.map(function (f) { return '\u2022 ' + f; }).join('\n') + '\n\n' : '') + 'Wer hat Lust, das mal auszuprobieren?\n\n' + p.hashtags.slice(0, 3).join(' '); },
                en: function (p) { return 'Hey everyone! \ud83d\ude4c\n\nLet me introduce: ' + p.name + '!\n\n' + p.descEn + '\n\n' + (p.features.length ? 'What makes it awesome:\n' + p.featuresEn.map(function (f) { return '\u2022 ' + f; }).join('\n') + '\n\n' : '') + 'Who wants to give it a try?\n\n' + p.hashtagsEn.slice(0, 3).join(' '); }
            },
            tiktok: {
                de: function (p) { return 'OK das m\u00fcsst ihr sehen!! \ud83d\ude31\n\n' + p.name + ' ist einfach WILD!\n' + p.desc + '\n\n' + (p.features.length ? p.features.slice(0, 2).map(function (f) { return '\ud83e\udd2f ' + f; }).join('\n') + '\n\n' : '') + 'Kommentiert wenn ihrs auch braucht! \ud83d\udc47\n\n' + p.hashtags.join(' ') + ' #fyp #musthave'; },
                en: function (p) { return 'OK you NEED to see this!! \ud83d\ude31\n\n' + p.name + ' is absolutely WILD!\n' + p.descEn + '\n\n' + (p.features.length ? p.featuresEn.slice(0, 2).map(function (f) { return '\ud83e\udd2f ' + f; }).join('\n') + '\n\n' : '') + 'Comment if you need this too! \ud83d\udc47\n\n' + p.hashtagsEn.join(' ') + ' #fyp #musthave'; }
            }
        },
        urgent: {
            twitter: {
                de: function (p) { return '\u26a0\ufe0f NUR F\u00dcR KURZE ZEIT: ' + p.name + '!\n\n' + p.desc + '\n\n' + (p.price ? '\ud83d\udcb0 Jetzt zuschlagen: ' + p.price + '\n' : '') + '\u23f0 Begrenztes Angebot \u2013 nicht verpassen!\n\n' + (p.url ? '\ud83d\udc49 Sofort sichern: ' + p.url + '\n\n' : '') + p.hashtags.slice(0, 4).join(' '); },
                en: function (p) { return '\u26a0\ufe0f LIMITED TIME ONLY: ' + p.name + '!\n\n' + p.descEn + '\n\n' + (p.price ? '\ud83d\udcb0 Get it now: ' + p.price + '\n' : '') + '\u23f0 Limited offer \u2013 don\'t miss out!\n\n' + (p.url ? '\ud83d\udc49 Grab yours: ' + p.url + '\n\n' : '') + p.hashtagsEn.slice(0, 4).join(' '); }
            },
            instagram: {
                de: function (p) { return '\ud83d\udea8 ACHTUNG! \ud83d\udea8\n\n' + p.name + ' ist DA!\n\n' + p.desc + '\n\n' + (p.features.length ? '\ud83d\udd25 ' + p.features.join(' \u2022 ') + '\n\n' : '') + '\u23f3 Nur solange der Vorrat reicht!\n' + (p.price ? '\ud83d\udcb0 ' + p.price + '\n\n' : '') + 'JETZT HANDELN bevor es zu sp\u00e4t ist! \ud83d\udc47\n\n' + p.hashtags.join(' '); },
                en: function (p) { return '\ud83d\udea8 ATTENTION! \ud83d\udea8\n\n' + p.name + ' is HERE!\n\n' + p.descEn + '\n\n' + (p.features.length ? '\ud83d\udd25 ' + p.featuresEn.join(' \u2022 ') + '\n\n' : '') + '\u23f3 Only while supplies last!\n' + (p.price ? '\ud83d\udcb0 ' + p.price + '\n\n' : '') + 'ACT NOW before it\'s too late! \ud83d\udc47\n\n' + p.hashtagsEn.join(' '); }
            },
            facebook: {
                de: function (p) { return '\ud83d\udea8\ud83d\udea8\ud83d\udea8 EILMELDUNG \ud83d\udea8\ud83d\udea8\ud83d\udea8\n\n' + p.name + ' ist endlich verf\u00fcgbar!\n\n' + p.desc + '\n\n' + (p.features.length ? 'Die Fakten:\n' + p.features.map(function (f) { return '\u26a1 ' + f; }).join('\n') + '\n\n' : '') + '\u23f0 ACHTUNG: Angebot endet bald!\n' + (p.price ? '\ud83d\udcb5 Nur ' + p.price + '\n' : '') + (p.url ? '\n\ud83d\udc49 SOFORT ZUSCHLAGEN: ' + p.url : ''); },
                en: function (p) { return '\ud83d\udea8\ud83d\udea8\ud83d\udea8 BREAKING \ud83d\udea8\ud83d\udea8\ud83d\udea8\n\n' + p.name + ' is finally available!\n\n' + p.descEn + '\n\n' + (p.features.length ? 'The facts:\n' + p.featuresEn.map(function (f) { return '\u26a1 ' + f; }).join('\n') + '\n\n' : '') + '\u23f0 WARNING: Offer ends soon!\n' + (p.price ? '\ud83d\udcb5 Only ' + p.price + '\n' : '') + (p.url ? '\n\ud83d\udc49 GRAB IT NOW: ' + p.url : ''); }
            },
            linkedin: {
                de: function (p) { return '\ud83d\udea8 Dringende Marktchance: ' + p.name + '\n\n' + p.desc + '\n\n' + (p.features.length ? 'Schl\u00fcsselvorteile:\n' + p.features.map(function (f) { return '\u2192 ' + f; }).join('\n') + '\n\n' : '') + 'Dieses Angebot ist zeitlich begrenzt. Wer jetzt nicht handelt, verpasst eine einmalige Gelegenheit.\n\n' + (p.url ? p.url + '\n\n' : '') + p.hashtags.slice(0, 3).join(' '); },
                en: function (p) { return '\ud83d\udea8 Urgent Market Opportunity: ' + p.name + '\n\n' + p.descEn + '\n\n' + (p.features.length ? 'Key advantages:\n' + p.featuresEn.map(function (f) { return '\u2192 ' + f; }).join('\n') + '\n\n' : '') + 'This offer is time-limited. Those who don\'t act now will miss a unique opportunity.\n\n' + (p.url ? p.url + '\n\n' : '') + p.hashtagsEn.slice(0, 3).join(' '); }
            },
            tiktok: {
                de: function (p) { return '\ud83d\udea8 STOP SCROLLING! \ud83d\udea8\n\n' + p.name + ' \u2013 ' + p.desc + '\n\n' + '\u23f0 Letzte Chance!\n' + (p.price ? '\ud83d\udcb0 ' + p.price + '\n' : '') + '\nLINK IN BIO BEVOR ES WEG IST!\n\n' + p.hashtags.join(' ') + ' #fyp #limitedoffer'; },
                en: function (p) { return '\ud83d\udea8 STOP SCROLLING! \ud83d\udea8\n\n' + p.name + ' \u2013 ' + p.descEn + '\n\n' + '\u23f0 Last chance!\n' + (p.price ? '\ud83d\udcb0 ' + p.price + '\n' : '') + '\nLINK IN BIO BEFORE IT\'S GONE!\n\n' + p.hashtagsEn.join(' ') + ' #fyp #limitedoffer'; }
            }
        },
        luxury: {
            twitter: {
                de: function (p) { return '\u2728 ' + p.name + '\n\nExklusivit\u00e4t neu definiert.\n' + p.desc + '\n\n' + (p.price ? 'Ab ' + p.price + '\n' : '') + (p.url ? '\n' + p.url : '') + '\n\n' + p.hashtags.slice(0, 3).join(' ') + ' #Luxus #Premium'; },
                en: function (p) { return '\u2728 ' + p.name + '\n\nRedefining exclusivity.\n' + p.descEn + '\n\n' + (p.price ? 'From ' + p.price + '\n' : '') + (p.url ? '\n' + p.url : '') + '\n\n' + p.hashtagsEn.slice(0, 3).join(' ') + ' #Luxury #Premium'; }
            },
            instagram: {
                de: function (p) { return '\u2726 ' + p.name.toUpperCase() + ' \u2726\n\n' + p.desc + '\n\n' + (p.features.length ? 'Exklusive Merkmale:\n' + p.features.map(function (f) { return '\u2726 ' + f; }).join('\n') + '\n\n' : '') + 'F\u00fcr alle, die das Beste verdienen.\n\n' + (p.price ? '\u2726 ' + p.price + '\n\n' : '') + p.hashtags.join(' ') + ' #Luxury #Exclusive'; },
                en: function (p) { return '\u2726 ' + p.name.toUpperCase() + ' \u2726\n\n' + p.descEn + '\n\n' + (p.features.length ? 'Exclusive features:\n' + p.featuresEn.map(function (f) { return '\u2726 ' + f; }).join('\n') + '\n\n' : '') + 'For those who deserve the finest.\n\n' + (p.price ? '\u2726 ' + p.price + '\n\n' : '') + p.hashtagsEn.join(' ') + ' #Luxury #Exclusive'; }
            },
            facebook: {
                de: function (p) { return '\u2014\u2014\u2014 ' + p.name.toUpperCase() + ' \u2014\u2014\u2014\n\n' + p.desc + '\n\n' + (p.features.length ? p.features.map(function (f) { return '\u25c7 ' + f; }).join('\n') + '\n\n' : '') + 'Perfektion kennt keine Kompromisse.\n\n' + (p.price ? 'Ab ' + p.price + '\n' : '') + (p.url ? '\nEntdecken Sie mehr: ' + p.url : ''); },
                en: function (p) { return '\u2014\u2014\u2014 ' + p.name.toUpperCase() + ' \u2014\u2014\u2014\n\n' + p.descEn + '\n\n' + (p.features.length ? p.featuresEn.map(function (f) { return '\u25c7 ' + f; }).join('\n') + '\n\n' : '') + 'Perfection knows no compromise.\n\n' + (p.price ? 'From ' + p.price + '\n' : '') + (p.url ? '\nDiscover more: ' + p.url : ''); }
            },
            linkedin: {
                de: function (p) { return p.name + ' \u2013 Exzellenz in jeder Hinsicht\n\n' + p.desc + '\n\n' + (p.features.length ? p.features.map(function (f) { return '\u2022 ' + f; }).join('\n') + '\n\n' : '') + 'Wir setzen Ma\u00dfst\u00e4be f\u00fcr Premium-Qualit\u00e4t.\n\n' + (p.url ? p.url + '\n\n' : '') + '#Premium #Excellence ' + p.hashtags.slice(0, 2).join(' '); },
                en: function (p) { return p.name + ' \u2013 Excellence in every way\n\n' + p.descEn + '\n\n' + (p.features.length ? p.featuresEn.map(function (f) { return '\u2022 ' + f; }).join('\n') + '\n\n' : '') + 'We set the standard for premium quality.\n\n' + (p.url ? p.url + '\n\n' : '') + '#Premium #Excellence ' + p.hashtagsEn.slice(0, 2).join(' '); }
            },
            tiktok: {
                de: function (p) { return '\u2728 ' + p.name + ' \u2013 Luxus der n\u00e4chsten Generation\n\n' + p.desc + '\n\n' + (p.price ? '\u2726 ' + p.price + '\n\n' : '') + p.hashtags.join(' ') + ' #luxury #aesthetic #premium'; },
                en: function (p) { return '\u2728 ' + p.name + ' \u2013 Next generation luxury\n\n' + p.descEn + '\n\n' + (p.price ? '\u2726 ' + p.price + '\n\n' : '') + p.hashtagsEn.join(' ') + ' #luxury #aesthetic #premium'; }
            }
        },
        fun: {
            twitter: {
                de: function (p) { return '\ud83c\udf89 YOOO! ' + p.name + ' ist da und es ist der HAMMER! \ud83d\udd28\n\n' + p.desc + '\n\n' + (p.features.length ? p.features.slice(0, 3).map(function (f) { return '\ud83c\udf1f ' + f; }).join('\n') + '\n' : '') + (p.url ? '\n\ud83d\ude80 Ab gehts: ' + p.url : '') + '\n\n' + p.hashtags.slice(0, 4).join(' '); },
                en: function (p) { return '\ud83c\udf89 YOOO! ' + p.name + ' is here and it\'s AMAZING! \ud83d\udd28\n\n' + p.descEn + '\n\n' + (p.features.length ? p.featuresEn.slice(0, 3).map(function (f) { return '\ud83c\udf1f ' + f; }).join('\n') + '\n' : '') + (p.url ? '\n\ud83d\ude80 Let\'s go: ' + p.url : '') + '\n\n' + p.hashtagsEn.slice(0, 4).join(' '); }
            },
            instagram: {
                de: function (p) { return '\ud83e\udd2f OKAY WOW \ud83e\udd2f\n\n' + p.name + ' hat mein Leben ver\u00e4ndert und ich bin NICHT dramatisch! \ud83d\ude02\n\n' + p.desc + '\n\n' + (p.features.length ? 'Reasons to love it:\n' + p.features.map(function (f) { return '\ud83d\udcab ' + f; }).join('\n') + '\n\n' : '') + 'Wer ist dabei?! \ud83d\ude4b\u200d\u2642\ufe0f\n\n' + p.hashtags.join(' '); },
                en: function (p) { return '\ud83e\udd2f OKAY WOW \ud83e\udd2f\n\n' + p.name + ' changed my life and I\'m NOT being dramatic! \ud83d\ude02\n\n' + p.descEn + '\n\n' + (p.features.length ? 'Reasons to love it:\n' + p.featuresEn.map(function (f) { return '\ud83d\udcab ' + f; }).join('\n') + '\n\n' : '') + 'Who\'s in?! \ud83d\ude4b\u200d\u2642\ufe0f\n\n' + p.hashtagsEn.join(' '); }
            },
            facebook: {
                de: function (p) { return '\ud83c\udf89\ud83c\udf89\ud83c\udf89 ES IST SOWEIT! \ud83c\udf89\ud83c\udf89\ud83c\udf89\n\n' + p.name + ' ist gelandet und wir k\u00f6nnen nicht aufh\u00f6ren dar\u00fcber zu reden!\n\n' + p.desc + '\n\n' + (p.features.length ? '\ud83d\ude0d Das ist alles drin:\n' + p.features.map(function (f) { return '\ud83d\udca5 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? 'Und das Beste? Nur ' + p.price + '! \ud83e\udd11\n\n' : '') + 'TAGGT jemanden der das braucht! \ud83d\udc47\ud83d\udc47\ud83d\udc47'; },
                en: function (p) { return '\ud83c\udf89\ud83c\udf89\ud83c\udf89 IT\'S HERE! \ud83c\udf89\ud83c\udf89\ud83c\udf89\n\n' + p.name + ' has landed and we can\'t stop talking about it!\n\n' + p.descEn + '\n\n' + (p.features.length ? '\ud83d\ude0d Here\'s what you get:\n' + p.featuresEn.map(function (f) { return '\ud83d\udca5 ' + f; }).join('\n') + '\n\n' : '') + (p.price ? 'Best part? Only ' + p.price + '! \ud83e\udd11\n\n' : '') + 'TAG someone who needs this! \ud83d\udc47\ud83d\udc47\ud83d\udc47'; }
            },
            linkedin: {
                de: function (p) { return '\ud83d\ude80 Plot Twist: ' + p.name + ' existiert jetzt!\n\n' + p.desc + '\n\n' + (p.features.length ? 'Die Highlights (ja, es wird noch besser):\n' + p.features.map(function (f) { return '\ud83d\udcaa ' + f; }).join('\n') + '\n\n' : '') + 'Wer will mitmachen? Schreibt mir! \ud83d\ude0e\n\n' + p.hashtags.slice(0, 3).join(' '); },
                en: function (p) { return '\ud83d\ude80 Plot Twist: ' + p.name + ' now exists!\n\n' + p.descEn + '\n\n' + (p.features.length ? 'The highlights (yes, it gets even better):\n' + p.featuresEn.map(function (f) { return '\ud83d\udcaa ' + f; }).join('\n') + '\n\n' : '') + 'Who\'s in? DM me! \ud83d\ude0e\n\n' + p.hashtagsEn.slice(0, 3).join(' '); }
            },
            tiktok: {
                de: function (p) { return '\ud83e\udee3 Wenn du ' + p.name + ' noch nicht kennst, lebst du unter einem Stein!\n\n' + p.desc + '\n\n' + (p.price ? '\ud83d\udcb0 ' + p.price + ' \u2013 SCHNAPPER!\n' : '') + '\nSpeichern & Teilen nicht vergessen! \ud83d\ude4f\n\n' + p.hashtags.join(' ') + ' #fyp #gamechanger'; },
                en: function (p) { return '\ud83e\udee3 If you don\'t know ' + p.name + ' yet, you\'re living under a rock!\n\n' + p.descEn + '\n\n' + (p.price ? '\ud83d\udcb0 ' + p.price + ' \u2013 STEAL!\n' : '') + '\nSave & Share! \ud83d\ude4f\n\n' + p.hashtagsEn.join(' ') + ' #fyp #gamechanger'; }
            }
        },
        minimal: {
            twitter: {
                de: function (p) { return p.name + '.\n' + p.desc + (p.url ? '\n\n' + p.url : '') + '\n\n' + p.hashtags.slice(0, 3).join(' '); },
                en: function (p) { return p.name + '.\n' + p.descEn + (p.url ? '\n\n' + p.url : '') + '\n\n' + p.hashtagsEn.slice(0, 3).join(' '); }
            },
            instagram: {
                de: function (p) { return p.name + '\n\n' + p.desc + (p.features.length ? '\n\n' + p.features.join(' / ') : '') + '\n\n' + p.hashtags.join(' '); },
                en: function (p) { return p.name + '\n\n' + p.descEn + (p.features.length ? '\n\n' + p.featuresEn.join(' / ') : '') + '\n\n' + p.hashtagsEn.join(' '); }
            },
            facebook: {
                de: function (p) { return p.name + '\n\n' + p.desc + (p.features.length ? '\n\n' + p.features.join(' \u2022 ') : '') + (p.price ? '\n\n' + p.price : '') + (p.url ? '\n\n' + p.url : ''); },
                en: function (p) { return p.name + '\n\n' + p.descEn + (p.features.length ? '\n\n' + p.featuresEn.join(' \u2022 ') : '') + (p.price ? '\n\n' + p.price : '') + (p.url ? '\n\n' + p.url : ''); }
            },
            linkedin: {
                de: function (p) { return p.name + '\n\n' + p.desc + (p.features.length ? '\n\n' + p.features.map(function (f) { return '\u2192 ' + f; }).join('\n') : '') + (p.url ? '\n\n' + p.url : ''); },
                en: function (p) { return p.name + '\n\n' + p.descEn + (p.features.length ? '\n\n' + p.featuresEn.map(function (f) { return '\u2192 ' + f; }).join('\n') : '') + (p.url ? '\n\n' + p.url : ''); }
            },
            tiktok: {
                de: function (p) { return p.name + '\n' + p.desc + '\n\n' + p.hashtags.slice(0, 5).join(' ') + ' #minimal'; },
                en: function (p) { return p.name + '\n' + p.descEn + '\n\n' + p.hashtagsEn.slice(0, 5).join(' ') + ' #minimal'; }
            }
        }
    };

    // --- Email Templates ---
    var emailTemplates = {
        de: function (p) {
            return 'Betreff: Entdecken Sie ' + p.name + ' \u2013 ' + p.desc.substring(0, 60) + '...\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                'Sehr geehrte Damen und Herren,\n\n' +
                'wir freuen uns, Ihnen ' + p.name + ' vorzustellen \u2013 ' + p.desc + '\n\n' +
                (p.features.length ?
                    'Die wichtigsten Vorteile auf einen Blick:\n\n' +
                    p.features.map(function (f) { return '  \u2714 ' + f; }).join('\n') + '\n\n' : '') +
                (p.audience ? 'Ideal f\u00fcr: ' + p.audience + '\n\n' : '') +
                (p.price ? 'Unser Angebot: ' + p.price + '\n\n' : '') +
                (p.url ? '\u27a1 Jetzt mehr erfahren: ' + p.url + '\n\n' : '') +
                'Haben Sie Fragen? Antworten Sie einfach auf diese E-Mail \u2013 wir helfen Ihnen gerne weiter.\n\n' +
                'Mit freundlichen Gr\u00fc\u00dfen,\n' +
                'Ihr ' + p.name + ' Team\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n' +
                'Sie erhalten diese E-Mail, weil Sie sich f\u00fcr ' + p.name + ' interessieren.\n' +
                'Abmelden | Datenschutz | Impressum';
        },
        en: function (p) {
            return 'Subject: Discover ' + p.name + ' \u2013 ' + p.descEn.substring(0, 60) + '...\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                'Dear Customer,\n\n' +
                'We are excited to introduce ' + p.name + ' \u2013 ' + p.descEn + '\n\n' +
                (p.features.length ?
                    'Key benefits at a glance:\n\n' +
                    p.featuresEn.map(function (f) { return '  \u2714 ' + f; }).join('\n') + '\n\n' : '') +
                (p.audience ? 'Ideal for: ' + p.audienceEn + '\n\n' : '') +
                (p.price ? 'Our offer: ' + p.price + '\n\n' : '') +
                (p.url ? '\u27a1 Learn more: ' + p.url + '\n\n' : '') +
                'Have questions? Simply reply to this email \u2013 we\'re happy to help.\n\n' +
                'Best regards,\n' +
                'The ' + p.name + ' Team\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n' +
                'You received this email because you showed interest in ' + p.name + '.\n' +
                'Unsubscribe | Privacy Policy | Legal';
        }
    };

    // --- SEO Templates ---
    var seoTemplates = {
        de: function (p) {
            return 'META TITLE:\n' + p.name + ' \u2013 ' + p.desc.substring(0, 50) + ' | Jetzt entdecken\n\n' +
                'META DESCRIPTION:\n' + p.desc + (p.features.length ? ' \u2714 ' + p.features.slice(0, 3).join(' \u2714 ') : '') + (p.price ? ' Ab ' + p.price + '.' : '') + ' Jetzt informieren!\n\n' +
                'SEO KEYWORDS:\n' + p.name + ', ' + p.name.toLowerCase() + ' kaufen, ' + p.name.toLowerCase() + ' test, ' + p.name.toLowerCase() + ' erfahrungen, ' +
                (p.category ? p.categoryDe + ', ' : '') + 'beste ' + p.name.toLowerCase() + ', ' + p.name.toLowerCase() + ' vergleich, ' + p.name.toLowerCase() + ' angebot\n\n' +
                'H1 \u00dcBERSCHRIFT:\n' + p.name + ' \u2013 ' + p.desc + '\n\n' +
                'H2 \u00dcBERSCHRIFTEN:\n' +
                'Warum ' + p.name + '?\n' +
                'Funktionen & Vorteile\n' +
                'F\u00fcr wen ist ' + p.name + ' geeignet?\n' +
                'Jetzt ' + p.name + ' bestellen\n\n' +
                'ALT-TEXT F\u00dcR BILDER:\n' +
                p.name + ' Produktbild \u2013 ' + p.desc.substring(0, 60);
        },
        en: function (p) {
            return 'META TITLE:\n' + p.name + ' \u2013 ' + p.descEn.substring(0, 50) + ' | Discover Now\n\n' +
                'META DESCRIPTION:\n' + p.descEn + (p.features.length ? ' \u2714 ' + p.featuresEn.slice(0, 3).join(' \u2714 ') : '') + (p.price ? ' From ' + p.price + '.' : '') + ' Learn more now!\n\n' +
                'SEO KEYWORDS:\n' + p.name + ', buy ' + p.name.toLowerCase() + ', ' + p.name.toLowerCase() + ' review, ' + p.name.toLowerCase() + ' features, ' +
                (p.category ? p.categoryEn + ', ' : '') + 'best ' + p.name.toLowerCase() + ', ' + p.name.toLowerCase() + ' comparison, ' + p.name.toLowerCase() + ' deal\n\n' +
                'H1 HEADING:\n' + p.name + ' \u2013 ' + p.descEn + '\n\n' +
                'H2 HEADINGS:\n' +
                'Why ' + p.name + '?\n' +
                'Features & Benefits\n' +
                'Who is ' + p.name + ' for?\n' +
                'Order ' + p.name + ' Now\n\n' +
                'IMAGE ALT TEXT:\n' +
                p.name + ' product image \u2013 ' + p.descEn.substring(0, 60);
        }
    };

    // --- Press Release Templates ---
    var pressTemplates = {
        de: function (p) {
            var today = new Date().toLocaleDateString('de-DE', { year: 'numeric', month: 'long', day: 'numeric' });
            return 'PRESSEMITTEILUNG\n' +
                'Datum: ' + today + '\n' +
                'Zur sofortigen Ver\u00f6ffentlichung\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                p.name + ': ' + p.desc + '\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                'Wir freuen uns, die Verf\u00fcgbarkeit von ' + p.name + ' bekannt zu geben. ' + p.desc + '\n\n' +
                (p.features.length ?
                    'Hauptmerkmale von ' + p.name + ':\n\n' +
                    p.features.map(function (f) { return '  \u2022 ' + f; }).join('\n') + '\n\n' : '') +
                (p.audience ? '"' + p.name + ' wurde speziell f\u00fcr ' + p.audience + ' entwickelt", erkl\u00e4rt das Entwicklerteam.\n\n' : '') +
                (p.price ? 'Verf\u00fcgbarkeit & Preis:\n' + p.name + ' ist ab sofort zum Preis von ' + p.price + ' erh\u00e4ltlich.\n\n' : '') +
                (p.url ? 'Weitere Informationen finden Sie unter: ' + p.url + '\n\n' : '') +
                'Pressekontakt:\n' +
                'E-Mail: presse@' + p.name.toLowerCase().replace(/\s+/g, '') + '.de\n' +
                'Web: ' + (p.url || 'www.' + p.name.toLowerCase().replace(/\s+/g, '') + '.de') + '\n\n' +
                '###';
        },
        en: function (p) {
            var today = new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
            return 'PRESS RELEASE\n' +
                'Date: ' + today + '\n' +
                'For Immediate Release\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                p.name + ': ' + p.descEn + '\n\n' +
                '\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n\n' +
                'We are pleased to announce the availability of ' + p.name + '. ' + p.descEn + '\n\n' +
                (p.features.length ?
                    'Key Features of ' + p.name + ':\n\n' +
                    p.featuresEn.map(function (f) { return '  \u2022 ' + f; }).join('\n') + '\n\n' : '') +
                (p.audience ? '"' + p.name + ' was specifically designed for ' + p.audienceEn + '," says the development team.\n\n' : '') +
                (p.price ? 'Availability & Pricing:\n' + p.name + ' is available now at ' + p.price + '.\n\n' : '') +
                (p.url ? 'For more information, visit: ' + p.url + '\n\n' : '') +
                'Press Contact:\n' +
                'Email: press@' + p.name.toLowerCase().replace(/\s+/g, '') + '.com\n' +
                'Web: ' + (p.url || 'www.' + p.name.toLowerCase().replace(/\s+/g, '') + '.com') + '\n\n' +
                '###';
        }
    };

    // --- Category Mapping ---
    var categoryNames = {
        tech: { de: 'Technologie', en: 'Technology' },
        fashion: { de: 'Mode & Bekleidung', en: 'Fashion & Apparel' },
        food: { de: 'Lebensmittel & Getr\u00e4nke', en: 'Food & Beverages' },
        health: { de: 'Gesundheit & Wellness', en: 'Health & Wellness' },
        home: { de: 'Haus & Garten', en: 'Home & Garden' },
        sport: { de: 'Sport & Fitness', en: 'Sports & Fitness' },
        beauty: { de: 'Sch\u00f6nheit & Pflege', en: 'Beauty & Care' },
        education: { de: 'Bildung & Kurse', en: 'Education & Courses' },
        software: { de: 'Software & Apps', en: 'Software & Apps' },
        other: { de: 'Sonstiges', en: 'Other' }
    };

    // --- Gather Product Data ---
    function getProductData() {
        var name = document.getElementById('productName').value.trim();
        var desc = document.getElementById('productDescription').value.trim();
        var price = document.getElementById('productPrice').value.trim();
        var url = document.getElementById('productUrl').value.trim();
        var features = document.getElementById('productFeatures').value.trim();
        var audience = document.getElementById('targetAudience').value.trim();
        var category = document.getElementById('productCategory').value;

        if (!name || !desc) {
            showToast(currentLang === 'de' ? 'Bitte Produktname und Beschreibung eingeben!' : 'Please enter product name and description!');
            return null;
        }

        var featureList = features ? features.split(',').map(function (f) { return f.trim(); }).filter(Boolean) : [];
        var catInfo = categoryNames[category] || { de: '', en: '' };

        return {
            name: name,
            desc: desc,
            descEn: desc, // User provides in their language; used as-is
            price: price,
            url: url,
            features: featureList,
            featuresEn: featureList,
            audience: audience,
            audienceEn: audience,
            category: category,
            categoryDe: catInfo.de,
            categoryEn: catInfo.en,
            hashtags: generateHashtags(name, featureList, category, 'de'),
            hashtagsEn: generateHashtags(name, featureList, category, 'en')
        };
    }

    // --- Generate Hashtags ---
    function generateHashtags(name, features, category, lang) {
        var tags = [];
        tags.push('#' + name.replace(/\s+/g, ''));

        var catTags = {
            tech: { de: ['#Technologie', '#Innovation', '#TechNews', '#Digital', '#Gadget'], en: ['#Technology', '#Innovation', '#TechNews', '#Digital', '#Gadget'] },
            fashion: { de: ['#Mode', '#Fashion', '#Style', '#OOTD', '#Trend'], en: ['#Fashion', '#Style', '#OOTD', '#Trend', '#Outfit'] },
            food: { de: ['#Foodie', '#Lecker', '#Essen', '#Kochen', '#Genuss'], en: ['#Foodie', '#Delicious', '#FoodLover', '#Cooking', '#Yummy'] },
            health: { de: ['#Gesundheit', '#Wellness', '#Fitness', '#Wohlbefinden'], en: ['#Health', '#Wellness', '#Fitness', '#Wellbeing'] },
            home: { de: ['#Zuhause', '#Wohnen', '#Interior', '#HomeDecor'], en: ['#Home', '#Living', '#Interior', '#HomeDecor'] },
            sport: { de: ['#Sport', '#Fitness', '#Training', '#Motivation'], en: ['#Sports', '#Fitness', '#Training', '#Motivation'] },
            beauty: { de: ['#Beauty', '#Pflege', '#Skincare', '#Sch\u00f6nheit'], en: ['#Beauty', '#Skincare', '#SelfCare', '#Glow'] },
            education: { de: ['#Bildung', '#Lernen', '#Wissen', '#Weiterbildung'], en: ['#Education', '#Learning', '#Knowledge', '#Growth'] },
            software: { de: ['#Software', '#App', '#Digital', '#SaaS', '#Produktivit\u00e4t'], en: ['#Software', '#App', '#Digital', '#SaaS', '#Productivity'] },
            other: { de: ['#Neu', '#MustHave', '#Empfehlung'], en: ['#New', '#MustHave', '#Recommended'] }
        };

        var ct = catTags[category];
        if (ct) {
            tags = tags.concat(ct[lang] || ct.en);
        }

        features.slice(0, 2).forEach(function (f) {
            tags.push('#' + f.replace(/\s+/g, '').replace(/[^a-zA-Z0-9\u00c0-\u017e]/g, ''));
        });

        return tags.filter(function (t, i, arr) { return arr.indexOf(t) === i; });
    }

    // --- Generate Slogans ---
    function generateSlogans(p) {
        var slogans = [];

        var sloganTemplatesDe = [
            p.name + ' \u2013 Weil du das Beste verdienst.',
            p.name + '. Einfach. Besser. Anders.',
            'Die Zukunft hei\u00dft ' + p.name + '.',
            p.name + ' \u2013 Dein n\u00e4chster Schritt nach vorn.',
            'Erlebe den Unterschied mit ' + p.name + '.',
            p.name + '. Mehr als du erwartest.'
        ];

        var sloganTemplatesEn = [
            p.name + ' \u2013 Because you deserve the best.',
            p.name + '. Simple. Better. Different.',
            'The future is called ' + p.name + '.',
            p.name + ' \u2013 Your next step forward.',
            'Experience the difference with ' + p.name + '.',
            p.name + '. More than you expect.'
        ];

        for (var i = 0; i < sloganTemplatesDe.length; i++) {
            slogans.push({ de: sloganTemplatesDe[i], en: sloganTemplatesEn[i] });
        }

        return slogans;
    }

    // --- Generate Landing Page HTML ---
    function generateLandingPage(p) {
        var accentColor = '#6C5CE7';
        return '<!DOCTYPE html>\n' +
            '<html lang="de">\n<head>\n' +
            '  <meta charset="UTF-8">\n' +
            '  <meta name="viewport" content="width=device-width, initial-scale=1.0">\n' +
            '  <title>' + p.name + ' \u2013 ' + p.desc.substring(0, 60) + '</title>\n' +
            '  <meta name="description" content="' + p.desc + '">\n' +
            '  <meta property="og:title" content="' + p.name + '">\n' +
            '  <meta property="og:description" content="' + p.desc + '">\n' +
            '  <style>\n' +
            '    * { margin: 0; padding: 0; box-sizing: border-box; }\n' +
            '    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; color: #333; }\n' +
            '    .hero { background: linear-gradient(135deg, ' + accentColor + ', #00CEC9); color: #fff; padding: 80px 20px; text-align: center; }\n' +
            '    .hero h1 { font-size: 3rem; margin-bottom: 16px; }\n' +
            '    .hero p { font-size: 1.3rem; opacity: 0.9; max-width: 600px; margin: 0 auto 32px; }\n' +
            '    .cta-btn { display: inline-block; padding: 16px 40px; background: #fff; color: ' + accentColor + '; font-size: 18px; font-weight: 700; border-radius: 50px; text-decoration: none; transition: transform 0.3s; }\n' +
            '    .cta-btn:hover { transform: scale(1.05); }\n' +
            '    .features { padding: 60px 20px; max-width: 800px; margin: 0 auto; }\n' +
            '    .features h2 { text-align: center; font-size: 2rem; margin-bottom: 40px; }\n' +
            '    .feature-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 30px; }\n' +
            '    .feature { text-align: center; padding: 24px; }\n' +
            '    .feature h3 { color: ' + accentColor + '; margin-bottom: 8px; }\n' +
            (p.price ? '    .pricing { text-align: center; padding: 60px 20px; background: #f8f9fa; }\n' +
                '    .pricing h2 { font-size: 2rem; margin-bottom: 16px; }\n' +
                '    .price-tag { font-size: 3rem; font-weight: 900; color: ' + accentColor + '; }\n' : '') +
            '    .footer { text-align: center; padding: 30px; color: #888; font-size: 14px; }\n' +
            '  </style>\n' +
            '</head>\n<body>\n' +
            '  <section class="hero">\n' +
            '    <h1>' + p.name + '</h1>\n' +
            '    <p>' + p.desc + '</p>\n' +
            (p.url ? '    <a href="' + escapeHtml(p.url) + '" class="cta-btn">Jetzt entdecken / Discover Now</a>\n' : '    <a href="#features" class="cta-btn">Mehr erfahren / Learn More</a>\n') +
            '  </section>\n' +
            (p.features.length ? '  <section class="features" id="features">\n' +
                '    <h2>Features</h2>\n' +
                '    <div class="feature-grid">\n' +
                p.features.map(function (f) { return '      <div class="feature">\n        <h3>' + escapeHtml(f) + '</h3>\n      </div>'; }).join('\n') + '\n' +
                '    </div>\n' +
                '  </section>\n' : '') +
            (p.price ? '  <section class="pricing">\n' +
                '    <h2>Preis / Price</h2>\n' +
                '    <div class="price-tag">' + escapeHtml(p.price) + '</div>\n' +
                '  </section>\n' : '') +
            '  <footer class="footer">\n' +
            '    &copy; ' + new Date().getFullYear() + ' ' + escapeHtml(p.name) + '. All rights reserved.\n' +
            '  </footer>\n' +
            '</body>\n</html>';
    }

    function escapeHtml(str) {
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    // --- Main Generate Function ---
    function generateAll() {
        var p = getProductData();
        if (!p) return;

        var btn = document.getElementById('generateBtn');
        btn.classList.add('loading');

        setTimeout(function () {
            var style = templates[selectedStyle] || templates.professional;
            var platforms = ['twitter', 'instagram', 'facebook', 'linkedin', 'tiktok'];

            platforms.forEach(function (platform) {
                var tpl = style[platform];
                if (tpl) {
                    setText(platform + '-de', tpl.de(p));
                    setText(platform + '-en', tpl.en(p));
                }
            });

            // Email
            setText('email-de', emailTemplates.de(p));
            setText('email-en', emailTemplates.en(p));

            // SEO
            setText('seo-de', seoTemplates.de(p));
            setText('seo-en', seoTemplates.en(p));

            // Press Release
            setText('press-de', pressTemplates.de(p));
            setText('press-en', pressTemplates.en(p));

            // Slogans
            var slogans = generateSlogans(p);
            var sloganGrid = document.getElementById('sloganGrid');
            sloganGrid.innerHTML = '';
            slogans.forEach(function (s) {
                var div = document.createElement('div');
                div.className = 'slogan-item';
                div.innerHTML = '<span class="slogan-lang">DE</span>' + escapeHtml(s.de);
                div.onclick = function () { copyToClipboard(s.de); };
                sloganGrid.appendChild(div);

                var divEn = document.createElement('div');
                divEn.className = 'slogan-item';
                divEn.innerHTML = '<span class="slogan-lang">EN</span>' + escapeHtml(s.en);
                divEn.onclick = function () { copyToClipboard(s.en); };
                sloganGrid.appendChild(divEn);
            });

            // Hashtags
            var hashtagCloud = document.getElementById('hashtagCloud');
            hashtagCloud.innerHTML = '';
            var allTags = p.hashtags.concat(p.hashtagsEn).filter(function (t, i, arr) { return arr.indexOf(t) === i; });
            allTags.forEach(function (tag) {
                var span = document.createElement('span');
                span.className = 'hashtag';
                span.textContent = tag;
                span.onclick = function () { copyToClipboard(tag); };
                hashtagCloud.appendChild(span);
            });

            // Landing Page
            var landingHtml = generateLandingPage(p);
            document.getElementById('landing-code').textContent = landingHtml;
            var iframe = document.createElement('iframe');
            iframe.srcdoc = landingHtml;
            var previewDiv = document.getElementById('landing-preview');
            previewDiv.innerHTML = '';
            previewDiv.appendChild(iframe);

            // Store data for export
            generatedData = {
                product: p,
                style: selectedStyle,
                social: {},
                email: { de: emailTemplates.de(p), en: emailTemplates.en(p) },
                seo: { de: seoTemplates.de(p), en: seoTemplates.en(p) },
                press: { de: pressTemplates.de(p), en: pressTemplates.en(p) },
                slogans: slogans,
                hashtags: allTags,
                landingPage: landingHtml
            };

            platforms.forEach(function (platform) {
                var tpl = style[platform];
                if (tpl) {
                    generatedData.social[platform] = { de: tpl.de(p), en: tpl.en(p) };
                }
            });

            // Show results
            document.getElementById('results').classList.remove('hidden');
            document.getElementById('results').scrollIntoView({ behavior: 'smooth', block: 'start' });

            btn.classList.remove('loading');
            showToast(currentLang === 'de' ? 'Alle Werbematerialien wurden generiert!' : 'All promotion materials generated!');
        }, 600);
    }

    window.generateAll = generateAll;

    // --- Helper Functions ---
    function setText(id, text) {
        var el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    function copyToClipboard(text) {
        navigator.clipboard.writeText(text).then(function () {
            showToast(currentLang === 'de' ? 'Kopiert!' : 'Copied!');
        }).catch(function () {
            fallbackCopy(text);
        });
    }

    function fallbackCopy(text) {
        var textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        showToast(currentLang === 'de' ? 'Kopiert!' : 'Copied!');
    }

    function copyText(id) {
        var el = document.getElementById(id);
        if (el) copyToClipboard(el.textContent);
    }

    window.copyText = copyText;

    function copyAll(section) {
        if (!generatedData) return;
        var text = '';
        if (section === 'social') {
            Object.keys(generatedData.social).forEach(function (platform) {
                text += '=== ' + platform.toUpperCase() + ' (DE) ===\n' + generatedData.social[platform].de + '\n\n';
                text += '=== ' + platform.toUpperCase() + ' (EN) ===\n' + generatedData.social[platform].en + '\n\n';
            });
        } else if (section === 'email') {
            text = '=== EMAIL (DE) ===\n' + generatedData.email.de + '\n\n=== EMAIL (EN) ===\n' + generatedData.email.en;
        } else if (section === 'seo') {
            text = '=== SEO (DE) ===\n' + generatedData.seo.de + '\n\n=== SEO (EN) ===\n' + generatedData.seo.en;
        } else if (section === 'press') {
            text = '=== PRESS (DE) ===\n' + generatedData.press.de + '\n\n=== PRESS (EN) ===\n' + generatedData.press.en;
        }
        copyToClipboard(text);
    }

    window.copyAll = copyAll;

    function copyHashtags() {
        if (generatedData) copyToClipboard(generatedData.hashtags.join(' '));
    }

    window.copyHashtags = copyHashtags;

    // --- Tabs ---
    function switchTab(btn, tab) {
        var parent = btn.closest('.result-card');
        parent.querySelectorAll('.tab-btn').forEach(function (b) { b.classList.remove('active'); });
        btn.classList.add('active');
        parent.querySelectorAll('.tab-content').forEach(function (c) { c.classList.add('hidden'); });
        document.getElementById('tab-' + tab).classList.remove('hidden');
    }

    window.switchTab = switchTab;

    // --- Landing Page Preview Toggle ---
    function togglePreview(mode) {
        var preview = document.getElementById('landing-preview');
        var code = document.getElementById('landing-code');
        var buttons = document.querySelectorAll('.preview-btn');

        buttons.forEach(function (b) { b.classList.remove('active'); });

        if (mode === 'preview') {
            preview.classList.remove('hidden');
            code.classList.add('hidden');
            buttons[0].classList.add('active');
        } else {
            preview.classList.add('hidden');
            code.classList.remove('hidden');
            buttons[1].classList.add('active');
        }
    }

    window.togglePreview = togglePreview;

    // --- Export Functions ---
    function exportAs(format) {
        if (!generatedData) {
            showToast(currentLang === 'de' ? 'Bitte zuerst generieren!' : 'Please generate first!');
            return;
        }

        var content = '';
        var filename = 'promo-' + generatedData.product.name.replace(/\s+/g, '-').toLowerCase();
        var mimeType = 'text/plain';

        if (format === 'txt') {
            content = buildTextExport();
            filename += '.txt';
        } else if (format === 'html') {
            content = generatedData.landingPage;
            filename += '-landingpage.html';
            mimeType = 'text/html';
        } else if (format === 'json') {
            content = JSON.stringify(generatedData, null, 2);
            filename += '.json';
            mimeType = 'application/json';
        } else if (format === 'csv') {
            content = buildCsvExport();
            filename += '.csv';
            mimeType = 'text/csv';
        }

        downloadFile(content, filename, mimeType);
        showToast((currentLang === 'de' ? 'Export als ' : 'Exported as ') + format.toUpperCase() + '!');
    }

    window.exportAs = exportAs;

    function buildTextExport() {
        var d = generatedData;
        var lines = [];
        lines.push('========================================');
        lines.push('PROMOMASTER - WERBEMATERIALIEN / PROMOTION MATERIALS');
        lines.push('Produkt / Product: ' + d.product.name);
        lines.push('Erstellt am / Generated: ' + new Date().toLocaleString());
        lines.push('========================================\n');

        Object.keys(d.social).forEach(function (platform) {
            lines.push('\n--- ' + platform.toUpperCase() + ' (DE) ---');
            lines.push(d.social[platform].de);
            lines.push('\n--- ' + platform.toUpperCase() + ' (EN) ---');
            lines.push(d.social[platform].en);
        });

        lines.push('\n\n--- E-MAIL MARKETING (DE) ---');
        lines.push(d.email.de);
        lines.push('\n--- E-MAIL MARKETING (EN) ---');
        lines.push(d.email.en);

        lines.push('\n\n--- SEO (DE) ---');
        lines.push(d.seo.de);
        lines.push('\n--- SEO (EN) ---');
        lines.push(d.seo.en);

        lines.push('\n\n--- PRESSEMITTEILUNG / PRESS RELEASE (DE) ---');
        lines.push(d.press.de);
        lines.push('\n--- PRESS RELEASE (EN) ---');
        lines.push(d.press.en);

        lines.push('\n\n--- SLOGANS ---');
        d.slogans.forEach(function (s) {
            lines.push('DE: ' + s.de);
            lines.push('EN: ' + s.en);
        });

        lines.push('\n\n--- HASHTAGS ---');
        lines.push(d.hashtags.join(' '));

        return lines.join('\n');
    }

    function buildCsvExport() {
        var d = generatedData;
        var rows = [['Platform', 'Language', 'Content']];

        Object.keys(d.social).forEach(function (platform) {
            rows.push([platform, 'DE', '"' + d.social[platform].de.replace(/"/g, '""') + '"']);
            rows.push([platform, 'EN', '"' + d.social[platform].en.replace(/"/g, '""') + '"']);
        });

        rows.push(['email', 'DE', '"' + d.email.de.replace(/"/g, '""') + '"']);
        rows.push(['email', 'EN', '"' + d.email.en.replace(/"/g, '""') + '"']);
        rows.push(['seo', 'DE', '"' + d.seo.de.replace(/"/g, '""') + '"']);
        rows.push(['seo', 'EN', '"' + d.seo.en.replace(/"/g, '""') + '"']);
        rows.push(['press', 'DE', '"' + d.press.de.replace(/"/g, '""') + '"']);
        rows.push(['press', 'EN', '"' + d.press.en.replace(/"/g, '""') + '"']);

        d.slogans.forEach(function (s, i) {
            rows.push(['slogan_' + (i + 1), 'DE', '"' + s.de.replace(/"/g, '""') + '"']);
            rows.push(['slogan_' + (i + 1), 'EN', '"' + s.en.replace(/"/g, '""') + '"']);
        });

        return rows.map(function (r) { return r.join(','); }).join('\n');
    }

    function downloadFile(content, filename, mimeType) {
        var blob = new Blob([content], { type: mimeType + ';charset=utf-8' });
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }

    // --- Toast ---
    function showToast(msg) {
        var toast = document.getElementById('toast');
        toast.textContent = msg;
        toast.classList.remove('hidden');
        toast.classList.add('show');
        setTimeout(function () {
            toast.classList.remove('show');
            setTimeout(function () { toast.classList.add('hidden'); }, 400);
        }, 2500);
    }

    // --- Initialize ---
    setLanguage('de');
})();
