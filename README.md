# Gera

Marketplace de serviços gerais. A solução usa .NET 10, ASP.NET Core Identity, SQL Server, React e Vite.

## Pré-requisitos

- .NET 10 SDK
- SQL Server (LocalDB ou instância compatível com SQL Server e NetTopologySuite)
- Node.js 20.19+ ou 22.12+ e npm

## Configuração local (User Secrets)

Na raiz do repositório:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=GeraDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project src/Gera.Api/Gera.Api.csproj
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 48
$rng.GetBytes($bytes)
$jwtSecret = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "JwtSettings:Secret" $jwtSecret --project src/Gera.Api/Gera.Api.csproj
dotnet user-secrets set "Frontend:Origin" "http://localhost:5173" --project src/Gera.Api/Gera.Api.csproj
```

`appsettings.json` não contém connection string ou segredo JWT. Em produção, injete esses valores por um cofre de segredos ou variáveis de ambiente.

## Banco de dados

Aplicar todas as migrations (incluindo `InitialCreate` e `AddRefreshTokens`):

```powershell
dotnet ef database update --project src/Gera.Infrastructure/Gera.Infrastructure.csproj --startup-project src/Gera.Api/Gera.Api.csproj --context AppDbContext
```

Para conferir tabelas, índice espacial e seed, conecte ao banco `GeraDb`; a tabela `__EFMigrationsHistory` registra as migrations aplicadas. As categorias iniciais são configuradas em `CategoriaConfiguration`.

## Executar API e web

```powershell
dotnet restore Gera.sln
dotnet build Gera.sln
dotnet run --project src/Gera.Api/Gera.Api.csproj --launch-profile http
```

A API HTTP local usa `http://localhost:5215`; Swagger fica em `/swagger` e health check em `/api/health`.

Em outro terminal:

```powershell
cd web
npm install
npm run dev
```

Abra `http://localhost:5173`. O servidor Vite encaminha `/api` para a API local. CORS está configurado para `localhost:5173` e `127.0.0.1:5173` quando a API é chamada diretamente.

## Verificação

```powershell
dotnet test Gera.sln
cd web
npm run build
```

Os detalhes dos endpoints estão em [docs/API.md](docs/API.md); as políticas em [docs/REGRAS-DE-NEGOCIO.md](docs/REGRAS-DE-NEGOCIO.md); a decisão de armazenamento de tokens em [docs/DECISOES.md](docs/DECISOES.md).
