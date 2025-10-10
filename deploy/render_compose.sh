#!/usr/bin/env bash
set -euxo pipefail
cd /home/ubuntu/app

# ensure jq exists (first run)
if ! command -v jq >/dev/null 2>&1; then
  sudo apt-get update && sudo apt-get install -y jq
fi

IMAGE_URI=$(jq -r .ImageURI imageDetail.json)
sed "s|\${IMAGE_URI}|${IMAGE_URI}|g" docker-compose.prod.yml > docker-compose.rendered.yml