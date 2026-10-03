<#
.SYNOPSIS
    Plays a match between a Chess2D bot and an opponent with cutechess-cli (or fastchess) and estimates the bot's Elo.

.DESCRIPTION
    On first run it downloads cutechess-cli and Stockfish into Tools\external (ignored by git);
    with -Fastchess also fastchess and the opening books.
    Then it builds the UCI front-end (Tools\Chess2D.Uci) and plays the match.
    The opponent is Stockfish with limited strength (UCI_Elo), or another Chess2D bot (-OpponentBot).

.EXAMPLE
    .\match.ps1
    Bot v2 at depth 3 against Stockfish limited to 1320 Elo, 100 games.

.EXAMPLE
    .\match.ps1 -Bot v2 -Depth 4 -StockfishElo 1500 -Games 200

.EXAMPLE
    .\match.ps1 -Bot v2 -Depth 3 -OpponentBot v1
    Bot against bot (no Stockfish) - handy for checking that a new version is stronger.

.EXAMPLE
    .\match.ps1 -Bot v16 -OpponentBot v15 -Sprt -Games 1000 -TimeControl 20+0.2 -Concurrency 8
    SPRT test: plays until it is statistically clear whether v16 is at least Elo1 (default 10) stronger
    (H1 accepted) or not better than Elo0 (default 0) (H0 accepted). -Games is only the upper limit then.

.EXAMPLE
    .\match.ps1 -Bot v19 -OpponentBot v18 -OpponentSource C:\temp\chess2d-old -OpponentLabel old
    The opponent is built from another copy of the repository (e.g. an older commit or another branch),
    so the same bot can be compared before and after a change in the shared engine code.

