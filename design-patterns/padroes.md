# Design patterns

Explicações curtas, com código C# direto ao ponto. Só os padrões que aparecem de verdade em backend .NET; o catálogo completo do Gang of Four tem 23, e a maioria você vai usar sem saber o nome (o `IEnumerable` é um Iterator, o `HttpClient` com handlers é uma Chain of Responsibility).

## Strategy

**Problema:** um comportamento varia conforme o caso (meio de pagamento, tipo de frete, regra de imposto) e o código vira um `switch` gigante.

**Solução:** uma interface, uma implementação por variação, e o DI entrega todas.

```csharp
public interface IFrete
{
    bool Atende(Pedido pedido);
    decimal Calcular(Pedido pedido);
}

public sealed class CalculoDeFrete(IEnumerable<IFrete> fretes)
{
    public decimal Calcular(Pedido pedido) =>
        fretes.First(f => f.Atende(pedido)).Calcular(pedido);
}
```

A partir do .NET 8, keyed services resolvem a estratégia por chave sem fábrica manual:

```csharp
builder.Services.AddKeyedScoped<IFrete, FreteExpresso>("expresso");
builder.Services.AddKeyedScoped<IFrete, FreteEconomico>("economico");

public sealed class Checkout([FromKeyedServices("expresso")] IFrete frete) { ... }
```

## Decorator

**Problema:** adicionar cache, log, retry ou métrica sem tocar na classe original.

**Solução:** uma classe que implementa a mesma interface e envolve a original. No exemplo do guia, o [CachedOrderRepository](../Arquitetura/src/Infra/Cache/CachedOrderRepository.md) põe cache na frente do `IOrderRepository` sem que Domain ou Application saibam.

```csharp
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<IOrderRepository>(sp =>
    new CachedOrderRepository(
        sp.GetRequiredService<OrderRepository>(),
        sp.GetRequiredService<ICacheService>()));
```

A biblioteca Scrutor tem `services.Decorate<IOrderRepository, CachedOrderRepository>()` se o registro manual cansar.

## Factory

**Problema:** criar um objeto exige várias etapas ou validações, e essa lógica se repete pelos casos de uso.

**Solução:** um ponto único de criação. O [OrderFactory](../Arquitetura/src/Domain/Factories/OrderFactory.md) monta os itens e delega ao `Order.Create`, que garante as invariantes.

Prefiro o método estático na própria entidade (`Order.Create`) quando a criação é simples, e a factory separada quando ela depende de outros objetos ou serviços.

## Specification

**Problema:** a mesma regra ("pedido pode ser aprovado") aparece no caso de uso, na consulta e na tela, cada vez escrita de um jeito.

**Solução:** a regra vira objeto, combinável com `And`, `Or` e `Not`. Veja a [OrderCanBeApprovedSpecification](../Arquitetura/src/Domain/Specifications/OrderCanBeApprovedSpecification.md).

Com `Expression<Func<T, bool>>` a mesma specification vira filtro do EF Core e roda no banco, não na memória.

## Adapter (camada anticorrupção)

**Problema:** a API do parceiro tem nomes, formatos e erros que não têm nada a ver com o seu domínio, e eles vazam para dentro do sistema.

**Solução:** um adaptador na Infra traduz o modelo de fora para o seu. Se o parceiro mudar, só o adaptador muda.

```csharp
public sealed class GatewayPagamentoAdapter(IGatewayApi api) : IPagamentoService
{
    public async Task<Result<Pagamento>> CobrarAsync(Cobranca cobranca, CancellationToken ct)
    {
        var resposta = await api.CreateChargeAsync(new ChargeRequest
        {
            AmountInCents = (long)(cobranca.Valor * 100),
            Currency = "BRL",
            ExternalId = cobranca.Id.ToString()
        }, ct);

        return resposta.Status switch
        {
            "paid" => Result.Ok(new Pagamento(cobranca.Id, resposta.Id)),
            "declined" => Result.Fail("Cartão recusado."),
            _ => Result.Fail($"Status inesperado do gateway: {resposta.Status}")
        };
    }
}
```

## Result

**Problema:** falhas esperadas (validação, regra de negócio) tratadas com exceção ficam escondidas e caras.

**Solução:** o método devolve sucesso ou falha no tipo de retorno. Uso FluentResults; o caso de uso do guia retorna `Result<CreateOrderOutput>`. Ver [ASP.NET Core na prática](../backend/aspnet-core.md).

## Outbox

**Problema:** salvar no banco e publicar no Kafka são duas operações; se a segunda falhar, o sistema fica inconsistente.

**Solução:** gravar o evento numa tabela na mesma transação e publicar depois. Tem página própria: [Outbox e idempotência](../mensageria/outbox-idempotencia.md).

## O que não uso

- **Singleton (o padrão GoF):** com DI, o lifetime `Singleton` resolve sem estado global estático.
- **Mediator via MediatR para tudo:** o caso de uso chamado direto é mais fácil de seguir e de debugar.
- **Repository genérico como única abstração:** `IRepository<T>` com `GetAll` incentiva carregar a tabela inteira. Prefiro métodos com intenção (`GetByCustomerIdAsync`).

## O que perguntam em entrevista

- Um padrão que você aplicou e o problema que ele resolveu.
- Diferença entre Strategy e State, ou entre Decorator e Proxy.
- Como adicionar cache a um repositório sem alterar a classe.
- Por que (ou por que não) usar MediatR.
