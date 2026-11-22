#!/bin/bash
# Legt bildervon.ingok.ch als NPM Proxy-Host 41 mit Zertifikat 87 (npm-87, schon ausgestellt) an.
# Ausfuehren auf cNginx (LXC 111):  ssh root@192.168.178.161 'pct exec 111 -- bash -s' < npm_host_41.sh
set -euo pipefail
D=/var/lib/docker/volumes/nginx_data/_data
T=$(date +%Y%m%d%H%M%S)
cp -a $D/database.sqlite $D/database.sqlite.bak_bildervon_host_$T
python3 - <<'PY'
import sqlite3, json, subprocess
from datetime import datetime
D = "/var/lib/docker/volumes/nginx_data/_data"
c = sqlite3.connect(f"{D}/database.sqlite")
ende = subprocess.run(["docker", "exec", "nginx_proxy_manager-app-1", "openssl", "x509", "-in",
                       "/etc/letsencrypt/live/npm-87/fullchain.pem", "-noout", "-enddate"],
                      capture_output=True, text=True, check=True).stdout.strip().split("=", 1)[1]
ende = datetime.strptime(ende, "%b %d %H:%M:%S %Y %Z").strftime("%Y-%m-%d %H:%M:%S")
meta = json.dumps({"letsencrypt_agree": True, "dns_challenge": False, "nginx_online": True,
                   "nginx_err": None, "letsencrypt_email": "metacube@gmail.com"})
jetzt = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
assert c.execute("select count(*) from certificate where id=87").fetchone()[0] == 0, "Zertifikat 87 existiert"
assert c.execute("select count(*) from proxy_host where id=41").fetchone()[0] == 0, "Host 41 existiert"
c.execute("insert into certificate (id,created_on,modified_on,owner_user_id,is_deleted,provider,nice_name,"
          "domain_names,expires_on,meta) values (87,?,?,1,0,'letsencrypt','bildervon.ingok.ch',"
          "'[\"bildervon.ingok.ch\"]',?,?)", (jetzt, jetzt, ende, meta))
c.execute("insert into proxy_host (id,created_on,modified_on,owner_user_id,is_deleted,domain_names,forward_host,"
          "forward_port,access_list_id,certificate_id,ssl_forced,caching_enabled,block_exploits,advanced_config,"
          "meta,allow_websocket_upgrade,http2_support,forward_scheme,enabled,locations,hsts_enabled,hsts_subdomains) "
          "values (41,?,?,1,0,'[\"bildervon.ingok.ch\"]','192.168.178.113',8095,0,87,1,0,1,'',?,0,1,'http',1,'[]',1,0)",
          (jetzt, jetzt, meta))
c.commit()
print("NPM-Datenbank: Zertifikat 87 und Host 41 eingetragen, Ablauf", ende)
PY
# Konfiguration nach dem Muster von Host 39 (braindump: HTTP/2, HSTS, Force SSL), ohne WebSocket/Extras
sed -e 's/braindump\.ingok\.ch/bildervon.ingok.ch/g' -e 's/"192\.168\.178\.106"/"192.168.178.113"/' \
    -e 's/set \$port           8013;/set $port           8095;/' -e 's/npm-84/npm-87/g' -e 's/proxy-host-39_/proxy-host-41_/g' \
    -e '/^ *proxy_set_header Upgrade/d' -e '/^ *proxy_set_header Connection/d' -e '/^ *proxy_http_version/d' \
    -e '/^client_max_body_size 50m;/d' -e '/^proxy_read_timeout 1800s;/d' -e '/^proxy_send_timeout 1800s;/d' \
    $D/nginx/proxy_host/39.conf > $D/nginx/proxy_host/41.conf
if docker exec nginx_proxy_manager-app-1 nginx -t; then
    docker exec nginx_proxy_manager-app-1 nginx -s reload
    echo "Fertig: https://bildervon.ingok.ch ist eingetragen."
else
    echo "nginx -t fehlgeschlagen - nehme die Aenderung zurueck." >&2
    rm -f $D/nginx/proxy_host/41.conf
    cp -a $D/database.sqlite.bak_bildervon_host_$T $D/database.sqlite
    exit 1
fi
