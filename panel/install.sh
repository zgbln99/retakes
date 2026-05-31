#!/usr/bin/env bash
#
# CWELOWNIA panel — one-shot installer for a fresh Ubuntu/Debian VPS.
# Installs Node.js (if missing), dependencies, creates .env, and sets up a
# systemd service that auto-starts the panel on boot.
#
# Usage:
#   sudo bash install.sh
#
set -euo pipefail

PANEL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_NAME="cwelownia-panel"
RUN_USER="${SUDO_USER:-$USER}"

echo "==> CWELOWNIA panel installer"
echo "    dir:  $PANEL_DIR"
echo "    user: $RUN_USER"

# --- 1. Node.js ---
if ! command -v node >/dev/null 2>&1; then
  echo "==> Installing Node.js 20.x"
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt-get install -y nodejs
else
  echo "==> Node.js present: $(node --version)"
fi

# --- 2. Dependencies ---
echo "==> Installing npm dependencies"
cd "$PANEL_DIR"
npm install --omit=dev

# --- 3. .env ---
if [ ! -f "$PANEL_DIR/.env" ]; then
  echo "==> Creating .env from template (EDIT IT AFTERWARDS!)"
  cp "$PANEL_DIR/.env.example" "$PANEL_DIR/.env"
  # generate a random session secret
  SECRET="$(head -c 32 /dev/urandom | base64 | tr -dc 'a-zA-Z0-9')"
  sed -i "s|^SESSION_SECRET=.*|SESSION_SECRET=${SECRET}|" "$PANEL_DIR/.env"
  echo "    -> $PANEL_DIR/.env created. Fill in DB_* and ADMIN_* values!"
else
  echo "==> .env already exists, leaving it untouched"
fi

# --- 4. systemd service ---
echo "==> Installing systemd service: $SERVICE_NAME"
cat > "/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=CWELOWNIA CS2 web panel
After=network.target

[Service]
Type=simple
User=${RUN_USER}
WorkingDirectory=${PANEL_DIR}
EnvironmentFile=${PANEL_DIR}/.env
ExecStart=$(command -v node) ${PANEL_DIR}/server.js
Restart=always
RestartSec=5
Environment=NODE_ENV=production

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}"

echo ""
echo "==> Done. Next steps:"
echo "    1. Edit your settings:   nano ${PANEL_DIR}/.env"
echo "    2. Start the panel:      sudo systemctl start ${SERVICE_NAME}"
echo "    3. Check status/logs:    systemctl status ${SERVICE_NAME}  |  journalctl -u ${SERVICE_NAME} -f"
echo "    4. Open:                 http://<VPS_IP>:\${PORT:-8080}"
echo ""
echo "    (Recommended: put nginx + HTTPS in front — see VPS_SETUP.md)"