.EXAMPLE
    .\match.ps1 -Bot v20 -OpponentBot v19 -Fastchess -Sprt -Elo1 30 -Games 1000 -TimeControl 20+0.2 -Concurrency 8
    The same SPRT with fastchess: games start from an opening book (each opening once with each colour,
    the bots' own book is switched off) and the statistics count game pairs, so tests need fewer games.
#>
param(
    [string]$Bot = "v2",
    [int]$Depth = 0,
    [string]$OpponentBot = "",
    [int]$OpponentDepth = 0,
    [int]$StockfishElo = 1320,
    [int]$Games = 100,
    [string]$TimeControl = "60+0.6",
    [int]$Concurrency = 4,
    [int]$MaxMoves = 200,
    # Sequential probability ratio test: stop as soon as the result is statistically clear
    [switch]$Sprt,
    [double]$Elo0 = 0,
    [double]$Elo1 = 10,
    # Repository roots to build each engine from (default: this repository). Lets the two engines
    # come from different branches or commits, e.g. v18 before and after a change in Position.
    [string]$BotSource = "",
    [string]$OpponentSource = "",
    # Added to the engine name in the PGN/log, e.g. "Chess2D-v18-old"
    [string]$BotLabel = "",
    [string]$OpponentLabel = "",
    # Play with fastchess instead of cutechess-cli, starting the games from an opening book
    [switch]$Fastchess,
    # Opening book for fastchess: a file name in Tools\external\openings or a full path. Default:
    # UHO_4060_v2.epd (unbalanced, fewer draws) against another bot, 8moves_v3.pgn (balanced) against Stockfish
    [string]$Openings = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"   # makes Invoke-WebRequest much faster in Windows PowerShell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$root = $PSScriptRoot
$external = Join-Path $root "external"
$matchesDir = Join-Path $root "matches"
New-Item -ItemType Directory -Force $external, $matchesDir | Out-Null

# Downloads the latest release asset whose name matches $pattern and unpacks it into $targetDir
function Install-GitHubRelease([string]$repo, [string]$pattern, [string]$targetDir) {
    $release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest" -Headers @{ "User-Agent" = "Chess2D" }
    $asset = $release.assets | Where-Object { $_.name -match $pattern } | Select-Object -First 1
    if ($null -eq $asset) { throw "No asset matching '$pattern' in the latest $repo release" }

    Write-Host "Downloading $($asset.name) ($($release.tag_name))..."
    $zip = Join-Path $external $asset.name
    Invoke-WebRequest $asset.browser_download_url -OutFile $zip -Headers @{ "User-Agent" = "Chess2D" }
    Expand-Archive $zip -DestinationPath $targetDir -Force
    Remove-Item $zip
}

function Find-Exe([string]$dir, [string]$filter) {
    if (-not (Test-Path $dir)) { return $null }
    Get-ChildItem $dir -Recurse -Filter $filter | Select-Object -First 1 -ExpandProperty FullName
}

# --- Tools ---
$useStockfish = [string]::IsNullOrEmpty($OpponentBot)

if ($Fastchess) {
    $runner = Find-Exe (Join-Path $external "fastchess") "fastchess.exe"
    if (-not $runner) {
        Install-GitHubRelease "Disservin/fastchess" "windows-x86-64\.zip$" (Join-Path $external "fastchess")
        $runner = Find-Exe (Join-Path $external "fastchess") "fastchess.exe"
    }

    # Opening books from the Stockfish project (https://github.com/official-stockfish/books)
    if (-not $Openings) { $Openings = if ($useStockfish) { "8moves_v3.pgn" } else { "UHO_4060_v2.epd" } }
    $openingsFile = if (Test-Path $Openings) { (Resolve-Path $Openings).Path } else { Join-Path $external "openings\$Openings" }
    if (-not (Test-Path $openingsFile)) {
        $name = Split-Path $openingsFile -Leaf
        Write-Host "Downloading opening book $name..."
        New-Item -ItemType Directory -Force (Split-Path $openingsFile -Parent) | Out-Null
        $zip = "$openingsFile.zip"
        Invoke-WebRequest "https://github.com/official-stockfish/books/raw/master/$name.zip" -OutFile $zip
        Expand-Archive $zip -DestinationPath (Split-Path $openingsFile -Parent) -Force
        Remove-Item $zip
    }
    $openingsFormat = if ($openingsFile -match "\.pgn$") { "pgn" } else { "epd" }
} else {
    $runner = Find-Exe (Join-Path $external "cutechess") "cutechess-cli.exe"
    if (-not $runner) {
        Install-GitHubRelease "cutechess/cutechess" "win64\.zip$" (Join-Path $external "cutechess")
        $runner = Find-Exe (Join-Path $external "cutechess") "cutechess-cli.exe"
    }
}

if ($useStockfish) {
    $stockfish = Find-Exe (Join-Path $external "stockfish") "stockfish*.exe"
    if (-not $stockfish) {
        Install-GitHubRelease "official-stockfish/Stockfish" "windows-x86-64.*\.zip$" (Join-Path $external "stockfish")
        $stockfish = Find-Exe (Join-Path $external "stockfish") "stockfish*.exe"
    }
}

# --- Sanity check: too many parallel games starve the engines of CPU and they lose on time ---
$cores = (Get-CimInstance Win32_Processor | Measure-Object -Property NumberOfCores -Sum).Sum
$safeConcurrency = [Math]::Max(1, [Math]::Floor($cores / 2))
if ($Concurrency -gt $safeConcurrency) {
    Write-Warning ("Concurrency $Concurrency is high for $cores CPU cores (each game runs two engines). " +
                   "Engines may lose on time and the result will be unreliable. Recommended: $safeConcurrency or less.")
}

# --- Build the UCI front-end with the current engine and bot sources ---
# Every match gets its own build folder, so a running match never locks the exe of the next one.
$stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
# Builds Chess2D.Uci from the repository at $sourceRoot (empty = this one) into its own folder, returns the exe
function Build-Uci([string]$sourceRoot, [string]$folder) {
    $repo = if ([string]::IsNullOrEmpty($sourceRoot)) { Split-Path $root -Parent } else { $sourceRoot }
    Write-Host "Building Chess2D.Uci from $repo..."
    $out = Join-Path $root "bin\uci\$stamp\$folder"
    dotnet build (Join-Path $repo "Tools\Chess2D.Uci\Chess2D.Uci.csproj") -c Release -nologo -v q -o $out | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Build of Chess2D.Uci from $repo failed" }
    $exe = Join-Path $out "Chess2D.Uci.exe"

    # Warm-up: start the freshly built engine once and let it search briefly. The first run of new files can be
    # slow (antivirus scan, first load of the code); this way it happens here and not in the middle of a game.
    "uci`nisready`nposition startpos moves e2e4 e7e5 g1f3 b8c6`ngo movetime 300`nquit`n" | & $exe --bot v2 --no-book | Out-Null
    return $exe
}

$uci = Build-Uci $BotSource "bot"
$opponentUci = if ($OpponentSource -ne $BotSource) { Build-Uci $OpponentSource "opponent" } else { $uci }

# Remove build folders of earlier matches. Only old ones: a match running in parallel may still be using
# a recent one, and deleting part of its files (e.g. the opening book) would break its engines.
Get-ChildItem (Join-Path $root "bin\uci") -Directory |
    Where-Object { $_.Name -ne $stamp -and $_.LastWriteTime -lt (Get-Date).AddHours(-12) } | ForEach-Object {
    try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { }
}

# --- Match ---
# Depth 0 = don't pass it: fixed-depth bots then use their default (3), time-managed bots (v11+)
# search as deep as the clock allows. With an opening book the bots' own book is switched off.
function Get-ChessEngineArgs([string]$botVersion, [int]$botDepth, [string]$exe, [string]$label) {
    $name = if ($botDepth -gt 0) { "Chess2D-$botVersion-d$botDepth" } else { "Chess2D-$botVersion" }
    if ($label) { $name += "-$label" }
    $uciArgs = @("--bot", $botVersion)
    if ($botDepth -gt 0) { $uciArgs += @("--depth", "$botDepth") }
    if ($Fastchess) {
        $uciArgs += "--no-book"
        # fastchess takes all engine arguments in one "args=..." value
        $engine = @("-engine", "name=$name", "cmd=$exe", "args=$($uciArgs -join ' ')")
    } else {
        $engine = @("-engine", "name=$name", "cmd=$exe") + ($uciArgs | ForEach-Object { "arg=$_" })
    }
    return $name, $engine
}

$botName, $engineArgs = Get-ChessEngineArgs $Bot $Depth $uci $BotLabel

if ($useStockfish) {
    $opponentName = "Stockfish-$StockfishElo"
    $opponentArgs = @("-engine", "name=$opponentName", "cmd=$stockfish",
                      "option.UCI_LimitStrength=true", "option.UCI_Elo=$StockfishElo")
} else {
    $opponentName, $opponentArgs = Get-ChessEngineArgs $OpponentBot $OpponentDepth $opponentUci $OpponentLabel
}

$pgn = Join-Path $matchesDir "$stamp`_$botName`_vs_$opponentName.pgn"
$log = [IO.Path]::ChangeExtension($pgn, ".log")

# -games 2 per round = each opening played once with each color
$rounds = [Math]::Max(1, [Math]::Ceiling($Games / 2))
$culture = [Globalization.CultureInfo]::InvariantCulture
$runnerArgs = $engineArgs + $opponentArgs + @(
    "-each", "proto=uci", "tc=$TimeControl",
    "-games", "2", "-rounds", "$rounds",
    "-concurrency", "$Concurrency",
    "-maxmoves", "$MaxMoves",
    "-ratinginterval", "10",
    "-recover"
)
if ($Fastchess) {
    # -repeat: both games of a round start from the same opening, with the colours swapped.
    # Output in cutechess format, so the summary below works for both runners.
    $runnerArgs += @("-repeat",
                     "-openings", "file=$openingsFile", "format=$openingsFormat", "order=random",
                     "-output", "format=cutechess",
                     "-pgnout", "file=$pgn")
} else {
    $runnerArgs += @("-pgnout", $pgn)
}
if ($Sprt) {
    # alpha/beta: 5% chance of accepting the wrong hypothesis in either direction.
    # fastchess: the logistic model, so Elo0/Elo1 mean the same as with cutechess.
    $runnerArgs += @("-sprt", "elo0=$($Elo0.ToString($culture))", "elo1=$($Elo1.ToString($culture))", "alpha=0.05", "beta=0.05")
    if ($Fastchess) { $runnerArgs += "model=logistic" }
}

$mode = if ($Sprt) { "SPRT elo0=$Elo0 elo1=$Elo1, at most $($rounds * 2) games" } else { "$($rounds * 2) games" }
$runnerName = if ($Fastchess) { "fastchess, openings $(Split-Path $openingsFile -Leaf)" } else { "cutechess" }
Write-Host "Match: $botName vs $opponentName, $mode, tc=$TimeControl, concurrency=$Concurrency ($runnerName)"
Write-Host "PGN: $pgn"
# Warnings that the runner prints to stderr must not abort the match.
# fastchess warns on every move that our engine reports no score; those lines are left out.
$ErrorActionPreference = "Continue"
# Two logs: <name>.full.log with everything the runner prints, and <name>.log with only the match itself
# (games, scores, Elo, summary). fastchess repeats engine warnings with the whole position and the engine's
# last "info" line ("Position; ...", "Moves; ...", "Warning; ...", "Info; ..."), which bury the results.
$fullLog = [IO.Path]::ChangeExtension($pgn, ".full.log")
$fullWriter = New-Object IO.StreamWriter($fullLog, $false, (New-Object Text.UTF8Encoding($false)))
# fastchess saves its state (config.json, for -recover) in the current directory: run it in Tools\matches
Push-Location $matchesDir
try {
    & $runner @runnerArgs 2>&1 | ForEach-Object { "$_" -replace "\x1b\[[0-9;]*m", "" } |
        ForEach-Object { $fullWriter.WriteLine($_); $fullWriter.Flush(); $_ } |
        Where-Object { $_ -notmatch "No info line available to extract score" -and $_ -notmatch "^(Position|Moves|Warning|Info);" } |
        Tee-Object -FilePath $log
} finally {
    Pop-Location
    $fullWriter.Dispose()
}
$ErrorActionPreference = "Stop"

# --- Summary ---
$eloLine = Select-String -Path $log -Pattern "Elo difference: (-?[\d.]+|-?inf) \+/- ([\d.]+|nan)" | Select-Object -Last 1
if ($eloLine) {
    $diff = $eloLine.Matches[0].Groups[1].Value
    $margin = $eloLine.Matches[0].Groups[2].Value
    Write-Host ""
    Write-Host "Elo difference ($botName vs $opponentName): $diff +/- $margin"
    if ($useStockfish -and $diff -notmatch "inf") {
        $estimate = [Math]::Round($StockfishElo + [double]::Parse($diff, $culture))
        Write-Host "Estimated Elo of ${botName}: about $estimate (Stockfish UCI_Elo scale)"
    }
    if ($diff -match "inf") {
        Write-Host "One side won every game - the Elo difference can't be computed. Pick a stronger/weaker opponent."
    }
}

if ($Sprt) {
    $sprtLine = Select-String -Path $log -Pattern "^SPRT:" | Select-Object -Last 1
    if ($sprtLine) {
        Write-Host $sprtLine.Line
        if ($sprtLine.Line -match "H1 was accepted") { Write-Host "SPRT result: $botName is stronger (H1 accepted)." }
        elseif ($sprtLine.Line -match "H0 was accepted") { Write-Host "SPRT result: $botName is NOT stronger (H0 accepted)." }
        else { Write-Host "SPRT result: inconclusive, the game limit was reached first." }
    }
}

$timeLosses = (Select-String -Path $log -Pattern "^Finished game .*(loses on time|time forfeit)").Count
if ($timeLosses -gt 0) {
    Write-Warning ("$timeLosses game(s) were lost on time. The result is unreliable - " +
                   "lower -Concurrency or use a longer -TimeControl.")
}
