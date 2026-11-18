#!/bin/bash
# Legt kompass.aiscom.ch als NPM Proxy-Host 45 mit Zertifikat 91 (npm-91) an (2026-10-17).
# Ziel: Kompass auf cDocker 192.168.178.113:8096. Die Anmeldung macht die App selbst.
# Ausfuehren auf cNginx (LXC 111):  ssh root@192.168.178.161 'pct exec 111 -- bash -s' < deploy/npm_host_45.sh
set -euo pipefail
D=/var/lib/docker/volumes/nginx_data/_data
C=nginx_proxy_manager-app-1
T=$(date +%Y%m%d%H%M%S)

# 1. Zertifikat mit NPMs eigenen Parametern (wie npm-89), damit NPM es selbst erneuert
if ! docker exec $C test -d /etc/letsencrypt/live/npm-91; then
  docker exec $C certbot certonly --non-interactive --config-dir /etc/letsencrypt --work-dir /tmp/letsencrypt-lib \
    --logs-dir /tmp/letsencrypt-log --cert-name npm-91 --agree-tos --email metacube@gmail.com \
    --authenticator webroot --webroot-path /data/letsencrypt-acme-challenge --preferred-chain "ISRG Root X1" \
    --key-type ecdsa --elliptic-curve secp384r1 -d kompass.aiscom.ch
fi

# 2. Datenbank (sonst kennt die NPM-Oberflaeche den Host nicht)
cp -a $D/database.sqlite $D/database.sqlite.bak_kompass_$T
python3 - <<'PYNPM'
import sqlite3, json, subprocess
from datetime import datetime
D = "/var/lib/docker/volumes/nginx_data/_data"
c = sqlite3.connect(f"{D}/database.sqlite")
ende = subprocess.run(["docker", "exec", "nginx_proxy_manager-app-1", "openssl", "x509", "-in",
                       "/etc/letsencrypt/live/npm-91/fullchain.pem", "-noout", "-enddate"],
                      capture_output=True, text=True, check=True).stdout.strip().split("=", 1)[1]
ende = datetime.strptime(ende, "%b %d %H:%M:%S %Y %Z").strftime("%Y-%m-%d %H:%M:%S")
meta = json.dumps({"letsencrypt_agree": True, "dns_challenge": False, "nginx_online": True,
                   "nginx_err": None, "letsencrypt_email": "metacube@gmail.com"})
erweitert = ""
jetzt = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
assert c.execute("select count(*) from certificate where id=91").fetchone()[0] == 0, "Zertifikat 91 existiert"
assert c.execute("select count(*) from proxy_host where id=45").fetchone()[0] == 0, "Host 45 existiert"
c.execute("insert into certificate (id,created_on,modified_on,owner_user_id,is_deleted,provider,nice_name,"
          "domain_names,expires_on,meta) values (91,?,?,1,0,'letsencrypt','kompass.aiscom.ch',"
          "'[\"kompass.aiscom.ch\"]',?,?)", (jetzt, jetzt, ende, meta))
c.execute("insert into proxy_host (id,created_on,modified_on,owner_user_id,is_deleted,domain_names,forward_host,"
          "forward_port,access_list_id,certificate_id,ssl_forced,caching_enabled,block_exploits,advanced_config,"
          "meta,allow_websocket_upgrade,http2_support,forward_scheme,enabled,locations,hsts_enabled,hsts_subdomains) "
          "values (45,?,?,1,0,'[\"kompass.aiscom.ch\"]','192.168.178.113',8096,0,91,1,0,1,?,?,0,1,'http',1,'[]',1,0)",
          (jetzt, jetzt, erweitert, meta))
c.commit()
print("NPM-Datenbank: Zertifikat 91 und Host 45 eingetragen, Ablauf", ende)
PYNPM

# 3. Konfiguration nach dem Muster von Host 43 (Force SSL, HTTP/2, HSTS, Block Exploits)
sed -e 's/bier\.ingok\.ch/kompass.aiscom.ch/g' -e 's/"192\.168\.178\.146"/"192.168.178.113"/' \
    -e 's/set \$port           80;/set $port           8096;/' -e 's/npm-89/npm-91/g' -e 's/proxy-host-43_/proxy-host-45_/g' \
    $D/nginx/proxy_host/43.conf > $D/nginx/proxy_host/45.conf
if docker exec $C nginx -t; then
  docker exec $C nginx -s reload
  echo "Fertig: https://kompass.aiscom.ch ist eingetragen."
else
  echo "nginx -t fehlgeschlagen - nehme die Aenderung zurueck." >&2
  rm -f $D/nginx/proxy_host/45.conf
  cp -a $D/database.sqlite.bak_kompass_$T $D/database.sqlite
  exit 1
fi
