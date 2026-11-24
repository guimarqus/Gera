$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-Checked {
    param([string]$Command, [string[]]$ArgumentList)

    & $Command @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($ArgumentList -join ' ') falhou com codigo $LASTEXITCODE."
    }
}

$schemaCheck = @'
SET NOCOUNT ON;
IF NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261107211323_InitialCreate'
) OR NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20261107212935_AddRefreshTokens'
) THROW 51000, 'Migrations esperadas ausentes.', 1;

IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo')
    AND name IN (N'Usuarios', N'Papeis', N'UsuarioPapeis', N'UsuarioClaims',
    N'UsuarioLogins', N'PapelClaims', N'UsuarioTokens', N'Categorias',
    N'Ofertas', N'OfertaFotos', N'OfertaTags', N'Favoritos', N'Solicitacoes',
    N'SolicitacaoHistoricos', N'Orcamentos', N'Mensagens', N'Avaliacoes',
    N'Notificacoes', N'Denuncias', N'Bloqueios', N'Pagamentos', N'RefreshTokens')) <> 22
    THROW 51001, 'Tabelas esperadas ausentes.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Ofertas')
      AND name = N'Localizacao' AND TYPE_NAME(user_type_id) = N'geography'
) THROW 51002, 'Coluna geography ausente.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.spatial_indexes
    WHERE object_id = OBJECT_ID(N'dbo.Ofertas')
      AND name = N'SPATIAL_IX_Ofertas_Localizacao' AND is_disabled = 0
) THROW 51003, 'Indice espacial ausente ou desabilitado.', 1;

IF EXISTS (SELECT 1 FROM dbo.Ofertas WHERE Localizacao.STSrid <> 4326)
    THROW 51004, 'Uma oferta tem SRID diferente de 4326.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Categorias WHERE Id = 1)
    THROW 51005, 'Seed de categorias ausente.', 1;

SELECT COUNT(*) AS Categorias FROM dbo.Categorias;
SELECT COUNT(*) AS OfertasComPontoValidado FROM dbo.Ofertas;
'@

Invoke-Checked sqlcmd @('-S', '(localdb)\MSSQLLocalDB', '-d', 'GeraDev', '-E', '-C', '-b', '-Q', $schemaCheck)
$testArtifacts = Join-Path ([IO.Path]::GetTempPath()) 'GeraQualityArtifacts'
Invoke-Checked dotnet @('test', 'Gera.sln', '--artifacts-path', $testArtifacts, '--consoleLoggerParameters:ErrorsOnly;Summary')

if (-not (Get-Command npm.cmd -ErrorAction SilentlyContinue)) {
    $nodeDir = 'C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Microsoft\VisualStudio\NodeJs'
    if (Test-Path -LiteralPath (Join-Path $nodeDir 'npm.cmd')) {
        $env:Path = "$nodeDir;$env:Path"
    }
}

Invoke-Checked npm.cmd @('--prefix', 'web', 'install', '--include=dev')
Invoke-Checked npm.cmd @('--prefix', 'web', 'run', 'test')
Invoke-Checked npm.cmd @('--prefix', 'web', 'run', 'build')
