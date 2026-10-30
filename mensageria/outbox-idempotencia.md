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

### O erro comum: consultar antes de gravar

```csharp
// ERRADO: consultar e depois gravar não é atômico
if (await db.MensagensProcessadas.AnyAsync(m => m.Id == mensagemId, ct))
    return;

db.Faturas.Add(Fatura.Para(evento.PedidoId, evento.Total));
db.MensagensProcessadas.Add(new MensagemProcessada(mensagemId, DateTime.UtcNow));
await db.SaveChangesAsync(ct);
```

Se a mesma mensagem chega a dois consumidores ao mesmo tempo (o broker reentregou, ou duas réplicas pegaram a mesma), as duas consultas rodam antes de qualquer `INSERT`, as duas respondem "não existe" e as duas seguem. A consulta é um *check-then-act*: entre o "verificar" e o "agir" existe uma janela em que o mundo muda. Nenhum `SELECT`, por mais rápido, fecha essa janela.

### A garantia vem do banco, não da consulta

Quem decide é a **chave primária** da tabela de mensagens processadas. O banco garante que só um `INSERT` com aquele id passa; o outro falha com violação de chave, e como a fatura está na mesma transação, ela volta junto. O consumidor trata essa falha como "já processada" e confirma a mensagem.

```csharp
public async Task HandleAsync(PedidoCriado evento, Guid mensagemId, CancellationToken ct)
{
    db.MensagensProcessadas.Add(new MensagemProcessada(mensagemId, DateTime.UtcNow));
    db.Faturas.Add(Fatura.Para(evento.PedidoId, evento.Total));

    try
    {
        await db.SaveChangesAsync(ct); // um commit: o registro da mensagem e a fatura, ou nada
    }
    catch (DbUpdateException ex) when (ex.ViolouChaveUnica())
    {
        // outro consumidor gravou esta mensagem antes: nada a fazer, a mensagem pode ser confirmada
    }
}

public static class DbUpdateExceptionExtensions
{
    // SQL Server: 2627 (PRIMARY KEY / UNIQUE constraint) e 2601 (índice único); PostgreSQL: 23505
    public static bool ViolouChaveUnica(this DbUpdateException ex) => ex.InnerException switch
    {
        SqlException sql => sql.Number is 2627 or 2601,
        PostgresException pg => pg.SqlState == PostgresErrorCodes.UniqueViolation,
        _ => false
    };
}
```

```csharp
// no mapeamento: o id da mensagem É a chave primária
modelBuilder.Entity<MensagemProcessada>().HasKey(m => m.Id);
```

O `AnyAsync` pode continuar antes do `INSERT` como atalho, para não montar a fatura à toa no caso comum de reentrega tardia. Mas ele é otimização, não garantia: o código tem de estar correto mesmo sem ele.

Três cuidados:

- **O `DbContext` fica sujo depois da falha.** As entidades continuam rastreadas como `Added`; não reaproveite a mesma instância para outra mensagem. Um escopo de DI por mensagem (o padrão do MassTransit e de um `BackgroundService` bem feito) resolve.
- **Só vale para efeitos dentro do mesmo banco.** Se o handler manda e-mail ou chama uma API, a transação não desfaz isso. Aí a regra é: grave no banco (inclusive o registro da mensagem) e publique o efeito externo pelo [outbox](#transactional-outbox), ou use a chave de idempotência da API de destino (o header `Idempotency-Key` de gateways de pagamento, por exemplo).
- **Isolamento não substitui a chave.** Subir para `SERIALIZABLE` também impede a duplicata, mas à custa de deadlocks e lock de faixa sob carga. A restrição única é mais barata e mais clara.

Outras formas de idempotência, quando o inbox é pesado demais:

- **Restrição única no próprio efeito:** uma fatura por `PedidoId`. A duplicata vira violação de chave, que o consumidor trata como sucesso.
- **Operação naturalmente idempotente:** "definir status como Pago" pode rodar duas vezes; "somar 10 ao saldo" não.
- **Versão do agregado:** ignore eventos com versão menor ou igual à já aplicada.

## Checklist

- Evento gravado na mesma transação do dado (outbox).
- Todo evento tem um id único e estável entre reenvios.
- Consumidor registra o que processou ou tem restrição única no efeito, e a garantia é a restrição do banco, nunca um `SELECT` antes do `INSERT`.
- Retry com espera crescente e dead letter para o que nunca vai passar.
- Métrica do tamanho do outbox pendente e da DLQ, com alerta.

## O que perguntam em entrevista

- Como garantir que salvar no banco e publicar o evento aconteçam juntos.
- O que é o outbox pattern e qual garantia ele dá.
- Como tornar um consumidor idempotente.
- Por que "consultar se já existe e depois gravar" não garante idempotência com dois consumidores ao mesmo tempo.
- Por que "exactly once" é tão difícil na prática.
