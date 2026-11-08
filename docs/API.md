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
