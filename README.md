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

- Configure `Authentication:SigningKey` com pelo menos 32 bytes antes de iniciar a API. Em desenvolvimento, use User Secrets (`dotnet user-secrets set "Authentication:SigningKey" "<segredo-aleatorio>" --project Scheduler.Api`); em produção, configure o GitHub Actions secret `AUTHENTICATION_SIGNING_KEY`. O workflow injeta o valor no container como `Authentication__SigningKey` e interrompe o deploy se ele estiver ausente ou curto. Gere um valor aleatório forte e não o versione nem o compartilhe.
- Faça login novamente em `POST /api/auth/login` para receber o novo token assinado. Tokens antigos `dev-token-*` deixam de ser aceitos pelos endpoints protegidos. O token expira após 12 horas.
- Todos os endpoints abaixo exigem `Authorization: Bearer {token}` de uma conta com papel `professional`. O backend deriva o dono do token validado; `ownerUserId` enviado pelo frontend não é usado para autorizar.
- `GET /api/professional-team/employees` lista funcionários.
- `POST /api/professional-team/employees` cadastra funcionário com `fullName`, `email`, `password`, `phone`, `specialty` e `timezone`.
- `PUT /api/professional-team/employees/{employeeId}` atualiza cadastro; `password` pode ser omitido e `isActive` controla o acesso.
- `DELETE /api/professional-team/employees/{employeeId}` inativa o funcionário, sem apagar o histórico.

A resposta de cada funcionário contém `id` e `userId` (ambos iguais ao ID da conta), `fullName`, `email`, `phone`, `specialty`, `timezone`, `isActive` e `teamOwnerUserId`.

A conta proprietária precisa estar ativa e ter `hasAppointmentsModule=true`. Funcionários são usuários de papel `employee`, recebem acesso ao calendário próprio e ficam vinculados à conta que os cadastrou. Configure serviços e disponibilidade para cada funcionário usando o respectivo `userId`.

`GET /api/public/professionals/{slug}` inclui `professionals` e `professionalUserId` em cada serviço. Quando não há funcionários ativos, `professionals` fica vazio e os serviços do proprietário continuam disponíveis; a agenda deve pular a etapa de escolha do profissional. Havendo funcionários ativos, `professionals` inclui o proprietário e os funcionários, para permitir a escolha. `GET /api/public/professionals/{slug}/available-slots` aceita `professionalUserId` como query opcional. Para reservar com `POST /api/public/professionals/{slug}/appointments` ou `/book`, envie `professionalUserId` no corpo junto ao `serviceId`. O profissional selecionado deve pertencer à equipe da agenda e ser o proprietário do serviço; horários e conflitos são verificados no calendário desse funcionário. Se omitido, o endpoint usa o proprietário da agenda.

### Sinal Pix via Mercado Pago

O agendamento público agora inicia uma cobrança Pix do sinal e mantém o horário com status `pending_payment`. A agenda só muda para `confirmed` após o webhook validar a aprovação junto ao Mercado Pago. Cada empresa conecta a própria conta pelo OAuth do Mercado Pago; o sinal é recebido por essa conta. A configuração inicial usa sinal de 20% do preço do serviço e reserva o horário por 30 minutos.

