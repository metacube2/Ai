param(
    [string[]]$SqlInstance = @(),
    [string]$OutputDirectory = (Join-Path $env:USERPROFILE "Desktop"),
    [switch]$ScanProgramFiles
)

$ErrorActionPreference = "Continue"

function Add-Section {
    param([string]$Title)

    $script:reportLines += ""
    $script:reportLines += "============================================================"
    $script:reportLines += $Title
    $script:reportLines += "============================================================"
}

function Add-Line {
    param([string]$Text = "")

    $script:reportLines += $Text
}

function Read-RegistryValues {
    param([string]$Path)

    try {
        if (Test-Path $Path) {
            return Get-ItemProperty -Path $Path -ErrorAction Stop
        }
    }
    catch {
        Add-Line "Registry read failed: $Path :: $($_.Exception.Message)"
    }

    return $null
}

function Get-SageUninstallEntries {
    $paths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    foreach ($path in $paths) {
        Get-ItemProperty -Path $path -ErrorAction SilentlyContinue |
            Where-Object {
                $_.DisplayName -and (
                    $_.DisplayName -match "Sage" -or
                    $_.Publisher -match "Sage"
                )
            } |
            Select-Object DisplayName, DisplayVersion, Publisher, InstallDate, InstallLocation, UninstallString, PSPath
    }
}

function Get-SageFileVersions {
    $roots = @(
        $env:ProgramFiles,
        ${env:ProgramFiles(x86)},
        $env:ProgramData
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $roots) {
        Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "Sage" } |
            ForEach-Object {
                Get-ChildItem -LiteralPath $_.FullName -Recurse -File -Include *.exe,*.dll -ErrorAction SilentlyContinue |
                    Where-Object { $_.VersionInfo.ProductName -match "Sage" -or $_.VersionInfo.CompanyName -match "Sage" -or $_.Name -match "Sage" } |
                    Select-Object FullName,
                        @{Name="FileVersion"; Expression={$_.VersionInfo.FileVersion}},
                        @{Name="ProductVersion"; Expression={$_.VersionInfo.ProductVersion}},
                        @{Name="ProductName"; Expression={$_.VersionInfo.ProductName}},
                        @{Name="CompanyName"; Expression={$_.VersionInfo.CompanyName}}
            }
    }
}

function Get-SqlInstancesFromRegistry {
    $instanceRoots = @(
        "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    )

    $instances = New-Object System.Collections.Generic.List[object]

    foreach ($root in $instanceRoots) {
        $values = Read-RegistryValues $root
        if (-not $values) {
            continue
        }

        $values.PSObject.Properties |
            Where-Object { $_.Name -notlike "PS*" } |
            ForEach-Object {
                $instanceName = $_.Name
                $instanceId = [string]$_.Value
                $setupPaths = @(
                    "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\Setup",
                    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\$instanceId\Setup"
                )

                foreach ($setupPath in $setupPaths) {
                    $setup = Read-RegistryValues $setupPath
                    if ($setup) {
                        $instances.Add([pscustomobject]@{
                            InstanceName = $instanceName
                            InstanceId = $instanceId
                            Edition = $setup.Edition
                            Version = $setup.Version
                            PatchLevel = $setup.PatchLevel
                            ProductCode = $setup.ProductCode
                            SQLPath = $setup.SQLPath
                            SetupPath = $setupPath
                        })
                    }
                }
            }
    }

    return $instances
}

function Get-SqlServices {
    Get-CimInstance Win32_Service -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "MSSQL*" -or $_.Name -like "SQLAgent*" -or $_.DisplayName -like "*SQL Server*" } |
        Select-Object Name, DisplayName, State, StartMode, PathName
}

function Resolve-Sqlcmd {
    $cmd = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $candidates = @(
        "$env:ProgramFiles\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\sqlcmd.exe",
        "$env:ProgramFiles\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe"
    )

    foreach ($candidate in $candidates) {
        $match = Get-ChildItem -Path $candidate -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($match) {
            return $match.FullName
        }
    }

    return $null
}

