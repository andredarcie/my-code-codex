# Microsserviços

Microsserviço resolve um problema de organização, não de tecnologia: deixar times diferentes entregarem partes diferentes do sistema sem coordenar cada deploy. Se você não tem esse problema, provavelmente não precisa da solução. Escrevi sobre isso em [Quando NÃO usar microserviços](https://dev.to/andredarcie/quando-nao-usar-microservicospt-br-4him) e em [Como modelar microserviços corretamente?](https://dev.to/andredarcie/como-modelar-microservicos-corretamente-pt-br-1gog).

## Comece pelo monolito modular

Um monolito bem dividido em módulos (um por [bounded context](./ddd.md)), com fronteiras respeitadas e sem um módulo ler a tabela do outro, entrega quase todos os benefícios de organização com uma fração do custo operacional. Se um módulo precisar escalar ou ser entregue separado, extrair fica fácil porque a fronteira já existe.

## Quando microsserviços fazem sentido

- Vários times precisam entregar em ritmos diferentes sem travar uns aos outros.
- Partes do sistema têm exigências de escala muito diferentes (o checkout em Black Friday contra o cadastro de produtos).
- Requisitos de isolamento: falha ou dado sensível que não pode contaminar o resto.

## O preço

| Monolito | Microsserviços |
| --- | --- |
| Chamada de método | Chamada de rede, que falha, atrasa e precisa de retry |
| Uma transação no banco | Consistência eventual, sagas, compensações |
| Um deploy | Dezenas de pipelines, versões e contratos |
| Um log para ler | Tracing distribuído obrigatório |
| Refatorar é mover código | Refatorar é migrar contrato entre times |

## Como cortar

- **Pelo negócio, não pela camada.** "Serviço de pedidos" sim; "serviço de banco de dados" ou "serviço de validação" não.
- **Cada serviço é dono dos seus dados.** Dois serviços no mesmo banco, lendo as tabelas um do outro, é um monolito distribuído: tem os custos dos dois mundos e os benefícios de nenhum.
- **Comunicação assíncrona por padrão** entre contextos (eventos via [mensageria](../mensageria/)), síncrona só quando o usuário precisa da resposta naquele momento.
- **Se dois serviços sempre mudam juntos, eles são um serviço.**

## Padrões que você vai precisar

- **API Gateway:** ponto único de entrada, autenticação, rate limit e roteamento (YARP, Azure API Management, AWS API Gateway, Kong).
- **Saga:** transação longa feita de passos locais com compensação. Orquestrada (um coordenador manda) ou coreografada (cada serviço reage a eventos).
- **Outbox:** publicar evento sem perder consistência. Ver [Outbox e idempotência](../mensageria/outbox-idempotencia.md).
- **Circuit breaker e timeout:** um serviço lento não pode derrubar os que dependem dele. Ver [Integração com APIs externas](../Infra/Integração%20com%20APIs/).
- **Correlation id e tracing:** sem eles, um erro que atravessa cinco serviços é impossível de rastrear. Ver [Observabilidade](../observabilidade/).

## Saga orquestrada, em uma imagem

```text
Checkout
  1. Pedidos:     criar pedido (pendente)        compensação: cancelar pedido
  2. Estoque:     reservar itens                 compensação: liberar reserva
  3. Pagamentos:  cobrar cartão                  compensação: estornar
  4. Pedidos:     confirmar pedido

Se o passo 3 falhar: liberar reserva (2), cancelar pedido (1).
```

## O que perguntam em entrevista

- Quando você não usaria microsserviços.
- Como manter consistência entre dois serviços sem transação distribuída.
- Saga orquestrada ou coreografada: vantagens de cada.
- O que é um monolito distribuído e como evitar.
- Como você rastreia uma requisição que passa por vários serviços.