1. Execute [`Scheduler.Api/Sql/add_appointment_payments.sql`](Scheduler.Api/Sql/add_appointment_payments.sql) no banco.
2. Crie uma aplicação de marketplace no Mercado Pago e configure os segredos fora dos arquivos versionados: `MercadoPago__ClientId`, `MercadoPago__ClientSecret`, `MercadoPago__WebhookSecret`, `MercadoPago__WebhookUrl`, `MercadoPago__OAuthCallbackUrl` e `MercadoPago__TokenEncryptionKey`. O callback OAuth deve ser `https://SEU_DOMINIO/api/mercadopago/oauth/callback`; o webhook deve ser `https://SEU_DOMINIO/api/payments/mercadopago/webhook`. Cadastre ambos na aplicação do Mercado Pago e configure o segredo de assinatura do webhook.
3. Gere `TokenEncryptionKey` como uma chave aleatória de 32 bytes codificada em Base64 (por exemplo, `[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))` no PowerShell). Mantenha-a em segredo e estável entre reinícios/instâncias; alterá-la invalida os tokens armazenados e exige reconectar as contas.
4. Para desenvolvimento, use User Secrets para cada configuração (por exemplo, `dotnet user-secrets set "MercadoPago:ClientId" "SEU_CLIENT_ID" --project Scheduler.Api`). Em produção, injete as mesmas configurações como variáveis de ambiente. Use credenciais de teste e contas de teste antes de ativar a conta de produção.
5. O profissional acessa `GET /api/mercadopago/connection` para verificar o vínculo e chama `POST /api/mercadopago/connect` autenticado para obter `authorizationUrl`; o frontend deve abrir essa URL para concluir o OAuth. O callback salva access/refresh tokens cifrados com AES-GCM. As renovações de token são feitas pelo backend.
6. `MercadoPago:DepositPercentage` e `MercadoPago:HoldMinutes` podem ser sobrescritos por configuração; os padrões são `20` e `30`. O sinal precisa resultar em pelo menos R$ 0,50.

As respostas de `/api/public/professionals/{slug}/appointments`, `/book` e `POST /api/client/appointments` incluem `paymentStatus`, `depositAmount`, `pixQrCode`, `pixQrCodeBase64`, `paymentReference` e `paymentExpiresAt`. O frontend deve mostrar QR Code/código Pix e consultar `GET /api/public/payments/{paymentReference}` até receber o status `approved` e o agendamento `confirmed`, ou até o prazo expirar. O endpoint `/book` passa a exigir e-mail, necessário para criar o pagamento. Reservas expiradas são canceladas automaticamente e deixam de bloquear o horário. Pagamentos aprovados após a expiração não confirmam o horário automaticamente; o valor recebido deve ser analisado e, se necessário, reembolsado no Mercado Pago.

### Ajustes necessários no frontend

1. Na tela administrativa da empresa, criar uma seção de equipe com listagem, formulário de cadastro/edição e ação para inativar funcionário. Remover `ownerUserId` das URLs e enviar o token da sessão no header `Authorization`.
2. Ao salvar um funcionário, exigir nome, e-mail e senha inicial; permitir telefone, especialidade e fuso horário. A senha não deve ser mostrada novamente após o cadastro.
3. Na agenda pública, carregar `professionals` e `services` de `GET /api/public/professionals/{slug}`. Se `professionals` estiver vazio, pular a seleção e mostrar os serviços diretamente; se houver funcionários, permitir escolher entre os profissionais e exibir apenas os serviços cujo `professionalUserId` corresponda à seleção.
4. Ao buscar horários, incluir `professionalUserId` na query de `available-slots`; ao reservar, enviar o mesmo ID e o serviço selecionado no corpo.
5. Para calendário/serviços de funcionário autenticado, usar o `user.id` como proprietário do próprio calendário; `teamOwnerUserId` identifica a empresa e não substitui o ID do funcionário.
6. Manter o fluxo de visitante e o login do cliente já existentes; a escolha de funcionário é independente do login do cliente.
7. Na confirmação do agendamento público, exibir e acompanhar o pagamento Pix conforme a seção acima; não tratar `pending_payment` como agendamento confirmado.
8. Na tela de configurações da empresa, consultar `GET /api/mercadopago/connection` e oferecer o botão “Conectar Mercado Pago”, que chama `POST /api/mercadopago/connect` autenticado e abre a URL retornada. Depois do callback OAuth, atualizar o estado de conexão.

## Como rodar

1. Ajuste a connection string em `Scheduler.Api/appsettings.json`.
2. Crie o banco com `Scheduler.Api/Sql/create_database.sql`.
3. Se você já tinha o banco antigo, execute também `Scheduler.Api/Sql/update_auth_client_portal.sql`.
4. Execute os scripts de atualização necessários, incluindo `Scheduler.Api/Sql/add_professional_team.sql` e `Scheduler.Api/Sql/add_appointment_payments.sql`.
5. Rode o seed em `Scheduler.Api/Sql/seed.sql`.
6. Execute:

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
- `AUTHENTICATION_SIGNING_KEY`: segredo aleatório com pelo menos 32 bytes para assinar os tokens de login.

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
