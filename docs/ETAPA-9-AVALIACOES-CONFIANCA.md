# Etapa 9 — avaliações, denúncias e bloqueios

## Entrega

- Participantes de uma solicitação concluída e confirmada podem avaliar uns aos outros uma vez, com nota de 1 a 5 e comentário opcional de até 1.000 caracteres.
- O autor da avaliação pode consultar a própria avaliação; o perfil público do prestador exibe avaliações de forma anônima e a resposta pública correspondente.
- Somente o usuário avaliado pode responder uma vez, com até 1.000 caracteres.
- Usuários autenticados podem denunciar ofertas, perfis e mensagens. Mensagens só podem ser denunciadas por participantes da solicitação relacionada.
- Denúncias ficam pendentes para análise administrativa futura. Esta etapa não entrega a fila de moderação, que pertence à Etapa 10.
- Usuários podem bloquear, listar e desbloquear outros usuários. O bloqueio oculta ofertas no feed autenticado, impede novas solicitações e interrompe acesso às conversas e envio de mensagens entre as partes.

## API

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/solicitacoes/{id}/avaliacoes` | Participante da solicitação |
| GET | `/api/solicitacoes/{id}/avaliacoes/minha` | Participante da solicitação |
| POST | `/api/avaliacoes/{id}/resposta` | Usuário avaliado |
| GET | `/api/prestadores/{id}/avaliacoes?pagina=1&tamanho=10` | Público, paginado |
| POST | `/api/denuncias` | Autenticado |
| GET | `/api/bloqueios` | Autenticado, próprios bloqueios |
| PUT | `/api/bloqueios/{usuarioAlvoId}` | Autenticado |
| DELETE | `/api/bloqueios/{usuarioAlvoId}` | Autenticado, próprio bloqueio |

Os alvos de denúncia usam `TipoAlvoDenuncia`: Oferta=1, Usuario=2, Mensagem=3. A identidade do denunciante vem do JWT. O motivo deve conter 3 a 150 caracteres e a descrição até 1.000. Não há nova migration: os modelos e constraints necessários já existiam.

## Decisão — ADR 009

Usar os modelos e constraints atuais, centralizando regras em `ConfiancaService`. Bloqueios aplicam-se às interações no produto, mas preservam o histórico no banco. A denúncia não revela ao denunciante o estado interno da moderação. O feed filtra bloqueios para usuários autenticados; buscas anônimas continuam públicas.

## Limitações

- A fila, decisões e auditoria de moderação ficam para Etapa 10.
- Não há recurso de contestação de avaliação nesta etapa.
- Os testes de integração dependem de SQL Server LocalDB disponível; validar os novos fluxos em ambiente SQL Server isolado antes de produção.
