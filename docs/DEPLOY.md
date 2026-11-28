# Deploy

O repositório não contém pipeline ou destino de produção configurado. Este documento registra os requisitos de configuração conhecidos, sem declarar um processo de deploy já operacional.

## Configuração necessária

- SQL Server acessível pela API e migrations revisadas/aplicadas por uma etapa controlada.
- `ConnectionStrings:DefaultConnection` e `JwtSettings:Secret` fornecidos por gerenciador de segredos ou variáveis protegidas; nunca em `appsettings.json` versionado.
- `Frontend:Origin` definido para a origem real do frontend, com HTTPS e política de CORS restrita.
- `ASPNETCORE_ENVIRONMENT=Production`; Swagger só é habilitado em Development.
- Sender de e-mail real: o sender de produção atual é `NullEmailSender`, então recuperação de senha não envia mensagem fora de Development.
- Armazenamento de mídia durável e compartilhado antes de escalar além de uma instância; hoje o adaptador configurado é armazenamento local.
- Cookies de refresh precisam operar em contexto HTTPS e com domínio/origens compatíveis.

## Verificações antes da publicação

1. Aplicar e revisar migrations em ambiente de staging com backup disponível.
2. Confirmar conexão, health check `/api/health`, CORS, autenticação e fluxo de refresh.
3. Verificar permissões de escrita/leitura do armazenamento de fotos.
4. Executar testes .NET, testes React e build de produção.
5. Definir observabilidade, backup, retenção, rollback e rotação de segredos no ambiente escolhido.

Os passos acima são critérios pendentes de operação; não existe evidência neste repositório de deploy de produção realizado.
