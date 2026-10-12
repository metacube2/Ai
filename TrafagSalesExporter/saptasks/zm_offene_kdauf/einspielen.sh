#!/bin/sh
# Spielt die genannten Programme aus diesem Ordner in T76 ein (Sitzung 1, SE38), sichert und
# aktiviert sie einzeln. Aufruf aus der Repository-Wurzel:
#   sh saptasks/zm_offene_kdauf/einspielen.sh REF OPT CMP
export MSYS_NO_PATHCONV=1
P=.tmp_sap_probe
W=$(cygpath -w "$PWD/saptasks/zm_offene_kdauf")
for t in "$@"; do
  prog="ZM_OFFENE_KDAUF_$t"
  echo "== $prog"
  timeout 120 cscript.exe //nologo $P/SapGuiSetReportSource.vbs "$prog" "$W\\$prog.abap" | grep -E "SICHERN|DIALOG"
  timeout 120 cscript.exe //nologo $P/SapGuiInaktiveLokalAktivieren.vbs 1 "$prog" | grep -E "MELDUNG|ABBRUCH|wnd"
done
