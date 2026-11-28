# Etapa 7 — Solicitações, negociação e Kanban

## Fluxo entregue

O cliente abre uma solicitação a partir de uma oferta ativa. Cliente e prestador podem enviar propostas e contrapropostas; a proposta pendente anterior é encerrada quando chega uma nova. A autoria fica registrada em `Orcamentos.AutorId`. Apenas o outro participante responde. A aceitação exige data futura e agenda o serviço com o valor acordado.

O prestador inicia o serviço agendado, solicita conclusão ao terminar e o cliente confirma. Cancelamentos exigem motivo e são permitidos até a etapa Agendados. As transições ficam centralizadas no serviço de aplicação, geram histórico e gravam notificação para o outro participante.

## API

- `POST /api/ofertas/{ofertaId}/solicitacoes`
- `GET /api/solicitacoes/minhas?etapa={etapa}` — até 100 resultados.
- `GET /api/solicitacoes/{id}` — somente participantes; terceiros recebem 404.
- `POST /api/solicitacoes/{id}/orcamentos`
- `POST /api/solicitacoes/{id}/orcamentos/{orcamentoId}/aceitar`
- `POST /api/solicitacoes/{id}/orcamentos/{orcamentoId}/recusar`
- `PATCH /api/solicitacoes/{id}/etapa`
- `POST /api/solicitacoes/{id}/solicitar-conclusao`
- `POST /api/solicitacoes/{id}/confirmar-conclusao`

Cada escrita inclui a versão Base64 da coluna `RowVersion`. Uma versão obsoleta produz `409 Conflict`. O ID do usuário é obtido do JWT.

## Banco e migração

A migration `20261128133837_RegistrarAutorOrcamento` adiciona `AutorId` à tabela `Orcamentos`, com chave estrangeira para `Usuarios`. Orçamentos existentes são preservados e recebem `AutorId = PrestadorId` como autor histórico conhecido.

## Interface

A rota protegida `/solicitacoes` apresenta as colunas do Kanban com propostas, histórico e ações permitidas por etapa. A página de detalhes da oferta inicia o pedido. SignalR e centro visual de notificações permanecem para a etapa de chat/notificações; nesta entrega as notificações são persistidas.
