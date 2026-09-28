#Requires -Version 7.0
param(
    [string]$Wix = '',
    [string]$Version = '11.1.1',
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$localWix = Join-Path (Split-Path $repo) '.local-tools\wix\wix.exe'
if (-not $Wix) {
    if (Test-Path -LiteralPath $localWix) { $Wix = $localWix }
    else { $Wix = (Get-Command wix -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source) }
}
$stage = Join-Path $repo 'bin\MsiStage\adm-app'
$output = Join-Path $repo 'bin\Msi\AutoDarkMode-Ambient-x64.msi'
$source = Join-Path $repo 'bin\Msi\obj\AutoDarkMode-Ambient.wxs'

if (-not $Wix -or -not (Test-Path -LiteralPath $Wix)) { throw "WiX executable not found: $Wix" }
if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $stage) {
        $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
        $resolvedBin = (Resolve-Path -LiteralPath (Join-Path $repo 'bin')).Path
        if (-not $resolvedStage.StartsWith($resolvedBin + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean staging path outside repository bin: $resolvedStage"
        }
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
    & dotnet restore (Join-Path $repo 'AutoDarkModeSvc\AutoDarkModeSvc.csproj') '-p:Platform=x64' '-p:RuntimeIdentifier=win-x64' '--verbosity' 'quiet'
    if ($LASTEXITCODE) { throw 'Service restore failed.' }
    & dotnet restore (Join-Path $repo 'AutoDarkModeApp\AutoDarkModeApp.csproj') '-p:Platform=x64' '-p:RuntimeIdentifier=win-x64' '-p:RuntimeIdentifiers=win-x64' '--verbosity' 'quiet'
    if ($LASTEXITCODE) { throw 'Settings app restore failed.' }
    & dotnet restore (Join-Path $repo 'AutoDarkModeShell\AutoDarkModeShell.csproj') '-p:Platform=x64' '-p:RuntimeIdentifier=win-x64' '--verbosity' 'quiet'
    if ($LASTEXITCODE) { throw 'Shell restore failed.' }
    $common = @('-c', 'Release', '--no-restore', '-p:Platform=x64', '-p:RuntimeIdentifier=win-x64', '-p:SelfContained=true', '-p:EnableRuntimePackDownload=false', '--verbosity', 'quiet')
    & dotnet publish (Join-Path $repo 'AutoDarkModeSvc\AutoDarkModeSvc.csproj') @common '-p:PublishDir=..\bin\MsiStage\adm-app\core\'
    if ($LASTEXITCODE) { throw 'Service self-contained publish failed.' }
    & dotnet publish (Join-Path $repo 'AutoDarkModeApp\AutoDarkModeApp.csproj') @common '-p:RuntimeIdentifiers=win-x64' '-p:PublishDir=..\bin\MsiStage\adm-app\ui\'
    if ($LASTEXITCODE) { throw 'Settings app self-contained publish failed.' }
    & dotnet publish (Join-Path $repo 'AutoDarkModeShell\AutoDarkModeShell.csproj') @common '-p:PublishSingleFile=true' '-p:PublishDir=..\bin\MsiStage\shell\'
    if ($LASTEXITCODE) { throw 'Shell self-contained publish failed.' }
    Copy-Item -LiteralPath (Join-Path $repo 'bin\MsiStage\shell\AutoDarkModeShell.exe') -Destination (Join-Path $stage 'core\AutoDarkModeShell.exe') -Force
}

$service = Join-Path $stage 'core\AutoDarkModeSvc.exe'
$app = Join-Path $stage 'ui\AutoDarkModeApp.exe'
$uf2 = Join-Path $stage 'ui\Assets\Firmware\adaptive-brightness.uf2'
$shell = Join-Path $stage 'core\AutoDarkModeShell.exe'
foreach ($required in @($service, $app, $uf2, $shell)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required payload is missing: $required" }
}

function StableId([string]$value) {
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
    return 'I' + [Convert]::ToHexString($hash).Substring(0, 20)
}

function StableGuid([string]$value) {
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($value.ToLowerInvariant()))
    $hex = [Convert]::ToHexString($hash).Substring(0, 32)
    return "$($hex.Substring(0,8))-$($hex.Substring(8,4))-$($hex.Substring(12,4))-$($hex.Substring(16,4))-$($hex.Substring(20,12))"
}

