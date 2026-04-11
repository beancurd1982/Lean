param(
    [string]$RootPath = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$algorithmName = 'AegisGrowthAllocation'
$normalizedPattern = '^\d{4}-\d{2}-\d{2}_\d{6}__AegisGrowthAllocation__.+\.txt$'
$indexPath = Join-Path $RootPath 'log-index.csv'

function Ensure-LogIndex {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        return
    }

    @'
processed_at,created_at,original_name,normalized_name,status,review_note
'@ | Set-Content -LiteralPath $Path -Encoding ascii
}

function Get-IndexRows {
    param([string]$Path)

    Ensure-LogIndex -Path $Path
    $rows = Import-Csv -LiteralPath $Path
    return @($rows)
}

function Save-IndexRows {
    param(
        [string]$Path,
        [object[]]$Rows
    )

    $Rows |
        Sort-Object processed_at, normalized_name |
        Export-Csv -LiteralPath $Path -NoTypeInformation -Encoding ascii
}

function Get-SanitizedStem {
    param([string]$Stem)

    $value = $Stem.Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = 'unnamed-log'
    }

    $value = $value -replace '\s+', '-'
    $value = $value -replace '[^A-Za-z0-9_-]', '-'
    $value = $value -replace '-{2,}', '-'
    $value = $value.Trim('-')

    if ([string]::IsNullOrWhiteSpace($value)) {
        return 'unnamed-log'
    }

    return $value
}

function Wait-ForStableFile {
    param([System.IO.FileInfo]$File)

    $stableChecks = 0
    $lastLength = -1L

    for ($attempt = 0; $attempt -lt 15; $attempt++) {
        $File.Refresh()

        try {
            $stream = [System.IO.File]::Open($File.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
        }
        catch {
            Start-Sleep -Milliseconds 500
            continue
        }

        if ($File.Length -eq $lastLength) {
            $stableChecks++
        }
        else {
            $stableChecks = 0
            $lastLength = $File.Length
        }

        if ($stableChecks -ge 1) {
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "File did not become stable in time: $($File.FullName)"
}

function Get-NormalizedName {
    param(
        [System.IO.FileInfo]$File,
        [string]$AlgorithmName
    )

    $timestamp = $File.CreationTime.ToString('yyyy-MM-dd_HHmmss')
    $sanitizedStem = Get-SanitizedStem -Stem $File.BaseName
    return '{0}__{1}__{2}{3}' -f $timestamp, $AlgorithmName, $sanitizedStem, $File.Extension.ToLowerInvariant()
}

function Get-UniqueTargetPath {
    param(
        [string]$RootPath,
        [string]$FileName
    )

    $candidatePath = Join-Path $RootPath $FileName
    if (-not (Test-Path -LiteralPath $candidatePath)) {
        return $candidatePath
    }

    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    $extension = [System.IO.Path]::GetExtension($FileName)

    for ($index = 1; $index -lt 1000; $index++) {
        $candidateName = '{0}__{1:D2}{2}' -f $baseName, $index, $extension
        $candidatePath = Join-Path $RootPath $candidateName
        if (-not (Test-Path -LiteralPath $candidatePath)) {
            return $candidatePath
        }
    }

    throw "Could not generate a unique normalized filename for $FileName"
}

Ensure-LogIndex -Path $indexPath
$indexRows = New-Object System.Collections.Generic.List[object]
foreach ($row in (Get-IndexRows -Path $indexPath)) {
    [void]$indexRows.Add($row)
}

$files = Get-ChildItem -LiteralPath $RootPath -File -Filter '*.txt' |
    Where-Object { $_.Name -ne 'README.md' }

foreach ($file in $files) {
    Wait-ForStableFile -File $file

    $existingRow = $indexRows | Where-Object { $_.normalized_name -eq $file.Name } | Select-Object -First 1
    if ($file.Name -match $normalizedPattern) {
        if (-not $existingRow) {
            [void]$indexRows.Add([pscustomobject]@{
                processed_at    = (Get-Date).ToString('s')
                created_at      = $file.CreationTime.ToString('s')
                original_name   = $file.Name
                normalized_name = $file.Name
                status          = 'unreviewed'
                review_note     = ''
            })
        }

        continue
    }

    $normalizedName = Get-NormalizedName -File $file -AlgorithmName $algorithmName
    $targetPath = Get-UniqueTargetPath -RootPath $RootPath -FileName $normalizedName
    $targetName = Split-Path -Leaf $targetPath

    Rename-Item -LiteralPath $file.FullName -NewName $targetName

    [void]$indexRows.Add([pscustomobject]@{
        processed_at    = (Get-Date).ToString('s')
        created_at      = $file.CreationTime.ToString('s')
        original_name   = $file.Name
        normalized_name = $targetName
        status          = 'unreviewed'
        review_note     = ''
    })
}

Save-IndexRows -Path $indexPath -Rows $indexRows.ToArray()
