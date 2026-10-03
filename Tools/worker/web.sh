#!/bin/bash
# Status page for the Tailscale network only: http://<tailscale ip>:8080
# Rewrites the page every minute and serves it on the Tailscale address (not on the home network).
mkdir -p "$HOME/chess/www"
cd "$HOME/chess/www"
(
    while true; do
        {
            echo '<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="refresh" content="60">'
            echo '<meta name="viewport" content="width=device-width, initial-scale=1"><title>NNUE pipeline</title>'
            echo '<style>body{font-family:monospace;font-size:14px;margin:16px;background:#111;color:#ddd}pre{white-space:pre-wrap}</style>'
            echo '</head><body><pre>'
            bash "$HOME/chess/status.sh" | sed 's/&/\&amp;/g; s/</\&lt;/g'
            echo '</pre></body></html>'
        } > index.html.tmp && mv index.html.tmp index.html
        sleep 60
    done
) &
# The Tailscale address may not be up yet right after a boot
until IP=$(tailscale ip -4 2>/dev/null) && [ -n "$IP" ] && python3 -m http.server 8080 --bind "$IP"; do sleep 30; done
