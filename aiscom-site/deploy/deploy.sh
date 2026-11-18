#!/bin/zsh
# Bringt die alte AISCOM-Seite (Next.js) auf cAiscom (LXC 104) zurueck,
# baut den Produkte-Bereich ein und legt /kamera-uploader/ an.
# Aufruf auf dem Mac:  ./deploy.sh
# Rollback: siehe README.md im Ordner darueber.
set -e
cd "${0:A:h}"
PVE=${PVE_HOST:?PVE_HOST setzen, z.B. root@<proxmox-host>}
STAMP=$(date +%Y%m%d-%H%M%S)

scp -q patch_produkte.py aiscom-next.service aiscom-next-proxy.conf sitemap.xml robots.txt \
    kamera-uploader/index.html "$PVE:/tmp/"
ssh "$PVE" "set -e
pct push 104 /tmp/patch_produkte.py /root/patch_produkte.py
pct push 104 /tmp/aiscom-next.service /etc/systemd/system/aiscom-next.service
pct push 104 /tmp/aiscom-next-proxy.conf /etc/apache2/conf-available/aiscom-next-proxy.conf
pct exec 104 -- mkdir -p /var/www/html/kamera-uploader
pct push 104 /tmp/index.html /var/www/html/kamera-uploader/index.html
pct exec 104 -- cp -a /var/www/html/sitemap.xml /root/sitemap.xml.bak-$STAMP
pct exec 104 -- cp -a /var/www/html/robots.txt /root/robots.txt.bak-$STAMP
pct push 104 /tmp/sitemap.xml /var/www/html/sitemap.xml
pct push 104 /tmp/robots.txt /var/www/html/robots.txt
"

ssh "$PVE" 'pct exec 104 -- bash -s' <<REMOTE
set -e
cd /var/www/html/aiscom/frontend
# Sicherung von Quelle und Build (einmal je Lauf)
mkdir -p /root/aiscom-src-backup-$STAMP
cp -a locales app components /root/aiscom-src-backup-$STAMP/
cp -a .next /root/aiscom-next-backup-$STAMP
python3 /root/patch_produkte.py
npm run build 2>&1 | tail -25
# Laufzeit als www-data: DB und Upload-Ordner muessen schreibbar sein
chown -R www-data:www-data ../database public/uploads .next
systemctl daemon-reload
systemctl enable --now aiscom-next
sleep 6
curl -sf http://127.0.0.1:3000/de | grep -q 'Projekte im Verkauf'
echo "Next laeuft mit Produkte-Bereich"
a2enmod -q proxy proxy_http
a2enconf -q aiscom-next-proxy
apache2ctl configtest
systemctl reload apache2
find / -xdev -name aiscom.db 2>/dev/null
REMOTE
echo "Deploy fertig - jetzt ./pruefen.sh"