function Invoke-SqlVersionQuery {
    param(
        [string]$SqlcmdPath,
        [string]$Instance
    )

    $query = @"
SET NOCOUNT ON;
SELECT
    @@VERSION AS FullVersion,
    SERVERPROPERTY('ProductVersion') AS ProductVersion,
    SERVERPROPERTY('ProductLevel') AS ProductLevel,
    SERVERPROPERTY('Edition') AS Edition,
    SERVERPROPERTY('EngineEdition') AS EngineEdition,
    SERVERPROPERTY('MachineName') AS MachineName,
    SERVERPROPERTY('ServerName') AS ServerName,
    SERVERPROPERTY('InstanceName') AS InstanceName,
    SERVERPROPERTY('Collation') AS Collation;
"@

    $tempQuery = Join-Path $env:TEMP ("sql_version_query_{0}.sql" -f ([guid]::NewGuid().ToString("N")))
    Set-Content -LiteralPath $tempQuery -Value $query -Encoding UTF8

    try {
        $output = & $SqlcmdPath -S $Instance -E -i $tempQuery -W -s "|" -b 2>&1
        [pscustomobject]@{
            Instance = $Instance
            Success = $LASTEXITCODE -eq 0
            Output = ($output -join [Environment]::NewLine)
        }
    }
    catch {
        [pscustomobject]@{
            Instance = $Instance
            Success = $false
            Output = $_.Exception.Message
        }
    }
    finally {
        Remove-Item -LiteralPath $tempQuery -Force -ErrorAction SilentlyContinue
    }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$reportPath = Join-Path $OutputDirectory "Sage_SQL_Environment_$timestamp.txt"
$jsonPath = Join-Path $OutputDirectory "Sage_SQL_Environment_$timestamp.json"
$reportLines = New-Object System.Collections.Generic.List[string]

$computer = Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue
$os = Get-CimInstance Win32_OperatingSystem -ErrorAction SilentlyContinue
$sageEntries = @(Get-SageUninstallEntries)
$sqlRegistryInstances = @(Get-SqlInstancesFromRegistry)
$sqlServices = @(Get-SqlServices)
$sageFileVersions = @()

if ($ScanProgramFiles) {
    $sageFileVersions = @(Get-SageFileVersions)
}

$sqlcmdPath = Resolve-Sqlcmd
$queryInstances = New-Object System.Collections.Generic.List[string]

foreach ($instance in $SqlInstance) {
    if (-not [string]::IsNullOrWhiteSpace($instance)) {
        $queryInstances.Add($instance)
    }
}

if ($queryInstances.Count -eq 0) {
    $machineName = $env:COMPUTERNAME
    foreach ($instance in $sqlRegistryInstances) {
        if ($instance.InstanceName -eq "MSSQLSERVER") {
            $queryInstances.Add("localhost")
        }
        else {
            $queryInstances.Add("localhost\$($instance.InstanceName)")
        }
    }
}

$queryInstances = @($queryInstances | Select-Object -Unique)
$sqlQueryResults = @()
if ($sqlcmdPath -and $queryInstances.Count -gt 0) {
    foreach ($instance in $queryInstances) {
        $sqlQueryResults += Invoke-SqlVersionQuery -SqlcmdPath $sqlcmdPath -Instance $instance
    }
}

Add-Section "Capture metadata"
Add-Line "Timestamp: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")"
Add-Line "Computer: $env:COMPUTERNAME"
Add-Line "User: $env:USERDOMAIN\$env:USERNAME"
Add-Line "Output text: $reportPath"
Add-Line "Output json: $jsonPath"

Add-Section "Windows / machine"
Add-Line "Manufacturer: $($computer.Manufacturer)"
Add-Line "Model: $($computer.Model)"
Add-Line "OS: $($os.Caption)"
Add-Line "OS Version: $($os.Version)"
Add-Line "OS Build: $($os.BuildNumber)"
Add-Line "Install date: $($os.InstallDate)"

Add-Section "Sage entries from installed programs"
if ($sageEntries.Count -eq 0) {
    Add-Line "No Sage uninstall entries found."
}
else {
    $sageEntries | Format-List | Out-String | ForEach-Object { Add-Line $_.TrimEnd() }
}

Add-Section "Sage file versions"
if (-not $ScanProgramFiles) {
    Add-Line "Skipped. Re-run with -ScanProgramFiles for file version scan."
}
elseif ($sageFileVersions.Count -eq 0) {
    Add-Line "No Sage file versions found under Program Files / ProgramData."
}
else {
    $sageFileVersions | Sort-Object ProductName, ProductVersion, FullName | Format-Table -AutoSize | Out-String | ForEach-Object { Add-Line $_.TrimEnd() }
}

Add-Section "SQL Server instances from registry"
if ($sqlRegistryInstances.Count -eq 0) {
    Add-Line "No SQL Server registry instances found."
}
else {
    $sqlRegistryInstances | Format-List | Out-String | ForEach-Object { Add-Line $_.TrimEnd() }
}

Add-Section "SQL Server services"
if ($sqlServices.Count -eq 0) {
    Add-Line "No SQL Server services found."
}
else {
    $sqlServices | Format-Table -AutoSize | Out-String | ForEach-Object { Add-Line $_.TrimEnd() }
}

Add-Section "SQL Server live query"
Add-Line "sqlcmd path: $(if ($sqlcmdPath) { $sqlcmdPath } else { "not found" })"
if (-not $sqlcmdPath) {
    Add-Line "Cannot query SQL Server live because sqlcmd.exe was not found."
}
elseif ($queryInstances.Count -eq 0) {
    Add-Line "No SQL instances to query. Pass -SqlInstance server\instance if needed."
}
else {
    foreach ($result in $sqlQueryResults) {
        Add-Line ""
        Add-Line "Instance: $($result.Instance)"
        Add-Line "Success: $($result.Success)"
        Add-Line $result.Output
    }
}

$data = [pscustomobject]@{
    CapturedAt = (Get-Date).ToString("o")
    ComputerName = $env:COMPUTERNAME
    UserName = "$env:USERDOMAIN\$env:USERNAME"
    Windows = [pscustomobject]@{
        Caption = $os.Caption
        Version = $os.Version
        BuildNumber = $os.BuildNumber
        InstallDate = $os.InstallDate
    }
    SageUninstallEntries = $sageEntries
    SageFileVersions = $sageFileVersions
    SqlRegistryInstances = $sqlRegistryInstances
    SqlServices = $sqlServices
    SqlcmdPath = $sqlcmdPath
    SqlQueryResults = $sqlQueryResults
}

$reportLines | Set-Content -LiteralPath $reportPath -Encoding UTF8
$data | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

Write-Host "Created:"
Write-Host "  $reportPath"
Write-Host "  $jsonPath"
