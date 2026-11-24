# API de autenticação

Base local: `http://localhost:5215/api`. Todos os endpoints, exceto os marcados como públicos, exigem `Authorization: Bearer <accessToken>`. O refresh token é entregue apenas no cookie `gera_refresh` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`). Envie cookies com `credentials: include`.

## Cadastro

`POST /auth/register`

```json
{
  "nome": "Maria Silva",
  "email": "maria@example.com",
  "nomeUsuario": "maria.silva",
  "telefone": "11999998888",
  "senha": "Senha1234",
  "aceitouTermos": true,
  "aceitouPrivacidade": true
}
```

Retorna `200` e define o cookie de refresh. O corpo contém somente o access token e sua expiração. Os aceites são gravados com data/hora UTC.

## Login

`POST /auth/login` aceita nome de usuário ou e-mail:

```json
{ "usuarioOuEmail": "maria@example.com", "senha": "Senha1234" }
```

Credenciais inválidas sempre retornam `401` com `Usuário ou senha incorretos`. Cinco falhas seguidas bloqueiam a conta por 15 minutos.

## Sessão e renovação

`GET /auth/eu` retorna o perfil associado ao `sub` do JWT:

```http
GET /api/auth/eu
Authorization: Bearer <accessToken>
```

`POST /auth/refresh` lê o refresh token do cookie, revoga-o e emite um novo. O access token renovado vai no corpo:

```http
POST /api/auth/refresh
Cookie: gera_refresh=<cookieHttpOnly>
```

`POST /auth/logout` revoga o refresh token atual e remove o cookie; retorna `204`:

```http
POST /api/auth/logout
Cookie: gera_refresh=<cookieHttpOnly>
```

Use `credentials: include` para refresh e logout. Tokens de acesso duram 15 minutos.

## Recuperação de senha

- `POST /auth/esqueci-senha` com `{ "email": "maria@example.com" }`. Sempre retorna a mesma mensagem, independentemente da existência da conta. No ambiente de desenvolvimento, o link de redefinição é escrito no log da API.
- `POST /auth/redefinir-senha` com `{ "email": "maria@example.com", "token": "token-recebido", "novaSenha": "NovaSenha123" }`.

Fora de Development, o sender atual não envia mensagens; conecte um provedor de e-mail antes de habilitar recuperação em produção.

O limite padrão para cada endereço IP é de 10 requisições por minuto nos endpoints de autenticação; novas tentativas recebem `429`.

## Categorias

`GET /categorias` e publico e devolve categorias ativas em arvore. A resposta fica em cache em memoria na API por 30 minutos.

```json
[
  { "id": 1, "nome": "Casa", "descricao": "Servicos para casa", "icone": null,
    "subcategorias": [{ "id": 2, "nome": "Eletrica", "descricao": null, "icone": null, "subcategorias": [] }] }
]
```

## Resumo do perfil

`GET /perfil/resumo` exige access token. O identificador do usuario e obtido exclusivamente do claim `sub`.

```json
{ "nome": "Maria Silva", "fotoUrl": null, "papeis": ["Cliente"], "papelAtivo": "Cliente" }
```
## Ofertas e rascunhos

- `GET /api/categorias/{id}/modelo-oferta` retorna a versão e os campos do modelo de cadastro. Subcategorias herdam o modelo da categoria principal. Categorias inativas ou inexistentes retornam `404`.

- `POST /api/ofertas` cria um rascunho para o usuário autenticado. O corpo pode ser vazio. A resposta é `201 Created`.
- `GET /api/ofertas/minhas?pagina=1&tamanho=20` lista as ofertas do usuário. Aceita filtro opcional `status` e até 100 itens por página.
- `GET /api/ofertas/{id}` devolve os dados de edição ao dono. Para visitantes, somente ofertas ativas são visíveis, com os dados públicos e sem coordenadas. Rascunhos e ofertas excluídas retornam `404` para terceiros.
- `PUT /api/ofertas/{id}` substitui os dados editáveis da oferta. Envie a `versao` da última resposta. Uma versão desatualizada retorna `409`. Coordenadas devem ser enviadas juntas. Ofertas ativas precisam continuar atendendo aos requisitos de publicação.
- O `PUT` também aceita `atributos` (objeto com chaves e valores textuais compatíveis com o modelo), `versaoModelo` e até cinco `tags` de 50 caracteres. Tags equivalentes sem distinção de acento e maiúsculas são agrupadas. Envie coleções vazias para limpar os dados. A resposta devolve `caracteristicas` e `tags`.
- `DELETE /api/ofertas/{id}` registra a exclusão lógica e retorna `204`. Operações privadas em ofertas de terceiros retornam `404`.
- `POST /api/ofertas/{id}/publicar` publica um rascunho completo e concede o papel Prestador na mesma transação. Chamadas repetidas para uma oferta ativa retornam seu estado atual.
- `PATCH /api/ofertas/{id}/status` aceita `{"status":3}` para pausar e `{"status":2}` para reativar. O limite de ofertas ativas por usuário é configurável em `Ofertas:LimiteAtivasPorUsuario` com padrão 20.
- A publicação exige categoria ativa, título com 5 a 120 caracteres, descrição com 30 a 2.000 caracteres, pelo menos uma foto, localização, raio de 1 a 100 km, disponibilidade e preço compatível com a modalidade.
- `GET /api/ofertas/{id}/fotos` lista as fotos para o proprietário. `PATCH .../fotos/ordem` recebe `fotoIds` com todas as fotos, e `PATCH .../fotos/{fotoId}/capa` define a capa. Ambos recebem `versao` no corpo.
- `DELETE .../fotos/{fotoId}` recebe a versão em `If-Match` e remove a foto do anúncio. A remoção da última foto de uma oferta ativa é bloqueada. As operações retornam a lista atualizada e a nova versão da oferta.
