[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('TrueWeatherSwamp', 'VisualImpairmentSupport', 'All')]
    [string]$Mod,

    [string]$ValheimDir,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot

function Find-DotNet {
    $candidates = @()
    $command = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        $candidates += $command.Source
    }

    foreach ($root in @(
        $env:DOTNET_ROOT,
        $env:ProgramFiles,
        ${env:ProgramFiles(x86)},
        (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'),
        (Join-Path $HOME '.dotnet')
    )) {
        if (-not [string]::IsNullOrWhiteSpace($root)) {
            $candidates += (Join-Path $root 'dotnet.exe')
        }
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw 'Nao foi encontrado dotnet.exe. Instale o .NET SDK ou adicione dotnet ao PATH.'
}

function Invoke-ModPackage([string]$modName, [string]$dotnetPath) {
    $modDirectory = Join-Path $repositoryRoot $modName
    $projectPath = Join-Path $modDirectory "$modName.csproj"
    $manifestPath = Join-Path $modDirectory 'manifest.json'

    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Projeto nao encontrado: $projectPath"
    }
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Manifest nao encontrado: $manifestPath"
    }

    $buildArguments = @('build', $projectPath, '--configuration', $Configuration, '--nologo')
    if (-not [string]::IsNullOrWhiteSpace($ValheimDir)) {
        $buildArguments += "-p:ValheimDir=$ValheimDir"
    }

    Write-Host "Compilando $modName ($Configuration)..."
    & $dotnetPath @buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "A compilacao de $modName falhou (codigo $LASTEXITCODE)."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $outputDirectory = Join-Path $modDirectory "bin\$Configuration\netstandard2.1"
    $packageFiles = @('manifest.json', 'README.md', 'CHANGELOG.md', 'icon.png')
    foreach ($file in $packageFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $modDirectory $file) -PathType Leaf)) {
            throw "Arquivo necessario ao pacote nao encontrado: $modName\$file"
        }
    }

    $packageFiles += "$modName.dll"
    if ($modName -eq 'VisualImpairmentSupport') {
        $packageFiles += @('image.png', 'ItemAnnouncer.Speaker.ps1')
    }

    $stagingDirectory = Join-Path ([IO.Path]::GetTempPath()) "ValheimMods-$modName-$([guid]::NewGuid().ToString('N'))"
    $archivePath = Join-Path $modDirectory "$modName-$($manifest.version_number).zip"
    $temporaryArchivePath = Join-Path $modDirectory ".$modName-$([guid]::NewGuid().ToString('N')).tmp.zip"

    try {
        New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
        foreach ($file in $packageFiles) {
            $sourcePath = if ($file -eq "$modName.dll" -or $file -eq 'ItemAnnouncer.Speaker.ps1') {
                Join-Path $outputDirectory $file
            }
            else {
                Join-Path $modDirectory $file
            }

            if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
                throw "Arquivo necessario ao pacote nao encontrado apos a compilacao: $sourcePath"
            }
            Copy-Item -LiteralPath $sourcePath -Destination $stagingDirectory
        }

        Compress-Archive -Path (Join-Path $stagingDirectory '*') -DestinationPath $temporaryArchivePath -Force
        Move-Item -LiteralPath $temporaryArchivePath -Destination $archivePath -Force
        Write-Host "Pacote criado: $archivePath"
    }
    finally {
        if (Test-Path -LiteralPath $stagingDirectory) {
            Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
        }
        if (Test-Path -LiteralPath $temporaryArchivePath) {
            Remove-Item -LiteralPath $temporaryArchivePath -Force
        }
    }
}

$dotnet = Find-DotNet
$modsToPackage = if ($Mod -eq 'All') {
    @('TrueWeatherSwamp', 'VisualImpairmentSupport')
}
else {
    @($Mod)
}

foreach ($modName in $modsToPackage) {
    Invoke-ModPackage -modName $modName -dotnetPath $dotnet
}