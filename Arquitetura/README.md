# Arquitetura

Como organizo um backend .NET, das camadas de um serviço até a conversa entre vários deles.

- [N-Camadas](./N-camadas.md): Domain, Application, Infra e API, com um exemplo completo de pedido em 27 arquivos de código.
- [DDD](./ddd.md): linguagem ubíqua, bounded contexts, agregados e entidades ricas.
- [Microsserviços](./microsservicos.md): quando valem o custo, como cortar e os padrões que vêm junto.
- [System design](./system-design.md): o roteiro da entrevista, as contas de guardanapo e as peças de um sistema grande.

## Minhas regras de bolso

- A dependência aponta para o domínio. O domínio não conhece EF Core, Kafka nem HTTP.
- Fronteira se decide pelo negócio, não pela tecnologia.
- Comece pelo monolito modular; extraia um serviço quando houver um motivo que você consiga escrever em uma frase.
- Toda decisão difícil de desfazer vira um ADR.
