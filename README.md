# Ello Clinic

Plataforma SaaS multi-tenant para gestão de clínicas multidisciplinares. Este repositório contém o MVP operacional em arquitetura de monólito modular, preparado para deploy no Render.

## Stack

- API ASP.NET Core 8, EF Core e PostgreSQL
- SPA Angular 21 LTS responsiva
- JWT, RBAC e filtro global por tenant
- Docker Compose para desenvolvimento e Blueprint do Render para produção

## Executar localmente

```bash
docker compose up --build
```

Acesse `http://localhost:4200`. Usuário inicial: tenant `demo`, e-mail `admin@ello.local`, senha `Ello@123`. Troque a credencial imediatamente fora do ambiente local.

Para executar sem Docker, inicie PostgreSQL, rode `dotnet run --project src/ElloClinic.Api` e, em `web`, execute `npm install` e `npm start`.

## Testes e cobertura

Execute a suíte completa com:

```bash
dotnet test ElloClinic.sln
```

Os testes unitários e de integração validam autenticação, isolamento multi-tenant, perfis de acesso, agenda, confirmação pública, prontuário, relatórios e financeiro. O build falha automaticamente se a cobertura de linhas da API ficar abaixo de 85%. O relatório Cobertura é gerado em `tests/ElloClinic.Api.Tests/TestResults/coverage.cobertura.xml`.

## Estrutura e decisões

O backend concentra módulos no mesmo processo e mantém limites por domínio. Todo agregado operacional deriva de `TenantEntity`; o contexto injeta o tenant do token em inclusões e aplica query filter global nas leituras. Agenda rejeita sobreposição de profissional, paciente ou sala. Evoluções finalizadas são tratadas como registros clínicos imutáveis na evolução planejada do módulo.

O MVP inclui autenticação, dashboard, catálogo clínico, profissionais, pacientes, agenda, evolução e financeiro. Recorrência avançada, anexos em object storage, refresh tokens, MFA, filas, documentos e integração de pagamentos pertencem às fases seguintes descritas no refinamento.

## Deploy no Render

Conecte o repositório ao Render e aplique o `render.yaml`. A API aceita a URL PostgreSQL nativa do Render e expõe `/health`. Para produção, configure domínio, CORS restrito, política de backup, object storage e rotação do `JWT_KEY`.

## Segurança

Dados de saúde exigem DPIA, base legal, retenção, anonimização, backups testados e contratos com operadores antes do uso real. O isolamento lógico implementado é uma camada; testes automatizados de autorização e auditoria de segurança devem integrar o pipeline antes da entrada em produção.
