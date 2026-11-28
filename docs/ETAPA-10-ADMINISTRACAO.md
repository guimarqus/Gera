# Etapa 10 — administração e moderação

## Entrega

- Painel administrativo com indicadores, fila de denúncias, gestão de categorias, busca de contas e trilha de auditoria.
- Denúncias podem ser encaminhadas para análise, aceitas ou rejeitadas com parecer e identificador do moderador.
- Ofertas podem ser retiradas do feed por moderação ou reativadas com justificativa.
- Categorias podem ser criadas e editadas, incluindo ativação, ordem e hierarquia. A exclusão é lógica para preservar relações históricas.
- Contas podem ser suspensas ou reativadas. A suspensão revoga refresh tokens e a API rejeita imediatamente access tokens de contas inativas.
- Administradores podem conceder ou remover o selo de verificação com justificativa auditada, após revisão manual de evidência em processo seguro externo. O sistema não armazena cópias de documentos de identidade.
- Cada operação administrativa cria um registro na tabela `AuditoriasAdmin` na mesma transação da alteração.

## Acesso e provisionamento

A política `Administrador` consulta o usuário e sua associação ao papel no Identity a cada requisição administrativa. O papel não é derivado do papel visual Cliente/Prestador e a API não oferece endpoint para um usuário conceder esse papel a si mesmo.

A migration `AuditoriaAdmin` cria a trilha de auditoria e o papel `Administrador`, mas não promove nenhuma conta automaticamente. O primeiro administrador deve ser provisionado pelo responsável pelo banco, depois da migration, associando o papel ao usuário escolhido. Exemplo operacional a executar com o e-mail revisado pelo operador:

```sql
DECLARE @Email nvarchar(256) = N'admin@exemplo.com';
INSERT INTO dbo.UsuarioPapeis (UserId, RoleId)
SELECT u.Id, r.Id
FROM dbo.Usuarios u
JOIN dbo.Papeis r ON r.NormalizedName = N'ADMINISTRADOR'
WHERE u.Email = @Email
  AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioPapeis ur WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);
```

## Endpoints

- `GET /api/admin/resumo`
- `GET /api/admin/denuncias?status=1&pagina=1&tamanho=20`
- `PATCH /api/admin/denuncias/{id}`
- `PATCH /api/admin/ofertas/{id}`
- `GET|POST /api/admin/categorias`; `PUT /api/admin/categorias/{id}`
- `GET /api/admin/usuarios?busca=&pagina=1&tamanho=20`
- `PATCH /api/admin/usuarios/{id}/status`
- `GET /api/admin/auditorias?pagina=1&tamanho=50`

Todos exigem usuário ativo no papel `Administrador`. Contas administrativas não podem ser suspensas por esse painel; o bloqueio de um administrador deve seguir o procedimento de acesso emergencial do operador do banco.

## Limitações de validação

Os builds e testes unitários são executáveis sem banco. A aplicação da migration, consultas administrativas e o teste de autorização precisam de SQL Server de teste. A instância LocalDB apresentou `Cannot create an automatic instance` neste ambiente; a migration não foi aplicada nesta sessão.

## Decisão — ADR 010

Persistir auditoria no SQL Server junto à alteração administrativa, sem registrar segredos ou payloads pessoais desnecessários. A autorização consulta membership no Identity em vez de confiar no papel visual ou em um claim que poderia ficar desatualizado. O JWT é revalidado contra o estado ativo da conta em cada requisição.
