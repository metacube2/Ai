#!/bin/bash
# Legt gym.ingok.ch als NPM Proxy-Host 42 mit Zertifikat 88 (npm-88) an, Ziel cAiscom 192.168.178.146:80.
# Muster: Ai/bildervon/npm_host_41.sh. Ausfuehren auf cNginx (LXC 111):
#   ssh root@192.168.178.161 'pct exec 111 -- bash -s' < npm_host_42.sh
set -euo pipefail
D=/var/lib/docker/volumes/nginx_data/_data
C=nginx_proxy_manager-app-1
T=$(date +%Y%m%d%H%M%S)

# 1. Zertifikat mit NPMs eigenen Parametern, damit NPM es selbst erneuert
if ! docker exec $C test -f /etc/letsencrypt/live/npm-88/fullchain.pem; then
  docker exec $C certbot certonly --config-dir /etc/letsencrypt --work-dir /tmp/letsencrypt-lib \
    --logs-dir /tmp/letsencrypt-log --cert-name npm-88 --agree-tos --non-interactive \
    --account 21865b8b913a7c58c8a3dfb187182702 --key-type ecdsa --elliptic-curve secp384r1 \
    --preferred-chain "ISRG Root X1" --authenticator webroot -w /data/letsencrypt-acme-challenge \
    -d gym.ingok.ch
fi

# 2. Datenbank
cp -a $D/database.sqlite $D/database.sqlite.bak_gym_$T
python3 - <<'PY'
import sqlite3, json, subprocess
from datetime import datetime
D = "/var/lib/docker/volumes/nginx_data/_data"
c = sqlite3.connect(f"{D}/database.sqlite")
ende = subprocess.run(["docker", "exec", "nginx_proxy_manager-app-1", "openssl", "x509", "-in",
                       "/etc/letsencrypt/live/npm-88/fullchain.pem", "-noout", "-enddate"],
                      capture_output=True, text=True, check=True).stdout.strip().split("=", 1)[1]
ende = datetime.strptime(ende, "%b %d %H:%M:%S %Y %Z").strftime("%Y-%m-%d %H:%M:%S")
meta = json.dumps({"letsencrypt_agree": True, "dns_challenge": False, "nginx_online": True,
                   "nginx_err": None, "letsencrypt_email": "metacube@gmail.com"})
jetzt = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
assert c.execute("select count(*) from certificate where id=88").fetchone()[0] == 0, "Zertifikat 88 existiert"
assert c.execute("select count(*) from proxy_host where id=42").fetchone()[0] == 0, "Host 42 existiert"
c.execute("insert into certificate (id,created_on,modified_on,owner_user_id,is_deleted,provider,nice_name,"
          "domain_names,expires_on,meta) values (88,?,?,1,0,'letsencrypt','gym.ingok.ch',"
          "'[\"gym.ingok.ch\"]',?,?)", (jetzt, jetzt, ende, meta))
c.execute("insert into proxy_host (id,created_on,modified_on,owner_user_id,is_deleted,domain_names,forward_host,"
          "forward_port,access_list_id,certificate_id,ssl_forced,caching_enabled,block_exploits,advanced_config,"
          "meta,allow_websocket_upgrade,http2_support,forward_scheme,enabled,locations,hsts_enabled,hsts_subdomains) "
          "values (42,?,?,1,0,'[\"gym.ingok.ch\"]','192.168.178.146',80,0,88,1,0,1,'',?,0,1,'http',1,'[]',1,0)",
          (jetzt, jetzt, meta))
c.commit()
print("NPM-Datenbank: Zertifikat 88 und Host 42 eingetragen, Ablauf", ende)
PY

# 3. Konfiguration aus Host 41 ableiten
sed -e 's/bildervon\.ingok\.ch/gym.ingok.ch/g' -e 's/"192\.168\.178\.113"/"192.168.178.146"/' \
    -e 's/set \$port           8095;/set $port           80;/' -e 's/npm-87/npm-88/g' -e 's/proxy-host-41_/proxy-host-42_/g' \
    $D/nginx/proxy_host/41.conf > $D/nginx/proxy_host/42.conf
if docker exec $C nginx -t; then
    docker exec $C nginx -s reload
    echo "Fertig: https://gym.ingok.ch ist eingetragen."
else
    echo "nginx -t fehlgeschlagen - nehme die Aenderung zurueck." >&2
    rm -f $D/nginx/proxy_host/42.conf
    cp -a $D/database.sqlite.bak_gym_$T $D/database.sqlite
    exit 1
fi
