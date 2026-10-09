$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Speech

$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.SetOutputToDefaultAudioDevice()

function Set-Voice([string]$fragment) {
    if ([string]::IsNullOrWhiteSpace($fragment)) {
        $portugueseVoice = $synth.GetInstalledVoices() |
            Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'pt-BR' } |
            Select-Object -First 1
        if ($null -ne $portugueseVoice) {
            $synth.SelectVoice($portugueseVoice.VoiceInfo.Name)
        }
        return
    }

    $matchingVoice = $synth.GetInstalledVoices() |
        Where-Object {
            $_.Enabled -and (
                $_.VoiceInfo.Name.IndexOf($fragment, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                $_.VoiceInfo.Culture.Name.IndexOf($fragment, [StringComparison]::OrdinalIgnoreCase) -ge 0
            )
        } |
        Select-Object -First 1

    if ($null -ne $matchingVoice) {
        $synth.SelectVoice($matchingVoice.VoiceInfo.Name)
    }
    else {
        [Console]::Error.WriteLine("Voz nao encontrada: $fragment. Sera usada a voz padrao do Windows.")
    }
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
        'SAY' {
            if (-not [string]::IsNullOrWhiteSpace($argument)) {
                $synth.SpeakAsyncCancelAll()
                $null = $synth.SpeakAsync($argument)
            }
        }
        'VOICE' { Set-Voice $argument }
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
    Set-Voice ''
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