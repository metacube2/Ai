#!/bin/zsh
# Spielt Ai/funkerbier/public nach cAiscom (LXC 104) /var/www/bier ein.
# Sichert einen vorhandenen Stand nach /root/bier-backup-<zeit>.
# Einmalig vorher (siehe README): PHPMailer in /opt/bier, Mailzugang /etc/bier/mail.json.
set -euo pipefail
cd "${0:A:h}/.."
STAMP=$(date +%Y%m%d-%H%M%S)
DATEIEN=(index.html app.js style.css dmd.js fx.js audio.js mod-worklet.js funkerbier.mod api.php icon.svg)

tar czf - -C public $DATEIEN -C ../deploy bier.conf | ssh -o BatchMode=yes root@192.168.178.161 \
  "cat > /tmp/bier.tgz && pct push 104 /tmp/bier.tgz /root/bier-$STAMP.tgz && rm /tmp/bier.tgz && pct exec 104 -- bash -c '
    set -e
    [ -d /var/www/bier ] && cp -a /var/www/bier /root/bier-backup-$STAMP
    mkdir -p /var/www/bier /var/lib/bier /root/bier-neu-$STAMP
    tar xzf /root/bier-$STAMP.tgz -C /root/bier-neu-$STAMP
    php -l /root/bier-neu-$STAMP/api.php
    install -m 644 /root/bier-neu-$STAMP/bier.conf /etc/apache2/sites-available/bier.conf
    rm /root/bier-neu-$STAMP/bier.conf
    cp /root/bier-neu-$STAMP/* /var/www/bier/
    rm -r /root/bier-neu-$STAMP
    chown -R root:root /var/www/bier && chmod 755 /var/www/bier && chmod 644 /var/www/bier/*
    chown www-data:www-data /var/lib/bier && chmod 750 /var/lib/bier
    a2enmod -q rewrite headers
    a2ensite -q bier
    apache2ctl configtest && systemctl reload apache2
  '"
echo "eingespielt: $STAMP"
