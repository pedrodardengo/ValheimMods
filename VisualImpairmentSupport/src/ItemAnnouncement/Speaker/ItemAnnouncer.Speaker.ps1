param([switch]$ListVoices)

$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Speech

if ($ListVoices) {
    $voiceCatalog = New-Object System.Speech.Synthesis.SpeechSynthesizer
    try {
        foreach ($installedVoice in $voiceCatalog.GetInstalledVoices()) {
            if ($installedVoice.Enabled) {
                $displayName = '{0} [{1}]' -f $installedVoice.VoiceInfo.Name, $installedVoice.VoiceInfo.Culture.Name
                [Console]::Out.WriteLine([Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($displayName)))
            }
        }
    }
    finally {
        $voiceCatalog.Dispose()
    }
    exit 0
}

$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.SetOutputToDefaultAudioDevice()

function Set-DefaultVoice {
    $portugueseVoice = $synth.GetInstalledVoices() |
        Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'pt-BR' } |
        Select-Object -First 1
    if ($null -ne $portugueseVoice) {
        $synth.SelectVoice($portugueseVoice.VoiceInfo.Name)
    }
}

function Set-Voice([string]$selection) {
    if ([string]::IsNullOrWhiteSpace($selection) -or $selection -eq 'Automatic (Portuguese if available)') {
        Set-DefaultVoice
        return
    }

    foreach ($installedVoice in $synth.GetInstalledVoices()) {
        if (-not $installedVoice.Enabled) { continue }
        $displayName = '{0} [{1}]' -f $installedVoice.VoiceInfo.Name, $installedVoice.VoiceInfo.Culture.Name
        if ($displayName -eq $selection) {
            $synth.SelectVoice($installedVoice.VoiceInfo.Name)
            return
        }
    }

    Set-DefaultVoice
}

function Handle-Command([string]$line) {
    $separator = $line.IndexOf(' ')
    if ($separator -lt 0) {
        $command = $line
        $argument = ''
    }
    else {
        $command = $line.Substring(0, $separator)
        $argument = $line.Substring($separator + 1)
    }

    switch ($command) {
        'VOICE' { Set-Voice $argument }
        'SAY' {
            if (-not [string]::IsNullOrWhiteSpace($argument)) {
                $synth.SpeakAsyncCancelAll()
                $null = $synth.SpeakAsync($argument)
            }
        }
        'RATE' {
            $parsed = 0
            if ([int]::TryParse($argument, [ref]$parsed)) {
                $synth.Rate = [Math]::Max(-10, [Math]::Min(10, $parsed))
            }
        }
        'VOLUME' {
            $parsed = 100
            if ([int]::TryParse($argument, [ref]$parsed)) {
                $synth.Volume = [Math]::Max(0, [Math]::Min(100, $parsed))
            }
        }
        'QUIT' { $synth.SpeakAsyncCancelAll(); return $false }
        default { [Console]::Error.WriteLine("Comando desconhecido: $command") }
    }
    return $true
}

try {
    Set-DefaultVoice
    [Console]::Out.WriteLine('READY')
    [Console]::Out.Flush()
    while ($null -ne ($line = [Console]::In.ReadLine())) {
        if (-not (Handle-Command $line)) { break }
    }
}
finally {
    $synth.SpeakAsyncCancelAll()
    $synth.Dispose()
}