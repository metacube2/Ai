#!/bin/bash
# Spielt die Nextvision-Zeitreise nach www.aiscom.ch/nextvision/ ein (LXC 104 cAiscom).
# Ruffle (Flash-Emulator) liegt nicht im Repo; es wird mit fester Version und Prüfsumme von npm geholt.
set -euo pipefail
cd "$(dirname "$0")"
RUFFLE_VER=0.7.1
RUFFLE_SHA512="6ZWxx/cfX3RoNyQ1AVimECSae4gKqMxdPimOD+chiIcOFiJS0ATGKcjkretxNT03lqNUpiSyJjF55AuvvZ+QeA=="
if [ ! -f site/ruffle/ruffle.js ]; then
  tmp=$(mktemp -d)
  curl -sfL -o "$tmp/r.tgz" "https://registry.npmjs.org/@ruffle-rs/ruffle/-/ruffle-$RUFFLE_VER.tgz"
  [ "$(openssl dgst -sha512 -binary "$tmp/r.tgz" | base64)" = "$RUFFLE_SHA512" ] || { echo "Ruffle-Pruefsumme falsch"; exit 1; }
  tar xzf "$tmp/r.tgz" -C "$tmp"
  mkdir -p site/ruffle
  cp "$tmp"/package/*.wasm "$tmp"/package/core.ruffle.*.js "$tmp"/package/ruffle.js "$tmp"/package/LICENSE_* site/ruffle/
  rm -rf "$tmp"
fi
STAMP=$(date +%Y%m%d-%H%M%S)
COPYFILE_DISABLE=1 tar czf - -C site . | ssh root@192.168.178.161 "cat > /tmp/nv.tgz && pct push 104 /tmp/nv.tgz /root/nextvision-$STAMP.tgz && rm /tmp/nv.tgz && pct exec 104 -- bash -c '
  set -e
  if [ -d /var/www/html/nextvision ]; then cp -a /var/www/html/nextvision /root/nextvision-backup-$STAMP; fi
  mkdir -p /var/www/html/nextvision.neu && tar xzf /root/nextvision-$STAMP.tgz -C /var/www/html/nextvision.neu
  chown -R www-data:www-data /var/www/html/nextvision.neu
  find /var/www/html/nextvision.neu -type d -exec chmod 755 {} + && find /var/www/html/nextvision.neu -type f -exec chmod 644 {} +
  rm -rf /var/www/html/nextvision && mv /var/www/html/nextvision.neu /var/www/html/nextvision'"
echo "eingespielt ($STAMP)"
