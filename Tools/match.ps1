<#
.SYNOPSIS
    Plays a match between a Chess2D bot and an opponent with cutechess-cli and estimates the bot's Elo.

.DESCRIPTION
    On first run it downloads cutechess-cli and Stockfish into Tools\external (ignored by git).
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
    [double]$Elo1 = 10
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
$cutechess = Find-Exe (Join-Path $external "cutechess") "cutechess-cli.exe"
if (-not $cutechess) {
    Install-GitHubRelease "cutechess/cutechess" "win64\.zip$" (Join-Path $external "cutechess")
    $cutechess = Find-Exe (Join-Path $external "cutechess") "cutechess-cli.exe"
}

$useStockfish = [string]::IsNullOrEmpty($OpponentBot)
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
Write-Host "Building Chess2D.Uci..."
$uciOut = Join-Path $root "bin\uci\$stamp"
dotnet build (Join-Path $root "Chess2D.Uci\Chess2D.Uci.csproj") -c Release -nologo -v q -o $uciOut
if ($LASTEXITCODE -ne 0) { throw "Build of Chess2D.Uci failed" }
$uci = Join-Path $uciOut "Chess2D.Uci.exe"

# Warm-up: start the freshly built engine once and let it search briefly. The first run of new files can be
# slow (antivirus scan, first load of the code); this way it happens here and not in the middle of a game.
"uci`nisready`nposition startpos moves e2e4 e7e5 g1f3 b8c6`ngo movetime 300`nquit`n" | & $uci --bot v2 --no-book | Out-Null

# Remove build folders of earlier matches (skipped silently if a match is still using them)
Get-ChildItem (Join-Path $root "bin\uci") -Directory | Where-Object { $_.Name -ne $stamp } | ForEach-Object {
    try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { }
}

# --- Match ---
# Depth 0 = don't pass it: fixed-depth bots then use their default (3), time-managed bots (v11+)
# search as deep as the clock allows
function Get-ChessEngineArgs([string]$botVersion, [int]$botDepth) {
    $name = if ($botDepth -gt 0) { "Chess2D-$botVersion-d$botDepth" } else { "Chess2D-$botVersion" }
    $engine = @("-engine", "name=$name", "cmd=$uci", "arg=--bot", "arg=$botVersion")
    if ($botDepth -gt 0) { $engine += @("arg=--depth", "arg=$botDepth") }
    return $name, $engine
}

$botName, $engineArgs = Get-ChessEngineArgs $Bot $Depth

if ($useStockfish) {
    $opponentName = "Stockfish-$StockfishElo"
    $opponentArgs = @("-engine", "name=$opponentName", "cmd=$stockfish",
                      "option.UCI_LimitStrength=true", "option.UCI_Elo=$StockfishElo")
} else {
    $opponentName, $opponentArgs = Get-ChessEngineArgs $OpponentBot $OpponentDepth
}

$pgn = Join-Path $matchesDir "$stamp`_$botName`_vs_$opponentName.pgn"
$log = [IO.Path]::ChangeExtension($pgn, ".log")

# -games 2 per round = each opening played once with each color
$rounds = [Math]::Max(1, [Math]::Ceiling($Games / 2))
$cutechessArgs = $engineArgs + $opponentArgs + @(
    "-each", "proto=uci", "tc=$TimeControl",
    "-games", "2", "-rounds", "$rounds",
    "-concurrency", "$Concurrency",
    "-maxmoves", "$MaxMoves",
    "-ratinginterval", "10",
    "-recover",
    "-pgnout", $pgn
)
if ($Sprt) {
    # alpha/beta: 5% chance of accepting the wrong hypothesis in either direction
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $cutechessArgs += @("-sprt", "elo0=$($Elo0.ToString($culture))", "elo1=$($Elo1.ToString($culture))", "alpha=0.05", "beta=0.05")
}

$mode = if ($Sprt) { "SPRT elo0=$Elo0 elo1=$Elo1, at most $($rounds * 2) games" } else { "$($rounds * 2) games" }
Write-Host "Match: $botName vs $opponentName, $mode, tc=$TimeControl, concurrency=$Concurrency"
Write-Host "PGN: $pgn"
# Warnings that cutechess prints to stderr must not abort the match
$ErrorActionPreference = "Continue"
& $cutechess @cutechessArgs 2>&1 | ForEach-Object { "$_" } | Tee-Object -FilePath $log
$ErrorActionPreference = "Stop"

# --- Summary ---
$eloLine = Select-String -Path $log -Pattern "Elo difference: (-?[\d.]+|-?inf) \+/- ([\d.]+|nan)" | Select-Object -Last 1
if ($eloLine) {
    $diff = $eloLine.Matches[0].Groups[1].Value
    $margin = $eloLine.Matches[0].Groups[2].Value
    Write-Host ""
    Write-Host "Elo difference ($botName vs $opponentName): $diff +/- $margin"
    if ($useStockfish -and $diff -notmatch "inf") {
        $estimate = [Math]::Round($StockfishElo + [double]::Parse($diff, [Globalization.CultureInfo]::InvariantCulture))
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

$timeLosses =(Select-String -Path $log -Pattern "^Finished game .*loses on time").Count
if ($timeLosses -gt 0) {
    Write-Warning ("$timeLosses game(s) were lost on time. The result is unreliable - " +
                   "lower -Concurrency or use a longer -TimeControl.")
}
