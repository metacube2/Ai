#!/bin/zsh
# Spielt Ai/gym/public nach cAiscom (LXC 104) /var/www/gym ein.
# Sichert einen vorhandenen Stand nach /root/gym-backup-<zeit>.
set -euo pipefail
cd "${0:A:h}/.."
STAMP=$(date +%Y%m%d-%H%M%S)
DATEIEN=(index.html app.js style.css sw.js api.php manifest.webmanifest icon.svg icon-192.png icon-512.png apple-touch-icon.png)

tar czf - -C public $DATEIEN -C ../deploy gym.conf | ssh -o BatchMode=yes root@192.168.178.161 \
  "cat > /tmp/gym.tgz && pct push 104 /tmp/gym.tgz /root/gym-$STAMP.tgz && rm /tmp/gym.tgz && pct exec 104 -- bash -c '
    set -e
    [ -d /var/www/gym ] && cp -a /var/www/gym /root/gym-backup-$STAMP
    mkdir -p /var/www/gym /var/lib/gym /root/gym-neu-$STAMP
    tar xzf /root/gym-$STAMP.tgz -C /root/gym-neu-$STAMP
    install -m 644 /root/gym-neu-$STAMP/gym.conf /etc/apache2/sites-available/gym.conf
    rm /root/gym-neu-$STAMP/gym.conf
    cp /root/gym-neu-$STAMP/* /var/www/gym/
    chown -R root:root /var/www/gym && chmod 755 /var/www/gym && chmod 644 /var/www/gym/*
    chown www-data:www-data /var/lib/gym && chmod 750 /var/lib/gym
    a2enmod -q rewrite headers
    a2ensite -q gym
    apache2ctl configtest && systemctl reload apache2
  '"
echo "eingespielt: $STAMP"
