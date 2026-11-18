#!/bin/bash
# Legt bier.ingok.ch als NPM-Proxy-Host mit eigenem Let's-Encrypt-Zertifikat an,
# Ziel cAiscom 192.168.178.146:80. Muster: Ai/gym/deploy/npm_host_42.sh, die IDs
# werden hier aber frei aus der NPM-Datenbank bestimmt.
# ERST AUSFUEHREN, WENN DER CNAME bier.ingok.ch -> aiscom.duckdns.org BEI HOSTTECH STEHT
# (sonst scheitert die Webroot-Pruefung von certbot). Auf cNginx (LXC 111):
#   ssh root@192.168.178.161 'pct exec 111 -- bash -s' < npm_host.sh
set -euo pipefail
NAME=bier.ingok.ch
D=/var/lib/docker/volumes/nginx_data/_data
C=nginx_proxy_manager-app-1
T=$(date +%Y%m%d%H%M%S)

if [ "$(dig +short @1.1.1.1 aiscom.duckdns.org A | tail -1)" != "$(dig +short @1.1.1.1 $NAME A | tail -1)" ]; then
  echo "DNS fuer $NAME zeigt noch nicht auf aiscom.duckdns.org - abgebrochen." >&2
  exit 1
fi

read -r HOST CERT < <(python3 - <<PY
import sqlite3
c = sqlite3.connect("$D/database.sqlite")
assert c.execute("select count(*) from proxy_host where domain_names like '%$NAME%' and is_deleted=0").fetchone()[0] == 0, "Host existiert schon"
print(c.execute("select max(id)+1 from proxy_host").fetchone()[0], c.execute("select max(id)+1 from certificate").fetchone()[0])
PY
)
VORLAGE=$(python3 -c "import sqlite3;print(sqlite3.connect('$D/database.sqlite').execute(\"select id from proxy_host where domain_names like '%gym.ingok.ch%'\").fetchone()[0])")
VCERT=$(python3 -c "import sqlite3;print(sqlite3.connect('$D/database.sqlite').execute('select certificate_id from proxy_host where id=$VORLAGE').fetchone()[0])")
echo "neuer Host $HOST, Zertifikat $CERT (Vorlage Host $VORLAGE / npm-$VCERT)"

docker exec $C certbot certonly --config-dir /etc/letsencrypt --work-dir /tmp/letsencrypt-lib \
  --logs-dir /tmp/letsencrypt-log --cert-name npm-$CERT --agree-tos --non-interactive \
  --account 21865b8b913a7c58c8a3dfb187182702 --key-type ecdsa --elliptic-curve secp384r1 \
  --preferred-chain "ISRG Root X1" --authenticator webroot -w /data/letsencrypt-acme-challenge \
  -d $NAME

cp -a $D/database.sqlite $D/database.sqlite.bak_bier_$T
python3 - <<PY
import sqlite3, json, subprocess
from datetime import datetime
c = sqlite3.connect("$D/database.sqlite")
ende = subprocess.run(["docker", "exec", "$C", "openssl", "x509", "-in",
                       "/etc/letsencrypt/live/npm-$CERT/fullchain.pem", "-noout", "-enddate"],
                      capture_output=True, text=True, check=True).stdout.strip().split("=", 1)[1]
ende = datetime.strptime(ende, "%b %d %H:%M:%S %Y %Z").strftime("%Y-%m-%d %H:%M:%S")
meta = json.dumps({"letsencrypt_agree": True, "dns_challenge": False, "nginx_online": True,
                   "nginx_err": None, "letsencrypt_email": "metacube@gmail.com"})
jetzt = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
c.execute("insert into certificate (id,created_on,modified_on,owner_user_id,is_deleted,provider,nice_name,"
          "domain_names,expires_on,meta) values (?,?,?,1,0,'letsencrypt','$NAME','[\"$NAME\"]',?,?)",
          ($CERT, jetzt, jetzt, ende, meta))
c.execute("insert into proxy_host (id,created_on,modified_on,owner_user_id,is_deleted,domain_names,forward_host,"
          "forward_port,access_list_id,certificate_id,ssl_forced,caching_enabled,block_exploits,advanced_config,"
          "meta,allow_websocket_upgrade,http2_support,forward_scheme,enabled,locations,hsts_enabled,hsts_subdomains) "
          "values (?,?,?,1,0,'[\"$NAME\"]','192.168.178.146',80,0,?,1,0,1,'',?,0,1,'http',1,'[]',1,0)",
          ($HOST, jetzt, jetzt, $CERT, meta))
c.commit()
print("NPM-Datenbank: Zertifikat $CERT und Host $HOST eingetragen, Ablauf", ende)
PY

sed -e "s/gym\.ingok\.ch/$NAME/g" -e "s/npm-$VCERT/npm-$CERT/g" -e "s/proxy-host-${VORLAGE}_/proxy-host-${HOST}_/g" \
    $D/nginx/proxy_host/$VORLAGE.conf > $D/nginx/proxy_host/$HOST.conf
if docker exec $C nginx -t; then
    docker exec $C nginx -s reload
    echo "Fertig: https://$NAME ist eingetragen (Host $HOST)."
else
    echo "nginx -t fehlgeschlagen - nehme die Aenderung zurueck." >&2
    rm -f $D/nginx/proxy_host/$HOST.conf
    cp -a $D/database.sqlite.bak_bier_$T $D/database.sqlite
    exit 1
fi
