# Outbox e idempotência

Os dois padrões que mais evitam incidente em sistema com mensageria. O outbox garante que o evento sai quando o dado foi salvo; a idempotência garante que receber o evento duas vezes não estraga nada.

## O problema da escrita dupla

O [caso de uso de criação de pedido](../Arquitetura/src/Application/UseCases/CreateOrderUseCase.md) do guia faz isto:

```csharp
await _unitOfWork.CommitAsync(ct);                         // 1. salva no banco
await _eventPublisher.PublishAsync(integrationEvent, ct);  // 2. publica no Kafka
```

Se o processo cair entre 1 e 2, o pedido existe e ninguém fica sabendo: o faturamento nunca fatura. Inverter a ordem cria o problema oposto: evento de um pedido que não foi salvo. Banco e broker não participam da mesma transação.

## Transactional outbox

O evento é gravado numa tabela **na mesma transação** do agregado. Um processo separado lê a tabela e publica.

```csharp
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Tipo { get; init; }
    public required string Conteudo { get; init; }
    public DateTime OcorreuEm { get; init; } = DateTime.UtcNow;
    public DateTime? PublicadoEm { get; set; }
}
```

No `SaveChanges`, os domain events viram linhas do outbox:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var eventos = ChangeTracker.Entries<Order>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    foreach (var evento in eventos)
    {
        OutboxMessages.Add(new OutboxMessage
        {
            Tipo = evento.GetType().Name,
            Conteudo = JsonSerializer.Serialize(evento, evento.GetType())
        });
    }

    foreach (var entry in ChangeTracker.Entries<Order>())
        entry.Entity.ClearDomainEvents();

    return await base.SaveChangesAsync(ct); // pedido e evento: tudo ou nada
}
```

E um worker publica o que está pendente:

```csharp
public sealed class OutboxPublisher(IServiceScopeFactory scopes, IProducer<string, string> producer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pendentes = await db.OutboxMessages
                .Where(m => m.PublicadoEm == null)
                .OrderBy(m => m.OcorreuEm)
                .Take(100)
                .ToListAsync(ct);

            foreach (var msg in pendentes)
            {
                await producer.ProduceAsync("pedidos.eventos",
                    new Message<string, string> { Key = msg.Id.ToString(), Value = msg.Conteudo }, ct);
                msg.PublicadoEm = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
```

Se o worker cair depois de publicar e antes de marcar, a mensagem sai de novo. O outbox garante **pelo menos uma vez**, nunca exatamente uma. Por isso o outro lado precisa ser idempotente.

Com várias instâncias, dois workers podem pegar as mesmas linhas. Rode o publicador em uma instância só ou bloqueie as linhas na leitura (`UPDLOCK, READPAST` no SQL Server, `FOR UPDATE SKIP LOCKED` no PostgreSQL). MassTransit, Wolverine e Brighter têm outbox pronto, e o CDC (Debezium lendo o log do banco) é a alternativa sem polling.

## Consumidor idempotente

A forma mais robusta é o **inbox**: registrar o id de cada mensagem processada, na mesma transação do efeito.

```csharp
public async Task HandleAsync(PedidoCriado evento, Guid mensagemId, CancellationToken ct)
{
    if (await db.MensagensProcessadas.AnyAsync(m => m.Id == mensagemId, ct))
        return; // já processada: ignora sem erro

    var fatura = Fatura.Para(evento.PedidoId, evento.Total);
    db.Faturas.Add(fatura);
    db.MensagensProcessadas.Add(new MensagemProcessada(mensagemId, DateTime.UtcNow));

    await db.SaveChangesAsync(ct); // a fatura e o registro da mensagem, juntos
}
```

Uma chave primária na tabela de mensagens processadas resolve a corrida entre dois consumidores: o segundo `INSERT` falha e a transação dele volta.

Outras formas de idempotência, quando o inbox é pesado demais:

- **Restrição única no próprio efeito:** uma fatura por `PedidoId`. A duplicata vira violação de chave, que o consumidor trata como sucesso.
- **Operação naturalmente idempotente:** "definir status como Pago" pode rodar duas vezes; "somar 10 ao saldo" não.
- **Versão do agregado:** ignore eventos com versão menor ou igual à já aplicada.

## Checklist

- Evento gravado na mesma transação do dado (outbox).
- Todo evento tem um id único e estável entre reenvios.
- Consumidor registra o que processou ou tem restrição única no efeito.
- Retry com espera crescente e dead letter para o que nunca vai passar.
- Métrica do tamanho do outbox pendente e da DLQ, com alerta.

## O que perguntam em entrevista

- Como garantir que salvar no banco e publicar o evento aconteçam juntos.
- O que é o outbox pattern e qual garantia ele dá.
- Como tornar um consumidor idempotente.
- Por que "exactly once" é tão difícil na prática.
