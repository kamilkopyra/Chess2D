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
#>
param(
    [string]$Bot = "v2",
    [int]$Depth = 3,
    [string]$OpponentBot = "",
    [int]$OpponentDepth = 3,
    [int]$StockfishElo = 1320,
    [int]$Games = 100,
    [string]$TimeControl = "60+0.6",
    [int]$Concurrency = 4,
    [int]$MaxMoves = 200
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

# --- Build the UCI front-end with the current engine and bot sources ---
Write-Host "Building Chess2D.Uci..."
$uciOut = Join-Path $root "bin\uci"
dotnet build (Join-Path $root "Chess2D.Uci\Chess2D.Uci.csproj") -c Release -nologo -v q -o $uciOut
if ($LASTEXITCODE -ne 0) { throw "Build of Chess2D.Uci failed" }
$uci = Join-Path $uciOut "Chess2D.Uci.exe"

# --- Match ---
$botName = "Chess2D-$Bot-d$Depth"
$engineArgs = @("-engine", "name=$botName", "cmd=$uci", "arg=--bot", "arg=$Bot", "arg=--depth", "arg=$Depth")

if ($useStockfish) {
    $opponentName = "Stockfish-$StockfishElo"
    $opponentArgs = @("-engine", "name=$opponentName", "cmd=$stockfish",
                      "option.UCI_LimitStrength=true", "option.UCI_Elo=$StockfishElo")
} else {
    $opponentName = "Chess2D-$OpponentBot-d$OpponentDepth"
    $opponentArgs = @("-engine", "name=$opponentName", "cmd=$uci",
                      "arg=--bot", "arg=$OpponentBot", "arg=--depth", "arg=$OpponentDepth")
}

$stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
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

Write-Host "Match: $botName vs $opponentName, $($rounds * 2) games, tc=$TimeControl, concurrency=$Concurrency"
Write-Host "PGN: $pgn"
& $cutechess @cutechessArgs 2>&1 | Tee-Object -FilePath $log

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
