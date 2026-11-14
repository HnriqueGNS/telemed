# Telemed

Plataforma de telemedicina em desenvolvimento, construída com foco em privacidade e proteção de dados de saúde conforme a **LGPD** (Lei nº 13.709/2018).

> **Status: em desenvolvimento inicial.** Ainda não está pronta para uso em produção nem para o tratamento de dados reais de pacientes.

## Objetivo

Oferecer uma base sólida para clínicas e profissionais de saúde com:

- Agendamento de consultas
- Teleconsultas por vídeo
- Prontuário eletrônico
- Gestão de pacientes e médicos
- Emissão de receitas e atestados digitais (assinatura ICP-Brasil)
- Consentimento do titular, auditoria de acessos e controle de permissões

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core (.NET 10), Entity Framework Core |
| Banco de dados | PostgreSQL 16 |
| Autenticação | ASP.NET Core Identity (cookie HttpOnly, 2FA por TOTP planejado) |
| Front-end | React + TypeScript + Vite |
| Infraestrutura local | Docker Compose |
| Logs | Serilog |

## Arquitetura

Monólito modular com Clean Architecture e suporte a múltiplas clínicas (`ClinicId` com filtro global de isolamento).

```
src/
  telemed.Api/             Controllers, middleware, autenticação
  telemed.Application/     Casos de uso e abstrações
  telemed.Domain/          Entidades e regras de domínio
  telemed.Infrastructure/  EF Core, Identity, integrações
web/                       Front-end React
```

## Como rodar localmente

**Pré-requisitos:** .NET SDK 10, Docker Desktop, Node 20+ e Git.

```powershell
# 1. Subir o PostgreSQL
docker compose up -d

# 2. Aplicar as migrations
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/telemed.Infrastructure --startup-project src/telemed.Api

# 3. Rodar a API
dotnet run --project src/telemed.Api
```

A API sobe em `http://localhost:5142`. Verifique em `/health`.

Para o front-end:

```powershell
cd web
npm install
npm run dev
```

> As credenciais em `docker-compose.yml` são **apenas para desenvolvimento local**. Nunca use esses valores em outros ambientes.

## Roadmap

- [x] **Etapa 1:** fundação (solução .NET, PostgreSQL, Identity, perfis, entidades base, multi-clínica)
- [ ] **Etapa 2:** registro/login, 2FA, criptografia de campos sensíveis, auditoria e consentimento
- [ ] **Etapa 3:** cadastros de pacientes, médicos e clínica
- [ ] **Etapa 4:** agenda e agendamento com lembretes
- [ ] **Etapa 5:** teleconsulta por vídeo
- [ ] **Etapa 6:** prontuário eletrônico criptografado e auditado
- [ ] **Etapa 7:** receitas e atestados com assinatura digital
- [ ] **Etapa 8:** pagamentos e painel administrativo

## Princípios de privacidade e segurança

- Dados de saúde são dados pessoais sensíveis (LGPD, art. 11); o projeto os trata com esse rigor desde o desenho.
- Consentimento versionado e revogável.
- Criptografia de campos sensíveis e registro de auditoria de acessos (em desenvolvimento).
- Nenhum dado de saúde em logs, URLs ou e-mails.

## Aviso importante

Este software é fornecido como base técnica e **não garante, por si só, conformidade com a LGPD**. A conformidade depende também de processos organizacionais, política de privacidade, definição de bases legais, encarregado (DPO) e validação jurídica por parte de quem o opera. Observe também as normas do CFM sobre telemedicina e prontuário eletrônico.

## Segurança

Encontrou uma vulnerabilidade? **Não abra uma issue pública.** Entre em contato diretamente pelo e-mail: `seu@email.com`.

## Contribuindo

Contribuições são bem-vindas depois que a base estiver estável. Por enquanto, abra uma issue para discutir ideias antes de enviar um pull request.

## Licença

Defina a licença antes de publicar (por exemplo, MIT ou Apache-2.0) e adicione o arquivo `LICENSE` na raiz. Sem licença, o código é público para leitura, mas ninguém tem permissão legal para usá-lo.
