# GitPusher Docker Deployment

## Zielhost

Für das Homelab ist `cDocker` / `pDocker` vorgesehen:

- LXC: `100`
- IP: `192.168.178.113`
- Proxmox-Host: `pvenuc`
- Proxmox-IP: `192.168.178.161`
- Proxmox-User: `root`
- Proxmox-Passwort: nicht im Repository ablegen; im Passwortmanager hinterlegen.
- empfohlener interner Port: `8088`
- Nginx Proxy Manager kann später auf `192.168.178.113:8088` zeigen.

## Deployment auf cDocker

Auf dem Docker-Host:

```bash
mkdir -p /data/compose/gitpusher
cd /data/compose/gitpusher
```

Repo-Dateien dort ablegen und starten:

```bash
docker compose up -d --build
```

Danach intern erreichbar:

```text
http://192.168.178.113:8088
```

## Weitere Dienste auf cDocker / pDocker

- Immich: `http://192.168.178.113:8005`
- PhotoPrism: `http://192.168.178.113:8002`
- Portainer: `http://192.168.178.113:9000`
- Dockge: `http://192.168.178.113:5001`

## Persistente Daten

Das Compose-File legt zwei Docker-Volumes an:

- `gitpusher_gitpusher-data` fuer `config.json`, `log.json`, `secrets.json`
- `gitpusher_gitpusher-deployments` fuer geklonte Repositories

Beim Anlegen eines Repositories im Dashboard sollte als Zielpfad ein Pfad unter `/deployments` verwendet werden, zum Beispiel:

```text
/deployments/meine-webseite
```

## GitHub Token

Nach dem ersten Start liegt die Datei im Container unter:

```text
/gitpusher/data/secrets.json
```

Bei Docker-Volumes kann sie so gesetzt werden:

```bash
docker exec -it gitpusher sh
vi /gitpusher/data/secrets.json
```

Format:

```json
{
  "github_pat": "ghp_DEIN_TOKEN",
  "webhook_secrets": {}
}
```

## Proxy

In Nginx Proxy Manager:

- Forward Hostname/IP: `192.168.178.113`
- Forward Port: `8088`
- Scheme: `http`
- Websockets sind nicht erforderlich.

Die GitHub Webhook Payload URL ist danach:

```text
https://deine-domain.example/webhook.php
```
