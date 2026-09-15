# Scheduler API (.NET + MySQL)

API de exemplo para integração com o front do Scheduler.

## O que já vem pronto

- login de profissional e cliente
- cadastro de profissional
- cadastro de cliente vinculado a um profissional
- endpoints do painel do cliente
- endpoints públicos para listar profissionais, serviços e horários disponíveis
- dashboard, clientes, serviços, disponibilidade, perfil e configurações do profissional

## Como rodar

1. Ajuste a connection string em `Scheduler.Api/appsettings.json`.
2. Crie o banco com `Scheduler.Api/Sql/create_database.sql`.
3. Se você já tinha o banco antigo, execute também `Scheduler.Api/Sql/update_auth_client_portal.sql`.
4. Rode o seed em `Scheduler.Api/Sql/seed.sql`.
5. Execute:

```bash
cd Scheduler.Api
dotnet restore
dotnet run
```

Swagger padrão:

```txt
http://localhost:5080/swagger
```

## Credenciais de teste

## Configuração segura do SMTP (Gmail)

O projeto não guarda mais a senha SMTP nos arquivos versionados. Para enviar e-mails pelo
`massinire@gmail.com`, ative a verificação em duas etapas nessa conta, crie uma **senha de app**
exclusiva para este sistema e guarde-a como segredo. Nunca use a senha normal do Gmail.

No ambiente de desenvolvimento, execute uma vez (substituindo o valor sem compartilhá-lo):

```bash
dotnet user-secrets set "Email:Password" "SUA_NOVA_SENHA_DE_APP" --project Scheduler.Api
```

Em produção, configure o segredo no provedor de hospedagem como a variável de ambiente
`Email__Password`. Mantenha também `Email__Username=massinire@gmail.com` e
`Email__FromEmail=massinire@gmail.com` caso a hospedagem sobrescreva a configuração do projeto.

O endpoint de teste de e-mail foi removido, pois ele permitia que qualquer pessoa escolhesse o
destinatário e poderia ser abusado para spam. Os endpoints públicos de agendamento agora têm
limite de cinco tentativas por IP a cada dez minutos.

### Profissional
- e-mail: `renan@email.com`
- senha: `123456`

### Cliente
- e-mail: `cliente@email.com`
- senha: `123456`
