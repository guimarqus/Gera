# Gera

Marketplace de serviÃ§os gerais. A soluÃ§Ã£o usa .NET 10, ASP.NET Core Identity, SQL Server, React e Vite.

## PrÃ©-requisitos

- .NET 10 SDK
- SQL Server (LocalDB ou instÃ¢ncia compatÃ­vel com SQL Server e NetTopologySuite)
- Node.js 20.19+ ou 22.12+ e npm

## ConfiguraÃ§Ã£o local (User Secrets)

Na raiz do repositÃ³rio:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=GeraDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project src/Gera.Api/Gera.Api.csproj
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 48
$rng.GetBytes($bytes)
$jwtSecret = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "JwtSettings:Secret" $jwtSecret --project src/Gera.Api/Gera.Api.csproj
dotnet user-secrets set "Frontend:Origin" "http://localhost:5173" --project src/Gera.Api/Gera.Api.csproj
```

`appsettings.json` nÃ£o contÃ©m connection string ou segredo JWT. Em produÃ§Ã£o, injete esses valores por um cofre de segredos ou variÃ¡veis de ambiente.

## Banco de dados

Aplicar todas as migrations (incluindo `InitialCreate` e `AddRefreshTokens`):

```powershell
dotnet ef database update --project src/Gera.Infrastructure/Gera.Infrastructure.csproj --startup-project src/Gera.Api/Gera.Api.csproj --context AppDbContext
```

Para conferir tabelas, Ã­ndice espacial e seed, conecte ao banco `GeraDb`; a tabela `__EFMigrationsHistory` registra as migrations aplicadas. As categorias iniciais sÃ£o configuradas em `CategoriaConfiguration`.

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

Abra `http://localhost:5173`. O servidor Vite encaminha `/api` para a API local. CORS estÃ¡ configurado para `localhost:5173` e `127.0.0.1:5173` quando a API Ã© chamada diretamente.

## VerificaÃ§Ã£o

```powershell
dotnet test Gera.sln
cd web
npm run build
```

Os detalhes dos endpoints estÃ£o em [docs/API.md](docs/API.md); as polÃ­ticas em [docs/REGRAS-DE-NEGOCIO.md](docs/REGRAS-DE-NEGOCIO.md); a decisÃ£o de armazenamento de tokens em [docs/DECISOES.md](docs/DECISOES.md).

Consulte tambÃ©m [a arquitetura](docs/ARQUITETURA.md), [o banco de dados e migrations](docs/BANCO-DE-DADOS.md), [o guia de contribuiÃ§Ã£o](docs/GUIA-DE-CONTRIBUICAO.md), [as instruÃ§Ãµes de deploy](docs/DEPLOY.md) e o [changelog](CHANGELOG.md).

## Etapa 3: interface e sessoes

O React usa tokens/componentes em `web/src/design`, layout e rotas em `web/src/layout` e `web/src/routing`; o mapa esta em [docs/FRONTEND.md](docs/FRONTEND.md). As paginas de servicos ainda usam conteudo provisÃ³rio, exceto Categorias, que consulta `GET /api/categorias`.

A tolerancia de reutilizacao do refresh concorrente e configuravel por `Auth:RefreshReuseGraceSeconds` (padrao 10 segundos). O cliente compartilha uma unica chamada de refresh em andamento. O endpoint `GET /api/perfil/resumo` fornece nome, foto, papeis e papel ativo.

Na pasta `web`, use `npm run test` para os testes React (Vitest + Testing Library), alem de `npm run build`. O envio real de e-mail para recuperacao de senha ainda precisa ser integrado antes da producao.


## Etapa 8: chat e notificacoes

O chat por solicitacao, mensagens e notificacoes no aplicativo estao descritos em [docs/ETAPA-8-CHAT-NOTIFICACOES.md](docs/ETAPA-8-CHAT-NOTIFICACOES.md).
