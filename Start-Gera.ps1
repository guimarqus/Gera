$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string]$Command, [string[]]$ArgumentList)

    & $Command @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($ArgumentList -join ' ') falhou com codigo $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    Invoke-Checked SqlLocalDB @('start', 'MSSQLLocalDB')
    Invoke-Checked sqlcmd @('-S', '(localdb)\MSSQLLocalDB', '-E', '-C', '-l', '5', '-b', '-Q', 'SET NOCOUNT ON; SELECT 1;')
    $env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=GeraDev;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5'
}

if ([string]::IsNullOrWhiteSpace($env:JwtSettings__Secret)) {
    $bytes = New-Object byte[] 48
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        $env:JwtSettings__Secret = [Convert]::ToBase64String($bytes)
    } finally {
        $generator.Dispose()
    }
}

if (-not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) {
    $nodeDir = 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Microsoft\VisualStudio\NodeJs'
    if (Test-Path -LiteralPath (Join-Path $nodeDir 'npm.cmd')) {
        $env:Path = "$nodeDir;$env:Path"
    }
}

$listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
foreach ($port in 5215, 5173) {
    if ($listeners.Port -contains $port) {
        throw "A porta $port ja esta em uso. Encerre a instancia anterior do Gera antes de iniciar."
    }
}

Invoke-Checked dotnet @('tool', 'restore')
Invoke-Checked dotnet @('build', 'Gera.sln', '--consoleLoggerParameters:ErrorsOnly')
Invoke-Checked dotnet @('ef', 'database', 'update', '--no-build', '--project', 'src/Gera.Infrastructure', '--startup-project', 'src/Gera.Api')

if (-not (Test-Path -LiteralPath 'web/node_modules/vite/dist/node/cli.js')) {
    Invoke-Checked npm.cmd @('--prefix', 'web', 'install', '--omit=dev', '--no-package-lock')
}

$api = Start-Process -FilePath dotnet -ArgumentList @('run', '--no-build', '--project', 'src/Gera.Api', '--launch-profile', 'http') -WindowStyle Hidden -PassThru
try {
    $ready = $false
    for ($attempt = 0; $attempt -lt 20 -and -not $api.HasExited; $attempt++) {
        try {
            Invoke-RestMethod 'http://localhost:5215/api/categorias' -TimeoutSec 3 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if (-not $ready) {
        throw 'A API nao conseguiu carregar as categorias do SQL Server.'
    }
    Invoke-Checked npm.cmd @('--prefix', 'web', 'run', 'dev', '--', '--host', '127.0.0.1', '--strictPort')
} finally {
    if (-not $api.HasExited) {
        & taskkill.exe /PID $api.Id /T /F
    }
}
