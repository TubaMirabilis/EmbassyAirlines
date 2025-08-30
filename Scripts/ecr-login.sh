#!/bin/bash
# Authenticate Docker to the AWS ECR registry for the currently active AWS identity.
#
# Usage:
#   ./ecr-login.sh [region]
#
# Behavior:
#   1. Determines AWS region in this precedence order:
#        a. First positional argument
#        b. $AWS_REGION env var
#        c. $AWS_DEFAULT_REGION env var
#        d. `aws configure get region`
#   2. Fetches the AWS account ID of the current caller (role or user) via STS.
#   3. Performs `docker login` against that account's private ECR registry endpoint.
#
# Notes:
#   - Requires: aws CLI v2, docker CLI.
#   - Token validity: 12 hours; rerun when it expires.
#   - Scope: Login grants access to ALL repositories in that account+region registry.
#   - Supports specifying a profile via AWS_PROFILE (export before running) or --profile in your ~/.aws/config.
#
# Exit codes:
#   0 success
#   1 missing dependencies / region / account id
#   2 docker login failed
set -euo pipefail

log() { printf "[ecr-login] %s\n" "$*" >&2; }
fail() { log "ERROR: $*"; exit 1; }

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command '$1' not found in PATH"
}

require_cmd aws
require_cmd docker

REGION_ARG="${1:-}" || true

# Resolve region
REGION="${REGION_ARG:-${AWS_REGION:-${AWS_DEFAULT_REGION:-}}}"
if [[ -z "$REGION" ]]; then
  REGION=$(aws configure get region 2>/dev/null || true)
fi
[[ -n "$REGION" ]] || fail "AWS region not specified. Provide as first arg or set AWS_REGION/AWS_DEFAULT_REGION or configure a default."

# Get account ID
ACCOUNT_ID=$(aws sts get-caller-identity --query 'Account' --output text 2>/dev/null || true)
[[ "$ACCOUNT_ID" =~ ^[0-9]{12}$ ]] || fail "Unable to determine AWS account ID (got: '$ACCOUNT_ID'). Check your AWS credentials."

REGISTRY="${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"
log "Logging in to ECR registry: ${REGISTRY}"; log "Using AWS profile: ${AWS_PROFILE:-<default>}"

# Fetch and pipe password
if aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$REGISTRY"; then
  log "Login successful. You can now tag & push images, e.g.:"
  cat >&2 <<EOF
  docker tag local-image:tag ${REGISTRY}/my-repo:tag
  docker push ${REGISTRY}/my-repo:tag
EOF
else
  fail "Docker login failed"
fi