$files = @(Get-ChildItem -LiteralPath $stage -Recurse -File | Where-Object { $_.Extension -ne '.pdb' } | Sort-Object FullName)
$directories = @{}
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($stage, $file.FullName).Replace('/', '\')
    $parts = $relative.Split('\')
    for ($i = 0; $i -lt $parts.Length - 1; $i++) {
        $path = ($parts[0..$i] -join '\')
        $directories[$path] = $parts[$i]
    }
}

New-Item -ItemType Directory -Force -Path (Split-Path $source), (Split-Path $output) | Out-Null
$settings = [Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.Encoding = [Text.UTF8Encoding]::new($false)
$xml = [Xml.XmlWriter]::Create($source, $settings)
try {
    $ns = 'http://wixtoolset.org/schemas/v4/wxs'
    $xml.WriteStartDocument()
    $xml.WriteStartElement('Wix', $ns)
    $xml.WriteStartElement('Package', $ns)
    foreach ($pair in @{
        Name='Auto Dark Mode (Ambient Light)'; Manufacturer='NeuronCState'; Version=$Version;
        UpgradeCode='8847501D-CBB8-4A7E-A1F3-2D388C3CA10F'; Scope='perUser'
    }.GetEnumerator()) { $xml.WriteAttributeString($pair.Key, [string]$pair.Value) }
    $xml.WriteStartElement('MajorUpgrade', $ns)
    $xml.WriteAttributeString('DowngradeErrorMessage', 'A newer Auto Dark Mode (Ambient Light) version is already installed.')
    $xml.WriteEndElement()
    $xml.WriteStartElement('MediaTemplate', $ns)
    $xml.WriteAttributeString('EmbedCab', 'yes')
    $xml.WriteEndElement()

    $xml.WriteStartElement('StandardDirectory', $ns)
    $xml.WriteAttributeString('Id', 'LocalAppDataFolder')
    foreach ($dir in @(@('PROGRAMS', 'Programs'), @('INSTALLFOLDER', 'AutoDarkMode-Ambient'), @('ADMAPPRoot', 'adm-app'))) {
        $xml.WriteStartElement('Directory', $ns)
        $xml.WriteAttributeString('Id', $dir[0])
        $xml.WriteAttributeString('Name', $dir[1])
    }
    function Write-Children([string]$parent) {
        $children = @($directories.Keys | Where-Object {
            $prefix = if ($parent) { "$parent\" } else { '' }
            $_.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -and
            $_.Substring($prefix.Length).IndexOf('\') -lt 0
        } | Sort-Object)
        foreach ($path in $children) {
            $xml.WriteStartElement('Directory', $ns)
            $xml.WriteAttributeString('Id', (StableId "dir/$path"))
            $xml.WriteAttributeString('Name', $directories[$path])
            Write-Children $path
            $xml.WriteEndElement()
        }
    }
    Write-Children ''
    for ($i = 0; $i -lt 3; $i++) { $xml.WriteEndElement() }
    $xml.WriteEndElement()

    $xml.WriteStartElement('Feature', $ns)
    $xml.WriteAttributeString('Id', 'Complete')
    $xml.WriteAttributeString('Title', 'Auto Dark Mode (Ambient Light)')
    $xml.WriteAttributeString('Level', '1')
    $xml.WriteStartElement('ComponentGroupRef', $ns)
    $xml.WriteAttributeString('Id', 'Payload')
    $xml.WriteEndElement()
    $xml.WriteEndElement()

    $xml.WriteStartElement('ComponentGroup', $ns)
    $xml.WriteAttributeString('Id', 'Payload')
    $xml.WriteStartElement('Component', $ns)
    $xml.WriteAttributeString('Id', 'DirectoryCleanup')
    $xml.WriteAttributeString('Guid', (StableGuid 'component/directory-cleanup'))
    $xml.WriteAttributeString('Directory', 'INSTALLFOLDER')
    foreach ($directoryId in @('PROGRAMS', 'INSTALLFOLDER', 'ADMAPPRoot') + @($directories.Keys | Sort-Object | ForEach-Object { StableId "dir/$_" })) {
        $xml.WriteStartElement('RemoveFolder', $ns)
        $xml.WriteAttributeString('Id', "Remove_$directoryId")
        $xml.WriteAttributeString('Directory', $directoryId)
        $xml.WriteAttributeString('On', 'uninstall')
        $xml.WriteEndElement()
    }
    $xml.WriteStartElement('RegistryValue', $ns)
    $xml.WriteAttributeString('Root', 'HKCU')
    $xml.WriteAttributeString('Key', 'Software\NeuronCState\AutoDarkModeAmbient')
    $xml.WriteAttributeString('Name', 'DirectoryCleanup')
    $xml.WriteAttributeString('Value', '1')
    $xml.WriteAttributeString('Type', 'integer')
    $xml.WriteAttributeString('KeyPath', 'yes')
    $xml.WriteEndElement()
    $xml.WriteEndElement()
    foreach ($file in $files) {
        $relative = [IO.Path]::GetRelativePath($stage, $file.FullName).Replace('/', '\')
        $id = StableId "file/$relative"
        $dir = [IO.Path]::GetDirectoryName($relative).Replace('/', '\')
        $xml.WriteStartElement('Component', $ns)
        $xml.WriteAttributeString('Id', "C_$id")
        $xml.WriteAttributeString('Guid', (StableGuid "component/$relative"))
        $xml.WriteAttributeString('Directory', (StableId "dir/$dir"))
        $xml.WriteStartElement('File', $ns)
        $xml.WriteAttributeString('Id', "F_$id")
        $xml.WriteAttributeString('Source', $file.FullName)
        if ($relative -ieq 'ui\AutoDarkModeApp.exe') {
            $xml.WriteStartElement('Shortcut', $ns)
            $xml.WriteAttributeString('Id', 'StartMenuShortcut')
            $xml.WriteAttributeString('Name', 'Auto Dark Mode (Ambient Light)')
            $xml.WriteAttributeString('Directory', 'ProgramMenuFolder')
            $xml.WriteAttributeString('WorkingDirectory', (StableId 'dir/ui'))
            $xml.WriteEndElement()
        }
        $xml.WriteEndElement()
        $xml.WriteStartElement('RegistryValue', $ns)
        $xml.WriteAttributeString('Root', 'HKCU')
        $xml.WriteAttributeString('Key', 'Software\NeuronCState\AutoDarkModeAmbient\InstalledFiles')
        $xml.WriteAttributeString('Name', $id)
        $xml.WriteAttributeString('Value', '1')
        $xml.WriteAttributeString('Type', 'integer')
        $xml.WriteAttributeString('KeyPath', 'yes')
        $xml.WriteEndElement()
        $xml.WriteEndElement()
    }
    $xml.WriteEndElement()
    $xml.WriteEndElement()
    $xml.WriteEndElement()
    $xml.WriteEndDocument()
} finally { $xml.Dispose() }

& $Wix build -arch x64 -out $output $source
if ($LASTEXITCODE) { throw 'WiX MSI build failed.' }
Write-Output "MSI: $output"
Write-Output "Payload files: $($files.Count)"
