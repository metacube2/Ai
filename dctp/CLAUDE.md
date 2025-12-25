# DCTP - Delta Code Transfer Protocol

Du generierst Code im DCTP-Format fuer effiziente Uebertragung.

## Regeln

1. **Zeilennummern am Ende jeder Zeile** im passenden Kommentar-Format
2. **Immer mit ###FILE: beginnen** bei jedem Codeblock
3. **Bei Korrekturen NUR die geaenderten Zeilen senden**, nie den ganzen File

## Zeilennummern-Format

- Python/Shell: `code  #Z1`
- JavaScript/Java/C/C++: `code  //Z1`
- HTML: `code  <!--Z1-->`
- CSS: `code  /*Z1*/`
- SQL: `code  --Z1`

## Befehle

| Befehl | Syntax | Beschreibung |
|--------|--------|--------------|
| `###FILE:` | `###FILE:pfad/datei.ext` | Datei angeben |
| `###NEW` | | Neue Datei, kompletter Inhalt folgt |
| `###DELETE:` | `###DELETE:Z5-Z12` | Zeilen 5-12 loeschen |
| `###INSERT_AFTER:` | `###INSERT_AFTER:Z5` | Nach Zeile 5 einfuegen |
| `###REPLACE:` | `###REPLACE:Z5-Z8` | Zeilen 5-8 ersetzen |
| `###END` | | Ende des Blocks |
| `###RENUMBER` | | Zeilennummern neu berechnen |
| `###CHECKSUM:` | `###CHECKSUM:a3f2b8c1` | Optional: Hash zur Validierung |

## Beispiel: Neue Datei

```
###FILE:src/calculator.py
###NEW
def add(a, b):  #Z1
    return a + b  #Z2
  #Z3
def multiply(a, b):  #Z4
    return a * b  #Z5
###END
```

## Beispiel: Korrektur (REPLACE)

```
###FILE:src/calculator.py
###REPLACE:Z4-Z5
def multiply(a, b):  #Z4
    """Multipliziert zwei Zahlen."""  #Z5
    return a * b  #Z6
###END
###RENUMBER
```

## Beispiel: Zeilen einfuegen

```
###FILE:src/calculator.py
###INSERT_AFTER:Z2
  #Z3
def subtract(a, b):  #Z4
    return a - b  #Z5
###END
###RENUMBER
```

## Beispiel: Zeilen loeschen

```
###FILE:src/calculator.py
###DELETE:Z10-Z15
###RENUMBER
```

## Beispiel: Mehrere Dateien

```
###FILE:src/models/user.py
###NEW
class User:  #Z1
    def __init__(self, name: str):  #Z2
        self.name = name  #Z3
###END

###FILE:src/models/order.py
###NEW
from .user import User  #Z1
  #Z2
class Order:  #Z3
    def __init__(self, user: User):  #Z4
        self.user = user  #Z5
###END
```

## Wichtig

- Bei Korrekturen: NUR Delta senden, nie kompletten File
- Nach INSERT/DELETE/REPLACE immer ###RENUMBER
- Leerzeilen auch nummerieren
- Zeilennummern werden beim Schreiben automatisch entfernt
