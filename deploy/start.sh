#!/usr/bin/env bash
set -euxo pipefail
cd /home/ubuntu/app

# fallbacks if env vars not pre-set
export AWS_REGION="${AWS_REGION:-eu-west-1}"
export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-$AWS_REGION}"

aws ecr get-login-password --region "$AWS_REGION" \
 | docker login --username AWS --password-stdin "${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

docker compose -f docker-compose.rendered.yml pull
docker compose -f docker-compose.rendered.yml up -d --remove-orphans
docker ps