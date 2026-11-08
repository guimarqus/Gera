# Registro de Decisões de Arquitetura (ADR) — Gera

Este documento registra as decisões técnicas, arquiteturais e de produto tomadas durante o desenvolvimento do **Gera**, justificando o contexto, as opções avaliadas e o motivo da escolha.

---

## ADR 001: Estrutura da Solution e Separação em Camadas

- **Data:** 2026-11-07
- **Status:** Aceito

### Contexto
O projeto Gera é um marketplace de serviços gerais de alta concorrência e simplicidade de uso, composto por backend C# .NET e frontend React. A arquitetura exige separação clara de responsabilidades, regras de negócio isoladas de frameworks de apresentação e banco, e conformidade com o padrão MVC em camadas (Model = Domain/Application + EF Core; Controller = Web API; View = React).

### Decisão
Organizar a solution `Gera.sln` em:
- `src/Gera.Domain`: Entidades, enums e exceptions do domínio. Dependência zero de frameworks externos (apenas biblioteca de tipos espaciais NetTopologySuite e tipos de Identity).
- `src/Gera.Application`: Contratos de serviços, DTOs, validadores FluentValidation e interface `IAppDbContext`.
- `src/Gera.Infrastructure`: Implementação do `AppDbContext`, mapeamentos fluentes EF Core, migrations, IdentityDbContext, repositórios e serviços de infraestrutura.
- `src/Gera.Api`: Controllers finos da Web API, middlewares (como o `GlobalExceptionHandlerMiddleware` com ProblemDetails), endpoints e configuração do Swagger.
- `tests/Gera.UnitTests`: Testes unitários com xUnit, Moq e FluentAssertions.
- `tests/Gera.IntegrationTests`: Testes de integração de endpoints com `WebApplicationFactory<Program>`.
- `web/`: Aplicação SPA React (Vite, Tailwind, React Router, TanStack Query).
- `docs/`: Documentação viva do projeto.

### Consequências
- Os controllers não acessam o `DbContext` diretamente nem contêm regras de negócio.
- O domínio permanece limpo, testável e desacoplado.

---

## ADR 002: Persistência com SQL Server, Coluna `geography` e NetTopologySuite

- **Data:** 2026-11-07
- **Status:** Aceito

### Contexto
O Gera necessita de buscas geolocalizadas eficientes e cálculos de proximidade entre clientes e prestadores para atender ao critério de tempo de resposta inferior a 3 segundos em redes móveis (4G).

### Decisão
Utilizar o **SQL Server** com o pacote `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite`:
- A entidade `Oferta` possui a propriedade `Localizacao` do tipo `Point` com SRID 4326 (WGS 84) mapeada para a coluna SQL Server `geography`.
- Índice espacial `SPATIAL_IX_Ofertas_Localizacao` criado para aceleração das consultas espaciais STDistance.
- Coordenadas `Latitude` e `Longitude` numéricas mantidas como colunas auxiliares para apresentação sem necessidade de conversão contínua.

### Consequências
- Consultas por raio de atendimento e ordenação por distância executam nativamente com auxílio de índice espacial no SQL Server.

---

## ADR 003: Convenção de Nomes e Tabelas em Português no Plural

- **Data:** 2026-11-07
- **Status:** Aceito

### Contexto
A especificação do projeto (Regra 9 das Regras Inegociáveis) determina que as entidades e tabelas estejam em português, no plural, idênticas no código e no banco de dados.

### Decisão
Mapear todas as entidades e tabelas Identity no plural em português:
- `Usuarios` (substituindo o padrão `AspNetUsers`)
- `Papeis` (substituindo `AspNetRoles`)
- `UsuarioPapeis`, `UsuarioClaims`, `UsuarioLogins`, `PapelClaims`, `UsuarioTokens`
- `Categorias`, `Ofertas`, `OfertaFotos`, `OfertaTags`, `Favoritos`
- `Solicitacoes`, `SolicitacaoHistoricos`, `Orcamentos`, `Mensagens`
- `Avaliacoes`, `Notificacoes`, `Denuncias`, `Bloqueios`, `Pagamentos`

### Consequências
- Esquema de banco de dados e modelos C# 100% padronizados e em conformidade estrita com o domínio de negócio.

---

## ADR 004: Concorrência Otimista e Tratamento de Erros

- **Data:** 2026-11-07
- **Status:** Aceito

### Contexto
No Kanban de serviços, dois usuários (cliente e prestador) podem interagir simultaneamente com a mesma solicitação. Além disso, erros e exceções de negócio precisam de formato padronizado conforme o padrão RFC 7807 (`ProblemDetails`).

### Decisão
- A entidade `Solicitacao` inclui a propriedade `RowVersion` mapeada com `.IsRowVersion()` (coluna `rowversion` no SQL Server) para controle de concorrência otimista.
- `GlobalExceptionHandlerMiddleware` captura exceções de domínio (`NotFoundException`, `NaoAutorizadoException`, `TransicaoInvalidaException`, `DomainException`) e serializa em `ProblemDetails` (`application/problem+json`).
- Registro explícito de `services.AddDataProtection()` na camada de infraestrutura para permitir geração segura de tokens Identity sem dependência implícita de outros subsistemas.

# ADR: refresh token em cookie HttpOnly e access token em memória

**Status:** aceito — Etapa 2.

O access token JWT, de vida curta (15 minutos), é retornado no corpo e mantido somente em memória no React. O refresh token aleatório é enviado exclusivamente em cookie `HttpOnly`, `Secure`, `SameSite=Strict`, limitado a `/api/auth`; a API armazena somente o hash SHA-256 e rotaciona o token a cada uso.

Essa escolha evita persistir o JWT em `localStorage` e impede que scripts do navegador leiam diretamente o refresh token. A rotação permite detectar reapresentação de token já revogado e revogar os refresh tokens ativos da conta. O cliente precisa enviar credenciais para refresh/logout; produção deve servir frontend e API em contexto seguro HTTPS.

## ADR 006: tolerancia para refresh concorrente e envio de e-mail

- **Status:** aceito — Etapa 3.

O cliente React compartilha uma unica promessa de refresh por vez. A API aceita por 10 segundos, configuraveis em `Auth:RefreshReuseGraceSeconds`, a reapresentacao de um refresh token que acabou de ser rotacionado e devolve a mesma rotacao guardada somente em memoria. Fora dessa janela, considera reutilizacao como roubo e revoga a cadeia do usuario. Tokens persistidos no banco continuam sendo apenas hashes.

O sender de desenvolvimento apenas registra o link de recuperacao no log. **O envio real de e-mail continua pendente e deve ser implementado antes da producao.**
