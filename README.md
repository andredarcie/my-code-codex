# My Code Codex

Meu guia pessoal de boas práticas, arquitetura, padrões e comportamento profissional como desenvolvedor .NET, ordenado pelo que o mercado de trabalho pede. Tudo aqui reflete minha experiência e opinião pessoal, e está em constante evolução conforme aprendo mais.

**Leia no site:** [andredarcie.github.io/my-code-codex](https://andredarcie.github.io/my-code-codex/)

As porcentagens vêm do [Radar de vagas dev](https://andredarcie.github.io/job/): 1.209 anúncios de 9 boards, reunidos em julho de 2026, lidos à procura de 50 requisitos além da stack.

## Código

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Backend](./backend/) | 55% (APIs e integrações) | [APIs REST](./backend/apis-rest.md), [ASP.NET Core na prática](./backend/aspnet-core.md) |
| [Clean code](./design-patterns/) | 41% | [SOLID](./design-patterns/solid.md), [Design patterns](./design-patterns/padroes.md) |

## Arquitetura

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Arquitetura](./Arquitetura/) | 40% | [N-Camadas](./Arquitetura/N-camadas.md), [DDD](./Arquitetura/ddd.md), [Microsserviços](./Arquitetura/microsservicos.md), [System design](./Arquitetura/system-design.md) |

## Dados e integração

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Persistência](./Persistence%20Patterns/) | 53% (banco de dados e SQL) | [Repository](./Persistence%20Patterns/), [EF Core](./Persistence%20Patterns/ef-core.md), [SQL](./Persistence%20Patterns/sql.md) |
| [APIs externas](./Infra/Integração%20com%20APIs/) | 55% (APIs e integrações) | IHttpClientFactory, Refit, Polly, SLA |
| [Mensageria](./mensageria/) | 18% | [Kafka, RabbitMQ e cia.](./mensageria/), [Outbox e idempotência](./mensageria/outbox-idempotencia.md) |

## Qualidade

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Testes](./testes/) | 25% | xUnit, NSubstitute, TDD, [Testes de integração](./testes/integracao.md) |
| [Segurança](./seguranca/) | 8% | JWT, políticas, BOLA, segredos, rate limiting, LGPD |

## Produção

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Performance](./performance/) | 35% | async/await, gargalos, benchmark, [Cache](./performance/cache.md) |
| [Observabilidade](./observabilidade/) | 33% | OpenTelemetry, logs estruturados, métricas, health checks |
| [Cloud](./cloud/) | 41% | Azure e AWS lado a lado, identidade, Terraform, custo |
| [DevOps](./dev-ops/) | 32% (CI/CD) | [Git](./dev-ops/git.md), [Docker e Kubernetes](./dev-ops/docker.md), [CI/CD](./dev-ops/ci-cd.md) |

## Carreira

| Seção | Vagas que pedem | Conteúdo |
| --- | --- | --- |
| [Soft skills](./soft-skills/) | 49% (trabalho em equipe) | feedback, ownership, estimativas, [Comunicação escrita](./soft-skills/comunicacao.md), [Code review](./soft-skills/code-review.md) |
| [O que as vagas pedem](./carreira/) | | o ranking completo, júnior, pleno e sênior, [Entrevistas técnicas](./carreira/entrevistas.md) |
| [Referências](./referencias/) | | livros, documentação e ferramentas |

## O site

O GitHub Pages monta o site com Jekyll a partir destes mesmos arquivos Markdown: `_layouts/` tem o modelo das páginas, `assets/css/` o visual e `_data/` a navegação e os números das vagas. Para editar o conteúdo, basta editar o `.md`.
