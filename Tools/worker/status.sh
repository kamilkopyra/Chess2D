#!/bin/bash
# Short progress report of the NNUE data pipeline (also written to the status page by web.sh)
cd "$HOME/chess"
PGN=$(ls lichess_db_standard_rated_*.pgn.zst 2>/dev/null | head -1)
TOTAL_BYTES=29224520887   # size of the 2026-09 file on database.lichess.org

echo "NNUE data pipeline - $(date '+%F %T')"
echo
if [ -n "$PGN" ] && [ ! -f "$PGN.done" ]; then
    size=$(stat -c %s "$PGN")
    echo "STEP 1/4: download  $((size / 1048576)) / $((TOTAL_BYTES / 1048576)) MB  ($((size * 100 / TOTAL_BYTES))%)"
elif [ ! -f positions.done ]; then
    echo "STEP 2/4: extracting positions (target 100 000 000)"
    [ -f extract.log ] && tail -1 extract.log
elif [ ! -f labelled.done ]; then
    [ -f positions.count ] || wc -l < positions.txt > positions.count
    total=$(cat positions.count)
    done=$(tail -1 label.log 2>/dev/null | grep -oE '^ *[0-9]+' | tr -d ' ')
    rate=$(tail -1 label.log 2>/dev/null | grep -oE '\(([0-9]+)/s' | tr -d '(/s')
    echo "STEP 3/4: labelling  ${done:-0} / $total  ($(( ${done:-0} * 100 / total ))%)"
    if [ -n "$rate" ] && [ "$rate" -gt 0 ]; then
        left=$(( (total - done) / rate ))
        echo "          $rate positions/s, about $((left / 86400)) d $((left % 86400 / 3600)) h left"
    fi
elif [ ! -f labelled.txt.zst ]; then
    echo "STEP 4/4: compressing"
else
    echo "FINISHED: labelled.txt.zst $(ls -lh labelled.txt.zst | awk '{print $5}')"
fi

echo
echo "== last log lines"
tail -4 pipeline.log 2>/dev/null
echo
echo "== machine"
uptime
T=$(cat /sys/class/thermal/thermal_zone*/temp 2>/dev/null | sort -n | tail -1)
[ -n "$T" ] && echo "hottest sensor: $((T / 1000)) C"
echo "disk: $(df -h ~ | tail -1 | awk '{print $4}') free"
tmux has-session -t nnue 2>/dev/null && echo "pipeline session: running" || echo "pipeline session: NOT RUNNING"
