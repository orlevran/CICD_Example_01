#!/usr/bin/env bash
set -euxo pipefail
cd /home/ubuntu/app
if [ -f docker-compose.rendered.yml ]; then
  docker compose -f docker-compose.rendered.yml down || true
fi