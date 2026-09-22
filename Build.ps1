param(
    [string]$GameDir = 'C:\Steam Games\steamapps\common\Sailwind',
    [string]$BepInExCore = '',
    [switch]$SkipChecks
)
$ErrorActionPreference = 'Stop'
if (-not $BepInExCore) { $BepInExCore = Join-Path $GameDir 'BepInEx\core' }
foreach ($reference in @(
    (Join-Path $GameDir 'Sailwind_Data\Managed\Assembly-CSharp.dll'),
    (Join-Path $BepInExCore 'BepInEx.dll'),
    (Join-Path $BepInExCore '0Harmony.dll')
)) {
    if (-not (Test-Path -LiteralPath $reference -PathType Leaf)) {
        throw "Missing build reference: $reference. Pass -GameDir or -BepInExCore."
    }
}
$project = Join-Path $PSScriptRoot 'src\SailwindFoodPreservation.csproj'
dotnet build $project -c Release "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore" -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
if (-not $SkipChecks) {
    $checks = Join-Path $PSScriptRoot 'tests\PreservationChecks.csproj'
    dotnet run --project $checks -c Release "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore" -p:NuGetAudit=false -- $GameDir
    if ($LASTEXITCODE -ne 0) { throw 'Preservation checks failed.' }
}
