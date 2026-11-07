# Gera — Marketplace de Serviços Gerais

O **Gera** conecta quem precisa de serviços gerais a profissionais próximos (pedreiro, eletricista, encanador, pintor, diarista, fretes, jardinagem, reparos).

---

## 🏗️ Estrutura do Projeto

```
Gera.sln
├── src/
│   ├── Gera.Api              → Controllers RESTful, Middlewares (ProblemDetails), Swagger, Program.cs
│   ├── Gera.Application      → Contratos, DTOs, Validadores FluentValidation, IAppDbContext
│   ├── Gera.Domain           → Entidades, Enums, Exceptions (sem acoplamento)
│   └── Gera.Infrastructure   → AppDbContext, Mapeamentos EF Core, Migrations, Identity, NetTopologySuite
├── tests/
│   ├── Gera.UnitTests        → Testes unitários de regras de domínio (xUnit + FluentAssertions)
│   └── Gera.IntegrationTests → Testes de integração de API (WebApplicationFactory)
├── web/                      → Aplicação Web SPA React
└── docs/                     → Documentação e registro de decisões de arquitetura (DECISOES.md)
```

---

## 🚀 Como Executar

### Pré-requisitos
- .NET 10 SDK (ou .NET 11)
- SQL Server (LocalDB ou instância dedicada)
- Node.js e npm

### Backend (.NET API)

1. Restaurar os pacotes da solution:
   ```powershell
   dotnet restore Gera.sln
   ```

2. Compilar os projetos:
   ```powershell
   dotnet build Gera.sln
   ```

3. Executar os testes automatizados (unitários e de integração):
   ```powershell
   dotnet test Gera.sln
   ```

4. Executar a API:
   ```powershell
   dotnet run --project src/Gera.Api/Gera.Api.csproj
   ```
   Acesse a documentação interativa do Swagger em: `https://localhost:7080/swagger` ou `http://localhost:5080/swagger`.
   Health check: `GET /api/health`

---

## 📚 Documentação
- [Registro de Decisões de Arquitetura](docs/DECISOES.md)
