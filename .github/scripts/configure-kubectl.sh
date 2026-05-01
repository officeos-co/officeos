#!/usr/bin/env bash
set -euo pipefail

KUBECONFIG_PATH="${RUNNER_TEMP:-/tmp}/eaos-kubeconfig"
export KUBECONFIG="${KUBECONFIG_PATH}"
rm -f "${KUBECONFIG_PATH}"

if [[ -n "${GITHUB_ENV:-}" ]]; then
  echo "KUBECONFIG=${KUBECONFIG_PATH}" >> "${GITHUB_ENV}"
fi

configure_from_secrets() {
  kubectl config set-cluster nova --server="${KUBE_SERVER}" --insecure-skip-tls-verify=true
  kubectl config set-credentials ci --token="${KUBE_TOKEN}"
  kubectl config set-context nova --cluster=nova --user=ci --namespace=default
  kubectl config use-context nova
}

configure_from_service_account() {
  local server
  local token

  server="https://${KUBERNETES_SERVICE_HOST}:${KUBERNETES_SERVICE_PORT_HTTPS:-443}"
  token="$(cat /var/run/secrets/kubernetes.io/serviceaccount/token)"

  rm -f "${KUBECONFIG_PATH}"
  kubectl config set-cluster nova \
    --server="${server}" \
    --certificate-authority=/var/run/secrets/kubernetes.io/serviceaccount/ca.crt
  kubectl config set-credentials ci --token="${token}"
  kubectl config set-context nova --cluster=nova --user=ci --namespace=default
  kubectl config use-context nova
}

can_use_kubectl() {
  kubectl auth can-i get deployments --quiet >/dev/null 2>&1
}

has_service_account() {
  [[ -f /var/run/secrets/kubernetes.io/serviceaccount/token && -f /var/run/secrets/kubernetes.io/serviceaccount/ca.crt && -n "${KUBERNETES_SERVICE_HOST:-}" ]]
}

if [[ -n "${KUBE_SERVER:-}" && -n "${KUBE_TOKEN:-}" ]]; then
  configure_from_secrets
  if can_use_kubectl; then
    exit 0
  fi

  echo "KUBE_SERVER/KUBE_TOKEN did not authenticate; trying the runner pod service account." >&2
fi

if has_service_account; then
  configure_from_service_account
  can_use_kubectl
  exit 0
fi

echo "Unable to configure kubectl: set valid KUBE_SERVER and KUBE_TOKEN secrets, or run from a Kubernetes pod with a mounted service account." >&2
exit 1
