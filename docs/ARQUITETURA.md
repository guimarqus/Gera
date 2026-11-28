# Arquitetura

## Visão geral

O Gera é uma aplicação web desacoplada: uma API ASP.NET Core fornece os casos de uso e uma SPA React/Vite apresenta a interface. A persistência usa Entity Framework Core e SQL Server; a localização usa NetTopologySuite e `geography`.

## Projetos e responsabilidades

- `Gera.Domain`: entidades, enums, exceções e regras independentes da interface HTTP.
- `Gera.Application`: casos de uso, DTOs, contratos e validações. `IAppDbContext` e serviços de ofertas ficam nesta camada.
- `Gera.Infrastructure`: `AppDbContext`, mapeamentos, migrations, Identity, armazenamento local de mídia e serviços técnicos.
- `Gera.Api`: composição da aplicação, autenticação/autorização, controllers, middleware, rate limiting, Swagger e health check.
- `web`: SPA React, rotas, componentes, estado de servidor e chamadas HTTP.
- `tests`: testes unitários e testes de integração com `WebApplicationFactory`.

Dependências seguem `Api → Application → Domain` e `Infrastructure → Application → Domain`. Controllers delegam regras e persistência aos serviços da Application.

## Fluxo de uma oferta

1. A API obtém o identificador do prestador do claim autenticado `sub`.
2. `OfertasController` chama `OfertaService` para rascunho, edição, publicação e status.
3. `OfertaService` aplica propriedade, validação, limite de ofertas e concorrência com `rowversion`.
4. `AppDbContext` persiste a oferta; a publicação agrupa status e concessão do papel Prestador em transação.
5. `OfertaFotosController` delega metadados a `OfertaFotosService` e os bytes à interface `IArmazenamentoMidia`.
6. `ArmazenamentoLocalMidia` valida e recodifica as imagens em WebP no ambiente atual.

## Configuração e execução

As configurações obrigatórias são `ConnectionStrings:DefaultConnection` e `JwtSettings:Secret`; mantenha-as em User Secrets no desenvolvimento ou em um cofre/variáveis protegidas no deploy. Consulte o [README](../README.md), [banco de dados](BANCO-DE-DADOS.md) e [deploy](DEPLOY.md).
