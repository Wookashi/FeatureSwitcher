#!/usr/bin/env bash
# Upgrade smoke test: proves a candidate image starts on data written by the previously released
# image — the path every real deployment takes, and one unit tests on fresh databases never cover.
#
#   1. run <previous-image> on a fresh volume, wait until healthy, write some data through the API
#   2. replace it with <candidate-image> on the SAME volume, wait until healthy, verify the data
#   3. restart the candidate once more (second start on an already-upgraded database)
#
# <previous-image> is used as-is when present locally (handy for testing against an image built from
# an older git tag), otherwise pulled. If it cannot be pulled (e.g. nothing released yet), steps 1 and the data check
# are skipped with a warning and only fresh start + restart of the candidate are tested.
#
# Usage: upgrade-test.sh <node|manager> <previous-image> <candidate-image>
set -euo pipefail

component="${1:?component (node|manager) required}"
previous_image="${2:?previous image required}"
candidate_image="${3:?candidate image required}"

container="fs-upgrade-${component}"
volume="fs-upgrade-${component}-data"
network="fs-upgrade-${component}-net"
manager_stub="fs-upgrade-${component}-manager-stub"
host_port=18080
base_url="http://localhost:${host_port}"
health_timeout_seconds=90
needs_manager_stub=false

case "${component}" in
  node)
    container_port=5216
    # Node <= 1.2.0 blocks startup (before Kestrel listens) until its registration request to the
    # Manager gets ANY HTTP response, so a plain HTTP server stands in for the Manager. Newer
    # versions retry registration in the background and report the Manager as Degraded on /health,
    # which still answers 200.
    needs_manager_stub=true
    env_args=(
      -e NodeConfiguration__Environment=ci
      -e NodeConfiguration__Name=UpgradeTest
      -e NodeConfiguration__Address=http://localhost:5216
      -e "ManagerSettings__Url=http://${manager_stub}:8080"
    )
    ;;
  manager)
    container_port=5033
    env_args=(
      -e Jwt__SecretKey=UpgradeTestSecretKey_OnlyForCi_AtLeast32Chars!!
    )
    ;;
  *)
    echo "Unknown component '${component}' (expected node|manager)" >&2
    exit 2
    ;;
esac

log() { echo "::group::$*"; }
endlog() { echo "::endgroup::"; }

remove_container() {
  docker rm -f "${container}" >/dev/null 2>&1 || true
}

cleanup() {
  local status=$?
  if [[ ${status} -ne 0 ]]; then
    echo "::error::Upgrade test for ${component} failed. Container logs:"
    docker logs "${container}" 2>&1 || true
  fi
  remove_container
  docker rm -f "${manager_stub}" >/dev/null 2>&1 || true
  docker network rm "${network}" >/dev/null 2>&1 || true
  docker volume rm -f "${volume}" >/dev/null 2>&1 || true
  exit "${status}"
}
trap cleanup EXIT

start() {
  local image="$1"
  remove_container
  docker run -d --name "${container}" \
    --network "${network}" \
    -p "${host_port}:${container_port}" \
    -v "${volume}:/data" \
    "${env_args[@]}" \
    "${image}" >/dev/null
}

# Healthy = /health answers 2xx AND the container is still running a few seconds later, so a
# crash right after the first successful probe (e.g. in a hosted service) is not missed.
wait_healthy() {
  local deadline=$((SECONDS + health_timeout_seconds))
  while (( SECONDS < deadline )); do
    if [[ "$(docker inspect -f '{{.State.Running}}' "${container}")" != "true" ]]; then
      echo "::error::Container exited during startup (exit code $(docker inspect -f '{{.State.ExitCode}}' "${container}"))."
      return 1
    fi
    if curl -fsS "${base_url}/health" >/dev/null 2>&1; then
      sleep 5
      if [[ "$(docker inspect -f '{{.State.Running}}' "${container}")" != "true" ]]; then
        echo "::error::Container exited shortly after becoming healthy."
        return 1
      fi
      curl -fsS "${base_url}/health"; echo
      return 0
    fi
    sleep 2
  done
  echo "::error::Container did not become healthy within ${health_timeout_seconds}s."
  return 1
}

post_json() {
  curl -fsS -X POST -H "Content-Type: application/json" -d "$2" "${base_url}$1"
}

seed_data() {
  case "${component}" in
    node)
      post_json /applications \
        '{"appName":"UpgradeTestApp","environment":"ci","features":[{"featureName":"UpgradeFlag","initialState":true}]}' >/dev/null
      ;;
    manager)
      post_json /api/auth/setup '{"username":"ci-admin","password":"ci-password"}' >/dev/null
      ;;
  esac
}

verify_data() {
  case "${component}" in
    node)
      local state
      state="$(curl -fsS "${base_url}/applications/UpgradeTestApp/features/UpgradeFlag/state/")"
      if [[ "${state}" != "true" ]]; then
        echo "::error::Expected feature state 'true' written by the previous version, got '${state}'."
        return 1
      fi
      ;;
    manager)
      post_json /api/auth/login '{"username":"ci-admin","password":"ci-password"}' >/dev/null || {
        echo "::error::Admin account created by the previous version cannot log in."
        return 1
      }
      ;;
  esac
}

docker rm -f "${container}" "${manager_stub}" >/dev/null 2>&1 || true
docker network rm "${network}" >/dev/null 2>&1 || true
docker volume rm -f "${volume}" >/dev/null 2>&1 || true
docker volume create "${volume}" >/dev/null
docker network create "${network}" >/dev/null

if [[ "${needs_manager_stub}" == "true" ]]; then
  docker pull -q busybox:stable >/dev/null
  docker run -d --name "${manager_stub}" --network "${network}" \
    busybox:stable httpd -f -p 8080 >/dev/null
fi

upgraded=false
if docker image inspect "${previous_image}" >/dev/null 2>&1 || docker pull "${previous_image}" >/dev/null 2>&1; then
  log "Previous version (${previous_image}): start and seed data"
  start "${previous_image}"
  wait_healthy
  seed_data
  verify_data
  endlog
  upgraded=true
else
  echo "::warning::Could not pull ${previous_image}; testing a fresh start of the candidate only."
fi

log "Candidate (${candidate_image}): start on the previous version's data"
start "${candidate_image}"
wait_healthy
if [[ "${upgraded}" == "true" ]]; then
  verify_data
else
  seed_data
fi
endlog

log "Candidate: restart on already-upgraded data"
docker restart "${container}" >/dev/null
wait_healthy
verify_data
endlog

echo "Upgrade test for ${component} passed."
