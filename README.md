# Scheduler API (.NET + MySQL)

API de exemplo para integração com o front do Scheduler.

## O que já vem pronto

- login de profissional e cliente
- cadastro de profissional
- cadastro de cliente vinculado a um profissional
- endpoints do painel do cliente
- endpoints públicos para listar profissionais, serviços e horários disponíveis
- dashboard, clientes, serviços, disponibilidade, perfil e configurações do profissional

## Login no agendamento público

Na página `/agendar/{slug}`, o cliente pode entrar com e-mail e senha ou criar uma conta vinculada à agenda. O formulário também continua permitindo agendamento como visitante, sem login.

O frontend carrega `professionalUserId` em `GET /api/public/professionals/{slug}` e usa `POST /api/auth/login` ou `POST /api/auth/register-client`. A conta autenticada precisa ter o papel `client` e estar vinculada ao mesmo profissional da URL. Os cadastros novos armazenam senha com hash; senhas antigas em texto puro são atualizadas para hash no primeiro login válido.

Ao criar uma conta, um cadastro anterior de agendamento público é reaproveitado apenas quando telefone e e-mail correspondem ao cliente daquela agenda.

## Equipe e agendamento por profissional

Execute [`Scheduler.Api/Sql/add_professional_team.sql`](Scheduler.Api/Sql/add_professional_team.sql) uma vez no banco existente para adicionar o vínculo de funcionários com a conta da empresa.

- Configure o segredo `Authentication:SigningKey` com pelo menos 32 bytes. Em desenvolvimento, use User Secrets (`dotnet user-secrets set "Authentication:SigningKey" "<segredo-aleatorio>" --project Scheduler.Api`); em produção, configure `Authentication__SigningKey` como segredo do ambiente. Nunca versionar esse valor.
- Faça login novamente em `POST /api/auth/login` para receber o novo token assinado. Tokens antigos `dev-token-*` deixam de ser aceitos pelos endpoints protegidos. O token expira após 12 horas.
- Todos os endpoints abaixo exigem `Authorization: Bearer {token}` de uma conta com papel `professional`. O backend deriva o dono do token validado; `ownerUserId` enviado pelo frontend não é usado para autorizar.
- `GET /api/professional-team/employees` lista funcionários.
- `POST /api/professional-team/employees` cadastra funcionário com `fullName`, `email`, `password`, `phone`, `specialty` e `timezone`.
- `PUT /api/professional-team/employees/{employeeId}` atualiza cadastro; `password` pode ser omitido e `isActive` controla o acesso.
- `DELETE /api/professional-team/employees/{employeeId}` inativa o funcionário, sem apagar o histórico.

A resposta de cada funcionário contém `id` e `userId` (ambos iguais ao ID da conta), `fullName`, `email`, `phone`, `specialty`, `timezone`, `isActive` e `teamOwnerUserId`.

A conta proprietária precisa estar ativa e ter `hasAppointmentsModule=true`. Funcionários são usuários de papel `employee`, recebem acesso ao calendário próprio e ficam vinculados à conta que os cadastrou. Configure serviços e disponibilidade para cada funcionário usando o respectivo `userId`.

`GET /api/public/professionals/{slug}` passa a incluir `professionals` e `professionalUserId` em cada serviço. `GET /api/public/professionals/{slug}/available-slots` aceita `professionalUserId` como query opcional. Para reservar com `POST /api/public/professionals/{slug}/appointments` ou `/book`, envie `professionalUserId` no corpo junto ao `serviceId`. O profissional selecionado deve pertencer à equipe da agenda e ser o proprietário do serviço; horários e conflitos são verificados no calendário desse funcionário. Se omitido, o endpoint conserva o comportamento antigo e usa o proprietário da agenda.

### Ajustes necessários no frontend

1. Na tela administrativa da empresa, criar uma seção de equipe com listagem, formulário de cadastro/edição e ação para inativar funcionário. Remover `ownerUserId` das URLs e enviar o token da sessão no header `Authorization`.
2. Ao salvar um funcionário, exigir nome, e-mail e senha inicial; permitir telefone, especialidade e fuso horário. A senha não deve ser mostrada novamente após o cadastro.
3. Na agenda pública, carregar `professionals` e `services` de `GET /api/public/professionals/{slug}`. Permitir escolher o profissional e exibir apenas os serviços cujo `professionalUserId` corresponda à seleção.
4. Ao buscar horários, incluir `professionalUserId` na query de `available-slots`; ao reservar, enviar o mesmo ID e o serviço selecionado no corpo.
5. Para calendário/serviços de funcionário autenticado, usar o `user.id` como proprietário do próprio calendário; `teamOwnerUserId` identifica a empresa e não substitui o ID do funcionário.
6. Manter o fluxo de visitante e o login do cliente já existentes; a escolha de funcionário é independente do login do cliente.

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
`massinirenan031@gmail.com`, ative a verificação em duas etapas nessa conta, crie uma **senha de app**
exclusiva para este sistema e guarde-a como segredo. Nunca use a senha normal do Gmail.

No ambiente de desenvolvimento, execute uma vez (substituindo o valor sem compartilhá-lo):

```bash
dotnet user-secrets set "Email:Password" "SUA_NOVA_SENHA_DE_APP" --project Scheduler.Api
```

Em produção, configure o segredo no provedor de hospedagem como a variável de ambiente
`Email__Password`. Mantenha também `Email__Username=massinirenan031@gmail.com` e
`Email__FromEmail=massinirenan031@gmail.com` caso a hospedagem sobrescreva a configuração do projeto.

### Deploy por GitHub Actions e Docker

Antes de executar o deploy, crie estes **Repository secrets** em
`Settings` > `Secrets and variables` > `Actions` no GitHub:

- `SMTP_PASSWORD`: a senha de app recém-criada para a conta remetente.
- `DATABASE_CONNECTION_STRING`: a connection string de produção do banco.

O workflow envia esses valores apenas para a execução remota na VPS e o Docker os injeta no
container em tempo de execução. Eles não entram na imagem nem nos arquivos `appsettings`.

O endpoint de teste de e-mail foi removido, pois ele permitia que qualquer pessoa escolhesse o
destinatário e poderia ser abusado para spam. Os endpoints públicos de agendamento agora têm
limite de cinco tentativas por IP a cada dez minutos.

### Profissional
- e-mail: `renan@email.com`
- senha: `123456`

### Cliente
- e-mail: `cliente@email.com`
- senha: `123456`
## Notificações de agendamentos

O fluxo de agendamento continua enviando e-mail, WhatsApp (quando configurado) e Web Push. Agora ele também cria notificações persistidas no sistema para o profissional e para o cliente.

Antes de publicar esta versão, execute [`Scheduler.Api/Sql/add_app_notifications.sql`](Scheduler.Api/Sql/add_app_notifications.sql) no banco existente. A API expõe:

- `GET /api/notifications?userId={id}&unreadOnly=true` para listar notificações;
- `PATCH /api/notifications/{id}/read?userId={id}` para marcá-las como lidas;
- `POST /api/client/appointments/{id}/response?userId={id}` com `{ "action": "accepted" }` ou `{ "action": "rejected" }` para o cliente aceitar ou recusar o agendamento.

Cada notificação de agendamento inclui `calendarUrl`, um link para adicionar o compromisso ao Google Agenda. O Web Push existente continua sendo disparado junto com a notificação interna.
