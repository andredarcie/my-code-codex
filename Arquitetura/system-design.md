# System design

Arquitetura e system design aparecem em quatro de cada dez vagas, e quase toda entrevista para sênior tem uma rodada de "desenhe um sistema que...". O objetivo não é chegar na resposta certa: é mostrar que você pergunta antes de desenhar, conhece as peças e sabe o preço de cada uma.

## Roteiro para a entrevista

1. **Requisitos funcionais** (5 minutos): o que o sistema faz. Corte o escopo em voz alta.
2. **Requisitos não funcionais:** quantos usuários, quantas requisições por segundo, latência aceitável, consistência ou disponibilidade, quanto tempo guardar os dados.
3. **Estimativa de ordem de grandeza:** leituras e escritas por segundo, volume de dados por ano.
4. **Desenho de alto nível:** cliente, API, serviços, banco, cache, fila.
5. **Aprofundar o ponto crítico:** o entrevistador vai puxar um gargalo. É aqui que se ganha a vaga.
6. **Trade-offs:** diga o que você sacrificou e por quê.

## Contas de guardanapo

| Referência | Valor aproximado |
| --- | --- |
| Segundos em um dia | ~86.400 (use 100 mil) |
| 1 milhão de requisições por dia | ~12 por segundo em média, pico de 3 a 5 vezes |
| Leitura em memória | nanossegundos |
| Leitura em SSD | ~100 microssegundos |
| Ida e volta na mesma região da cloud | ~1 milissegundo |
| Ida e volta São Paulo e Virgínia | ~120 milissegundos |

Exemplo: 10 milhões de pedidos por dia são ~115 por segundo, com pico perto de 500. Um SQL Server bem indexado aguenta isso; o gargalo provável é outro.

## As peças e quando usar

**Cache** (Redis): leitura muito maior que escrita, dado que tolera alguns segundos de atraso. Ver [Cache](../performance/cache.md).

**Réplicas de leitura:** muita consulta, pouca escrita. Cuidado com o atraso de replicação: o usuário salva e não vê a própria alteração.

**Particionamento (sharding):** a escrita não cabe num nó só. Escolher a chave de partição é a decisão mais difícil e mais cara de desfazer.

**Fila** (Kafka, RabbitMQ, SQS): desacoplar quem produz de quem processa, absorver picos, processar em segundo plano. Ver [Mensageria](../mensageria/).

**CDN:** arquivos estáticos e conteúdo público perto do usuário.

**Load balancer:** distribuir requisições entre instâncias sem estado. Estado de sessão vai para o Redis, nunca para a memória da instância.

**Rate limiting:** proteger o sistema de cliente abusivo e de você mesmo.

## Teorema CAP, sem decoreba

Numa partição de rede (que vai acontecer), você escolhe entre responder com dado possivelmente desatualizado (disponibilidade) ou recusar até ter certeza (consistência).

- **Saldo bancário, estoque do último item:** consistência.
- **Contador de curtidas, feed, recomendação:** disponibilidade, consistência eventual.

A maioria dos sistemas mistura os dois, decidindo por funcionalidade.

## Problemas clássicos para treinar

- Encurtador de URL: geração de id único, leitura muito maior que escrita, cache.
- Feed de rede social: fan-out na escrita ou na leitura.
- Sistema de pagamentos: idempotência, consistência, auditoria. Ver [APIs REST](../backend/apis-rest.md).
- Rate limiter distribuído: token bucket no Redis.
- Notificações: fila, retry, preferências do usuário, múltiplos canais.
- Reserva de ingressos: concorrência no último lugar, bloqueio temporário.

## Decisões ficam escritas

Em time de verdade, uma decisão de arquitetura vira um ADR (Architecture Decision Record): contexto, decisão, alternativas descartadas e consequências, em uma página. Daqui a um ano, ninguém lembra por que escolheu Kafka. Modelo em [Comunicação escrita](../soft-skills/comunicacao.md).

## Livros

- *Designing Data-Intensive Applications*, Martin Kleppmann. O mais importante da lista; tenho um [resumo em português, capítulo por capítulo](https://andredarcie.github.io/dive/).
- *System Design Interview*, Alex Xu, volumes 1 e 2. Focado em entrevista.
- *Fundamentos da Arquitetura de Software*, Mark Richards e Neal Ford.

## O que perguntam em entrevista

- Desenhe um encurtador de URL que aguente 100 milhões de acessos por dia.
- Como garantir que um pagamento não seja processado duas vezes.
- Quando usar cache e como invalidar.
- SQL ou NoSQL para este caso, e por quê.
- O que acontece com o seu desenho se o banco cair.
