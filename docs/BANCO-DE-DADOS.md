# Banco de dados

## Tecnologia

SQL Server é o banco oficial. O `AppDbContext` usa EF Core e Identity com chaves inteiras. As tabelas usam nomes em português no plural. A oferta mantém `Localizacao` como `geography` via NetTopologySuite; latitude e longitude também são mantidas para o editor.

## Tabelas da migration inicial

O modelo atual contém as tabelas de domínio `Categorias`, `Ofertas`, `OfertaFotos`, `OfertaTags`, `Favoritos`, `Solicitacoes`, `SolicitacaoHistoricos`, `Orcamentos`, `Mensagens`, `Avaliacoes`, `Notificacoes`, `Denuncias`, `Bloqueios`, `Pagamentos` e `RefreshTokens`, além das tabelas de Identity `Usuarios`, `Papeis`, `UsuarioPapeis`, `UsuarioClaims`, `UsuarioLogins`, `PapelClaims` e `UsuarioTokens`.

As categorias iniciais são declaradas por `CategoriaConfiguration.HasData`. Há índices para consultas por status/categoria, solicitações por participante/etapa, mensagens por solicitação/data e notificações por usuário/leitura. A migration inicial cria `SPATIAL_IX_Ofertas_Localizacao` na coluna `geography`.

## Migrations

- `20261107211323_InitialCreate`: esquema inicial, índices e seed.
- `20261107212935_AddRefreshTokens`: persistência dos refresh tokens.
- `20261124022624_CadastroOfertasRascunhos`: campos e estado de rascunho para ofertas, índice espacial recriado.
- `20261124022700_CadastrarPapelPrestador`: papel Prestador.
- `20261124025500_MetadadosFotosOferta`: capa e URL de miniatura; define a primeira foto existente como capa.

Consulte o código das migrations antes de atualizar um banco existente. A sessão de validação executou a suíte de integração, cujo fixture aplica `Database.MigrateAsync()` em um banco LocalDB de nome exclusivo e o remove ao final. Os testes de publicação também gravam coordenadas e confirmam o seed de categorias. A consulta manual por `sqlcmd` não foi concluída por falha TLS do cliente ODBC; não há confirmação do estado de um banco persistente de desenvolvimento.

## Comandos

Configure `ConnectionStrings:DefaultConnection` via User Secrets antes de executar migrations. Para listar migrations e atualizar:

```powershell
dotnet ef migrations list --project src/Gera.Infrastructure/Gera.Infrastructure.csproj --startup-project src/Gera.Api/Gera.Api.csproj --context AppDbContext
dotnet ef database update --project src/Gera.Infrastructure/Gera.Infrastructure.csproj --startup-project src/Gera.Api/Gera.Api.csproj --context AppDbContext
```

Os testes de integração usam `GeraTest__SqlServer` quando configurada; sem ela, usam `(localdb)\MSSQLLocalDB` e um catálogo temporário com nome aleatório.
