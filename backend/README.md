# Backend com ASP.NET Core

APIs e integrações são o requisito mais citado nas vagas que analisei: mais da metade pede. Na prática isso quer dizer saber desenhar um contrato HTTP que outro time consegue consumir sem te chamar no Slack, e saber montar a aplicação por trás dele sem virar um emaranhado.

Esta seção cobre as duas coisas:

- [APIs REST](./apis-rest.md): verbos, status codes, erros com ProblemDetails, paginação, versionamento e idempotência.
- [ASP.NET Core na prática](./aspnet-core.md): pipeline, injeção de dependência, Options, validação, Minimal APIs ou Controllers, OpenAPI.

A organização das camadas está em [Arquitetura N-Camadas](../Arquitetura/N-camadas.md), e o consumo de APIs de terceiros em [Integração com APIs externas](../Infra/Integração%20com%20APIs/).

## O mínimo que espero de um backend .NET pleno

- Uma API com contrato claro: rotas por recurso, status code certo, erro no formato ProblemDetails.
- Validação na borda e regra de negócio no domínio, nunca no controller.
- `async` de ponta a ponta, com `CancellationToken` repassado até o banco.
- Configuração tipada com Options, nada de `IConfiguration["Chave"]` espalhado.
- Logs estruturados e health check, para alguém conseguir operar o serviço às três da manhã.
- Testes de integração cobrindo os endpoints principais.

## Stack que mais aparece nas vagas .NET

| Camada | O que costuma pedir |
| --- | --- |
| API | ASP.NET Core Web API, Minimal APIs, OpenAPI |
| Dados | EF Core, Dapper, SQL Server ou PostgreSQL |
| Cache | Redis, `IMemoryCache`, `HybridCache` |
| Mensageria | Kafka, RabbitMQ, Azure Service Bus, SQS |
| Observabilidade | Serilog, OpenTelemetry, Datadog, New Relic, Grafana |
| Testes | xUnit, NSubstitute ou Moq, Testcontainers |
| Entrega | Docker, Kubernetes, GitHub Actions ou Azure DevOps |
