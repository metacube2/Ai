# Stellt p3_de.py auf Faktorgrenze 0,3 bis 8 um (Entscheid Ingo 24.11.) und schreibt die Kopie p3_de_f8.py
s = open('p3_de.py', encoding='utf-8').read()
reps = [
    ("    elif stimmt and not typ_other and aktiv and fak is not None and 0.3 <= fak <= 3:",
     "    elif stimmt and not typ_other and aktiv and fak is not None and 0.3 <= fak <= 8:"),
    ("        elif not (0.3 <= fak <= 3): gruende.append(f'Faktor {fak} ausserhalb 0,3-3')",
     "        elif not (0.3 <= fak <= 8): gruende.append(f'Faktor {fak} ausserhalb 0,3-8')"),
    ("Faktor lokale Kosten / (STPRS/PEINH) zwischen 0,3 und 3.'",
     "Faktor lokale Kosten / (STPRS/PEINH) zwischen 0,3 und 8 (Faktor 0,3 bis 8, Entscheid Ingo 24.11.).'"),
    ("'2 pruefen: alles andere (z. B. nur Textaehnlichkeit, Nummer nur im Text, Faktor 0,1-0,3 oder 3-10, keine lokalen Kosten).'",
     "'2 pruefen: alles andere (z. B. nur Textaehnlichkeit, Nummer nur im Text, Faktor 0,1-0,3 oder 8-10, keine lokalen Kosten).'"),
    ("'Hinweis Faktor: DE kauft bei TR AG zum Konzernpreis; der Median liegt bei rund 3,9. Mit Grenze 3 statt z. B. 8 landen viele sonst stimmige Vorschlaege in pruefen (Spalte Faktor ansehen).'",
     "'Faktor 0,3 bis 8, Entscheid Ingo 24.11. (vorher 0,3 bis 3): DE kauft bei TR AG zum Konzernpreis, der Median liegt bei rund 3,9.'"),
]
for a, b in reps:
    assert a in s, a[:60]
    s = s.replace(a, b)
open('p3_de_f8.py', 'w', encoding='utf-8').write(s)
print('ok')
