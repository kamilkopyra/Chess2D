#!/bin/bash
# Starts pipeline.sh in a tmux session "nnue" and the status page (web.sh) in a session "web", unless they
# are already running (also called from crontab @reboot).
# Watch the pipeline with: tmux attach -t nnue   (leave with Ctrl+B, then D)
tmux has-session -t nnue 2>/dev/null || tmux new-session -d -s nnue "bash $HOME/chess/pipeline.sh; echo; echo 'pipeline finished, press Enter'; read"
tmux has-session -t web 2>/dev/null || tmux new-session -d -s web "bash $HOME/chess/web.sh"
