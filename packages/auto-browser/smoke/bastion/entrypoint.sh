#!/bin/sh
set -eu

: "${BASTION_USER:=tunnel}"
: "${BASTION_AUTHORIZED_KEYS_PATH:=/ssh-material/smoke_authorized_keys}"

if ! id "${BASTION_USER}" >/dev/null 2>&1; then
  adduser -D -h "/home/${BASTION_USER}" "${BASTION_USER}"
fi

passwd -u "${BASTION_USER}" >/dev/null 2>&1 || true

if [ ! -r "${BASTION_AUTHORIZED_KEYS_PATH}" ]; then
  echo >&2 "authorized keys file is missing: ${BASTION_AUTHORIZED_KEYS_PATH}"
  exit 1
fi

mkdir -p "/home/${BASTION_USER}/.ssh" /run/sshd
cp "${BASTION_AUTHORIZED_KEYS_PATH}" "/home/${BASTION_USER}/.ssh/authorized_keys"
chown -R "${BASTION_USER}:${BASTION_USER}" "/home/${BASTION_USER}/.ssh"
chmod 700 "/home/${BASTION_USER}/.ssh"
chmod 600 "/home/${BASTION_USER}/.ssh/authorized_keys"

ssh-keygen -A >/dev/null

exec /usr/sbin/sshd -D -e -f /etc/ssh/sshd_config
