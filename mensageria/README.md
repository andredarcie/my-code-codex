# Mensageria

Mensageria aparece em quase todo sistema de empresa grande, banco e fintech: é onde o sistema deixa de ser um request esperando outro. Também é onde mais aparecem bugs difíceis, porque mensagem duplica, atrasa e chega fora de ordem.

## Fila ou tópico

- **Fila (ponto a ponto):** cada mensagem é processada por um consumidor. Trabalho a fazer: "gerar a nota fiscal do pedido 42".
- **Tópico (publish/subscribe):** cada assinante recebe uma cópia. Fato que aconteceu: "pedido 42 foi criado", e faturamento, estoque e e-mail reagem cada um do seu jeito.

Comando é imperativo e tem um dono (`GerarNotaFiscal`). Evento é passado e não sabe quem escuta (`PedidoCriado`).

## Qual broker

| | Kafka | RabbitMQ | Azure Service Bus | AWS SQS + SNS |
| --- | --- | --- | --- | --- |
| Modelo | log particionado e persistente | filas e exchanges | filas e tópicos gerenciados | fila (SQS) e fan-out (SNS) |
| Bom para | alto volume, streaming, reprocessar histórico | roteamento flexível, tarefas | ecossistema Azure, sessões, transações | ecossistema AWS, simplicidade |
| Ordem | garantida dentro da partição | por fila, com um consumidor | com sessions | FIFO opcional |
| Reprocessar | sim, voltando o offset | não, a mensagem sai da fila | não | não |

Nos bancos e fintechs brasileiros, Kafka domina. Em empresas na Azure, Service Bus. Saber o conceito vale mais que a ferramenta: os problemas são os mesmos.

## As garantias de entrega

- **At most once:** pode perder, nunca duplica. Raramente aceitável.
- **At least once:** nunca perde, pode duplicar. O padrão na prática.
- **Exactly once:** só existe de ponta a ponta com muita cooperação. Na prática, at least once com consumidor idempotente.

Conclusão: **todo consumidor tem que ser idempotente.** Processar a mesma mensagem duas vezes não pode cobrar duas vezes. Ver [Outbox e idempotência](./outbox-idempotencia.md).

## Kafka com Confluent.Kafka

Consumidor com commit manual, depois de processar:

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "faturamento",
    EnableAutoCommit = false,              // commit só depois de processar
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("pedidos.criados");

while (!ct.IsCancellationRequested)
{
    var resultado = consumer.Consume(ct);

    await using var scope = scopes.CreateAsyncScope();
    var handler = scope.ServiceProvider.GetRequiredService<PedidoCriadoHandler>();
    await handler.HandleAsync(resultado.Message.Value, ct);

    consumer.Commit(resultado);            // se cair antes daqui, a mensagem volta: at least once
}
```

Pontos que caem em entrevista:

- **A chave define a partição**, e a ordem só é garantida dentro da partição. Use o id do agregado (`pedidoId`) como chave para que os eventos do mesmo pedido cheguem em ordem.
- **Consumidores do mesmo grupo dividem as partições.** Mais consumidores que partições é consumidor parado.
- **Auto commit ligado** confirma a mensagem antes do processamento terminar: se o processo cair, a mensagem se perde.

## Falhas e dead letter

Mensagem que falha sempre (JSON inválido, dado que nunca vai passar na validação) não pode travar a fila para sempre.

1. Retry com espera crescente para falha transitória (banco fora, timeout).
2. Depois de N tentativas, mover para uma **dead letter queue** (DLQ) com o motivo.
3. Alerta na DLQ e uma forma de reprocessar depois da correção.

Separe erro transitório (tenta de novo) de erro permanente (DLQ direto). Tentar dez vezes um JSON quebrado só atrasa a fila.

## Bibliotecas

- **Confluent.Kafka:** o client oficial, mais baixo nível.
- **MassTransit:** abstrai RabbitMQ, Azure Service Bus, SQS e Kafka, com retry, outbox, sagas e testes. Atenção: a partir da versão 9 o MassTransit passou a ter licença comercial; a versão 8 continua open source.
- **Wolverine e Brighter:** alternativas open source com outbox embutido.

## O que perguntam em entrevista

- Diferença entre fila e tópico, comando e evento.
- Como garantir ordem no Kafka.
- O que acontece se o consumidor cair no meio do processamento.
- Como evitar processar a mesma mensagem duas vezes.
- Kafka ou RabbitMQ para um caso específico, e por quê.
