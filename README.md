# My Code Codex

Meu guia pessoal de boas práticas, arquitetura, padrões e comportamento profissional como desenvolvedor .NET. Tudo aqui reflete minha experiência e opinião pessoal, e está em constante evolução conforme aprendo mais.

**Leia no site:** [andredarcie.github.io/my-code-codex](https://andredarcie.github.io/my-code-codex/)

## Código

| Seção | Conteúdo |
| --- | --- |
| [Backend](./backend/) | [APIs REST](./backend/apis-rest.md), [ASP.NET Core na prática](./backend/aspnet-core.md) |
| [Clean code](./design-patterns/) | [SOLID](./design-patterns/solid.md), [Design patterns](./design-patterns/padroes.md) |

## Arquitetura

| Seção | Conteúdo |
| --- | --- |
| [Arquitetura](./Arquitetura/) | [N-Camadas](./Arquitetura/N-camadas.md), [DDD](./Arquitetura/ddd.md), [Microsserviços](./Arquitetura/microsservicos.md), [System design](./Arquitetura/system-design.md) |

## Dados e integração

| Seção | Conteúdo |
| --- | --- |
| [Persistência](./Persistence%20Patterns/) | [Repository](./Persistence%20Patterns/), [EF Core](./Persistence%20Patterns/ef-core.md), [SQL](./Persistence%20Patterns/sql.md) |
| [APIs externas](./Infra/Integração%20com%20APIs/) | IHttpClientFactory, Refit, Polly, SLA |
| [Mensageria](./mensageria/) | [Kafka, RabbitMQ e cia.](./mensageria/), [Outbox e idempotência](./mensageria/outbox-idempotencia.md) |

## Qualidade

| Seção | Conteúdo |
| --- | --- |
| [Testes](./testes/) | xUnit, NSubstitute, TDD, [Testes de integração](./testes/integracao.md) |
| [Segurança](./seguranca/) | JWT, políticas, BOLA, segredos, rate limiting, LGPD |

## Produção

| Seção | Conteúdo |
| --- | --- |
| [Performance](./performance/) | async/await, gargalos, benchmark, [Cache](./performance/cache.md) |
| [Observabilidade](./observabilidade/) | OpenTelemetry, logs estruturados, métricas, health checks |
| [Cloud](./cloud/) | Azure e AWS lado a lado, identidade, Terraform, custo |
| [DevOps](./dev-ops/) | [Git](./dev-ops/git.md), [Docker e Kubernetes](./dev-ops/docker.md), [CI/CD](./dev-ops/ci-cd.md) |

## Carreira

| Seção | Conteúdo |
| --- | --- |
| [Soft skills](./soft-skills/) | feedback, ownership, estimativas, [Comunicação escrita](./soft-skills/comunicacao.md), [Code review](./soft-skills/code-review.md) |
| [Carreira](./carreira/) | o que mais importa, júnior, pleno e sênior, [Entrevistas técnicas](./carreira/entrevistas.md) |
| [Referências](./referencias/) | livros, documentação e ferramentas |

## O site

O GitHub Pages monta o site com Jekyll a partir destes mesmos arquivos Markdown: `_layouts/` tem o modelo das páginas, `assets/css/` o visual e `_data/` a navegação. Para editar o conteúdo, basta editar o `.md`.
