# Etapa 8 — Chat e notificações

## Chat por solicitação

As mensagens de texto, fotos, áudio, propostas e mudanças de etapa ficam persistidas na tabela `Mensagens`. O histórico mostra as 50 mais recentes e aceita cursor `antesDe`; conversas ficam limitadas às solicitações das quais o usuário é cliente ou prestador. A API responde `404` para terceiros.

SignalR atende em `/hubs/chat`. O cliente autenticado entra em um grupo somente depois de o servidor confirmar que participa daquela solicitação. Eventos: `NovaMensagem`, `UsuarioDigitando`, `MensagensLidas` e `SolicitacaoAtualizada`. A API aceita o access token na query `access_token` somente na rota do Hub, conforme o handshake WebSocket do SignalR.

Rotas REST:

- `GET /api/mensagens/conversas`
- `GET /api/mensagens/{solicitacaoId}?antesDe={id}` — 50 itens por página.
- `POST /api/mensagens/{solicitacaoId}` com `{ "conteudo": "..." }` — limite de 2.000 caracteres e 30 envios/minuto por usuário.
- `POST /api/mensagens/{solicitacaoId}/midia` em multipart, campo `arquivo` — imagem JPEG/PNG/WebP até 5 MB ou áudio MP3/WAV/OGG/WebM/M4A até 10 MB; limite 10 envios/minuto.
- `POST /api/mensagens/{solicitacaoId}/lidas` marca como lidas mensagens recebidas.
- `GET /api/mensagens/midia/{guid}` abre anexo somente a participantes.

Fotos são validadas pelo conteúdo, decodificadas, limitadas a 16 megapixels e recriadas em WebP até 1.600 px, removendo metadados ao reencodar. Áudio usa assinatura real do formato; extensão e MIME declarados pelo navegador são ignorados. Arquivos recebem GUID no servidor e ficam em `src/Gera.Api/App_Data/mensagens` no desenvolvimento. As respostas não expõem caminho físico e o download envia `X-Content-Type-Options: nosniff`.

## Notificações

- `GET /api/notificacoes?pagina=1&tamanho=20` retorna a página e total de não lidas (máximo 50 itens por página).
- `PATCH /api/notificacoes/{id}/lida` e `POST /api/notificacoes/lidas` atualizam apenas notificações do usuário autenticado.
- `GET /api/notificacoes/preferencias` e `PUT /api/notificacoes/preferencias` controlam a geração de notificações no aplicativo.
- O menu superior mostra a quantidade não lida e a página `/notificacoes` lista, abre e marca itens. `/configuracoes` permite ativar ou desativar notificações.
- Mensagens e atualizações do Kanban criam notificações persistidas quando a preferência está ativa; o Hub avisa as sessões do usuário para atualizar o contador.

Não foi necessária migration: `Mensagens`, `Notificacoes`, `TipoMensagem` e a preferência `Usuarios.NotificacoesAtivas` já existiam. Os arquivos ficam no armazenamento local de desenvolvimento; armazenamento distribuído, limpeza/retensão e escala do SignalR precisam ser definidos para produção.

## ADR 009 — Grupos SignalR privados

O cliente não escolhe grupos de usuários. O Hub valida participação em cada solicitação antes de associar a conexão ao grupo `solicitacao:{id}`; grupo de usuário é criado a partir do claim `sub`. A autenticação via query token é aceita exclusivamente em `/hubs/chat` para compatibilidade com WebSocket do navegador. A autorização da REST API continua sendo a fonte de verdade para ler e alterar dados.
