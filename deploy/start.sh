#!/usr/bin/env bash
set -Eeuo pipefail

cd /home/ubuntu/app

# --- Region defaults ---
AWS_REGION="${AWS_REGION:-eu-west-1}"
AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-$AWS_REGION}"
export AWS_REGION AWS_DEFAULT_REGION

# --- Resolve account id if not supplied ---
AWS_ACCOUNT_ID="${AWS_ACCOUNT_ID:-$(aws sts get-caller-identity --query Account --output text)}"
: "${AWS_ACCOUNT_ID:?Failed to resolve AWS_ACCOUNT_ID}"
ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

# --- Login to ECR (non-interactive) ---
aws ecr get-login-password --region "$AWS_REGION" \
  | sudo docker login --username AWS --password-stdin "$ECR_URI"

# --- Deploy with Compose ---
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.rendered.yml}"
if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file '$COMPOSE_FILE' not found" >&2
  exit 1
fi

sudo docker compose -f "$COMPOSE_FILE" pull
sudo docker compose -f "$COMPOSE_FILE" up -d --remove-orphans
sudo docker ps