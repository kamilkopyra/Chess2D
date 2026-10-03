#!/bin/bash
# NNUE training data, step by step: download one Lichess month, extract quiet positions, label them with
# Stockfish, compress the result. Every step is skipped once its .done marker exists, and labelling continues
# where it stopped, so after a crash or a power cut the script just starts again (run.sh, crontab @reboot).
set -u -o pipefail
cd "$HOME/chess"

MONTH=2026-09
THREADS=$(cat threads.txt 2>/dev/null || echo 6)   # change threads.txt to use more or fewer cores
NODES=5000
PGN=lichess_db_standard_rated_$MONTH.pgn.zst
URL=https://database.lichess.org/standard/$PGN

log() { echo "$(date '+%F %T') $*" | tee -a pipeline.log; }

# 1. Download (wget -c continues a partial file), then check it against the published checksum
if [ ! -f "$PGN.done" ]; then
    log "download $PGN"
    # A dead connection (network change, router restart) is dropped after 60 s and the download continues
    until wget -c -q --timeout=60 --tries=0 --retry-connrefused --waitretry=30 "$URL"; do log "download interrupted, retrying in 60 s"; sleep 60; done
    log "checking the checksum"
    if curl -s https://database.lichess.org/standard/sha256sums.txt | grep " $PGN\$" | sha256sum -c --status; then
        touch "$PGN.done"
        log "download ok"
    else
        log "checksum mismatch: deleting the file, it will be downloaded again"
        rm -f "$PGN"
        exit 1
    fi
fi

# 2. Extract: about 100M quiet positions, 1800+ games plus a quarter of the weaker ones, no bullet, no duplicates.
#    Not resumable: an interrupted extraction starts again.
if [ ! -f positions.done ]; then
    log "extract"
    if zstd -dc "$PGN" | bin/Chess2D.Tune extract --out positions.tmp --per-game 5 --skip-plies 16 --max 100000000 \
            --min-elo 1800 --low-elo-keep 0.25 --min-seconds 180 --dedup-bits 32 - >> extract.log 2>&1; then
        mv positions.tmp positions.txt
        touch positions.done
        log "extract done: $(wc -l < positions.txt) positions"
    else
        log "extract failed (see extract.log)"
        exit 1
    fi
fi

# 3. Label with Stockfish (continues an existing labelled.txt)
if [ ! -f labelled.done ]; then
    log "label with $THREADS threads, $NODES nodes"
    until bin/Chess2D.Tune label --data positions.txt --out labelled.txt --stockfish bin/stockfish \
            --threads "$THREADS" --nodes "$NODES" >> label.log 2>&1; do
        log "label stopped with an error, continuing in 60 s"
        sleep 60
    done
    touch labelled.done
    log "label done: $(wc -l < labelled.txt) positions"
fi

# 4. Compress for the transfer
if [ ! -f labelled.txt.zst ]; then
    log "compress"
    zstd -q -T0 -10 labelled.txt -o labelled.txt.zst.tmp && mv labelled.txt.zst.tmp labelled.txt.zst
fi
log "all done: $(ls -lh labelled.txt.zst | awk '{print $5}') labelled.txt.zst"
