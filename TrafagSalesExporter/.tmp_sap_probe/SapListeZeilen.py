# SE16-Liste aus P76Lesen.vbs "dump" in Zeilen; mit Muster ueber mehrere Seiten (Doku: saptasks/BERECHTIGUNG_RAHMENVERTRAG_AT_2026-10-13.md).
import re, sys, collections
txt = open(sys.argv[1], encoding='cp1252', errors='replace').read()
pat = sys.argv[2] if len(sys.argv) > 2 else '.'
out = collections.OrderedDict()
for page in txt.split('TITEL=')[1:]:
    rows = collections.defaultdict(dict)
    for line in page.splitlines():
        m = re.search(r'/usr/(?:lbl|txt)\[(\d+),(\d+)\] = (.*)$', line)
        if m:
            rows[int(m.group(2))][int(m.group(1))] = m.group(3).strip()
    for r in sorted(rows):
        cells = [v for k, v in sorted(rows[r].items()) if v]
        if len(cells) > 6 and re.search(pat, ' '.join(cells)):
            key = cells[1] + ' ' + cells[4]
            out.setdefault(key, [])
            if cells[6] not in out[key]:
                out[key].append(cells[6])
for k, v in out.items():
    print(k, ':', ','.join(v))
