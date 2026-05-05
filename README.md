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

### Profissional
- e-mail: `renan@email.com`
- senha: `123456`

### Cliente
- e-mail: `cliente@email.com`
- senha: `123456`
