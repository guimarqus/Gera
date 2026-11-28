# Etapa 11 — pagamentos, verificação e e-mail

## Entregue nesta etapa

- E-mail real de recuperação de senha via SMTP configurável, usando TLS e credenciais fornecidas pelo ambiente.
- Em produção, a API valida a configuração SMTP ao iniciar e não usa mais o sender no-op.
- Em desenvolvimento, SMTP pode ser configurado para enviar mensagens reais. Sem SMTP, o app avisa no log que a mensagem não foi enviada; tokens e links de redefinição não são registrados.
- Falhas do envio não alteram a resposta genérica do endpoint de recuperação e não revelam se o e-mail pertence a uma conta.
- Administradores podem conceder ou remover o selo de verificação com justificativa registrada na auditoria. A revisão deve ocorrer por processo seguro externo; documentos pessoais não são armazenados na aplicação.
- O perfil público do prestador mostra o selo somente quando `Verificado` foi concedido por um administrador.

Endpoint administrativo: `PATCH /api/admin/usuarios/{id}/verificacao`, com `{ "verificado": true, "motivo": "referência interna da validação" }`. O motivo fica na auditoria; não inclua números de documentos ou cópias de evidências.

Configuração SMTP, via configuração segura do ambiente:

```text
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__EnableSsl=true
Email__Smtp__UserName
Email__Smtp__Password
Email__Smtp__FromAddress
Email__Smtp__FromName=Gera
```

Não coloque credenciais SMTP em arquivos versionados. O host, remetente e autenticação precisam ser fornecidos pelo serviço de e-mail escolhido. Sem essas informações não é possível validar entrega real nesta sessão.

## Pagamentos — bloqueado por decisão de produto

A tabela `Pagamentos` e os enums existem, mas ainda não há API de cobrança, provedor, webhooks autenticados ou política de estorno. Nenhuma alteração nesta etapa marca uma transação como paga. O prompt do projeto condiciona pagamentos a uma definição comercial, contratual e regulatória. É necessário escolher o provedor contratado e definir comissão, responsabilidades por disputa/estorno e quando o pagamento é capturado/liberado antes de implementar checkout.

## Validação e limitações

- Build e testes unitários verificam a configuração SMTP; uma entrega de e-mail exige credenciais e servidor SMTP acessíveis.
- Os testes com SQL Server e a migration da auditoria administrativa continuam sujeitos à disponibilidade do LocalDB, que falhou com `Cannot create an automatic instance` nesta máquina.
- Não foi realizado teste de envio para endereço externo.

## Decisão — ADR 011

Usar SMTP TLS configurado pelo ambiente para recuperação de senha e falhar no startup de produção se estiver ausente ou inválido. Falhas de envio são registradas sem e-mail nem token e mantêm a resposta genérica. A integração financeira permanece sem implementação até o provedor e as regras comerciais serem definidos.
