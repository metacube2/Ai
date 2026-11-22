#!/bin/sh
set -eu

mkdir -p /gitpusher/data /deployments

if [ ! -f /gitpusher/data/config.json ]; then
    printf '%s\n' '{"repositories":[]}' > /gitpusher/data/config.json
fi

if [ ! -f /gitpusher/data/log.json ]; then
    printf '%s\n' '{"entries":[]}' > /gitpusher/data/log.json
fi

if [ ! -f /gitpusher/data/secrets.json ]; then
    printf '%s\n' '{"github_pat":"","webhook_secrets":{}}' > /gitpusher/data/secrets.json
fi

chown -R www-data:www-data /gitpusher/data /deployments
chmod 700 /gitpusher/data
chmod 600 /gitpusher/data/config.json /gitpusher/data/log.json /gitpusher/data/secrets.json

exec "$@"
