# ASP.NET Core na prática

O que separa quem "já usou ASP.NET Core" de quem domina é entender o que acontece entre a requisição chegar e o seu código rodar: o pipeline, o container de DI e a configuração.

## O pipeline de middlewares

Cada requisição atravessa os middlewares na ordem em que foram registrados, e a resposta volta na ordem inversa. A ordem importa:

```csharp
var app = builder.Build();

app.UseExceptionHandler();   // primeiro: captura erro de tudo que vem depois
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();     // quem é você
app.UseAuthorization();      // o que você pode fazer, sempre depois da autenticação
app.UseOutputCache();

app.MapPedidos();
app.MapHealthChecks("/health");

app.Run();
```

`UseAuthorization` antes de `UseAuthentication` é bug clássico: a autorização roda sem usuário e tudo vira 401.

## Injeção de dependência e tempo de vida

| Lifetime | Instância | Uso típico |
| --- | --- | --- |
| `Singleton` | uma para a aplicação inteira | cache em memória, clients thread-safe, configuração |
| `Scoped` | uma por requisição | `DbContext`, repositórios, casos de uso, Unit of Work |
| `Transient` | uma a cada injeção | serviços leves e sem estado |

O erro que derruba produção é a **dependência cativa**: um singleton que recebe um scoped. O `DbContext` capturado vive para sempre, compartilhado entre requisições paralelas, e o EF Core lança exceção de concorrência no primeiro pico. Em Development o ASP.NET Core valida escopos e acusa isso na inicialização; não desligue essa validação.

Quando um singleton precisa de algo scoped (um `BackgroundService` que usa o banco, por exemplo), crie o escopo explicitamente:

```csharp
public sealed class ExpiraPedidosWorker(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = scopes.CreateAsyncScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ExpirePendingOrdersUseCase>();
            await useCase.ExecuteAsync(ct);
        }
    }
}
```

## Configuração tipada com Options

```csharp
public sealed class PagamentosOptions
{
    public const string Secao = "Pagamentos";

    [Required, Url] public string BaseUrl { get; init; } = "";
    [Range(1, 30)] public int TimeoutSegundos { get; init; } = 5;
}

builder.Services.AddOptions<PagamentosOptions>()
    .BindConfiguration(PagamentosOptions.Secao)
    .ValidateDataAnnotations()
    .ValidateOnStart(); // configuração errada derruba o deploy, não a primeira requisição
```

Injete `IOptions<T>` quando o valor não muda, `IOptionsSnapshot<T>` (scoped) para reler a cada requisição e `IOptionsMonitor<T>` (singleton) para reagir a mudanças. Segredo nunca vai no `appsettings.json`: veja [Segurança](../seguranca/).

## Minimal APIs ou Controllers

As duas formas são suportadas e performam de forma parecida. A escolha é de organização:

- **Minimal APIs**: menos cerimônia, ótimas para serviços pequenos e microsserviços. Organize com `MapGroup` e um arquivo de endpoints por recurso, senão o `Program.cs` vira um monolito.
- **Controllers**: ainda dominam os sistemas legados que você vai encontrar nas empresas, e muita vaga pede experiência com eles.

```csharp
public static class PedidosEndpoints
{
    public static RouteGroupBuilder MapPedidos(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/pedidos").WithTags("Pedidos").RequireAuthorization();

        grupo.MapPost("/", CriarAsync);
        grupo.MapGet("/{id:guid}", BuscarAsync);

        return grupo;
    }

    private static async Task<Results<Created<CreateOrderOutput>, ValidationProblem, ProblemHttpResult>> CriarAsync(
        CreateOrderRequest request,
        IValidator<CreateOrderRequest> validator,
        CreateOrderUseCase useCase,
        CancellationToken ct)
    {
        var validacao = await validator.ValidateAsync(request, ct);
        if (!validacao.IsValid)
            return TypedResults.ValidationProblem(validacao.ToDictionary());

        var input = new CreateOrderInput(
            request.CustomerId,
            request.Items.Select(i => new OrderItemInput(i.ProductId, i.Price, i.Quantity)).ToList());

        var resultado = await useCase.ExecuteAsync(input, ct);
        if (resultado.IsFailed)
            return TypedResults.Problem(resultado.Errors[0].Message, statusCode: StatusCodes.Status422UnprocessableEntity);

        return TypedResults.Created($"/pedidos/{resultado.Value.OrderId}", resultado.Value);
    }

    // BuscarAsync segue o mesmo formato: Results<Ok<PedidoResponse>, NotFound>
}
```

`TypedResults` deixa o tipo de retorno explícito: o compilador confere e o OpenAPI documenta cada resposta sozinho.

## Validação

Validação de formato (campo obrigatório, tamanho, e-mail válido) fica na borda, com FluentValidation. Regra de negócio (cliente bloqueado não compra) fica no domínio.

```csharp
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("O pedido precisa de ao menos um item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.Price).GreaterThan(0);
        });
    }
}

builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
```

Prefiro chamar o validador explicitamente no endpoint, como no exemplo acima. A validação automática do pacote `FluentValidation.AspNetCore` não é mais recomendada pelos próprios autores.

## Result em vez de exceção

Exceção é para o inesperado. "Cliente atingiu o limite de pedidos" é um resultado esperado do negócio, então volta como `Result`:

```csharp
if (!_domainService.CustomerCanPlaceOrder(existingOrders))
    return Result.Fail("Cliente atingiu o limite de pedidos pendentes.");
```

Uso FluentResults. Exceção para fluxo de negócio é cara, some no meio do código e transforma regra de negócio em `try/catch`.

## async de ponta a ponta

- Nunca `.Result` ou `.Wait()` em código assíncrono: bloqueia thread do pool e, sob carga, esgota o servidor.
- Receba o `CancellationToken` no endpoint e repasse até o `ToListAsync(ct)`. Cliente que desistiu não deve continuar consumindo banco.
- `async void` só em event handler. Em qualquer outro lugar a exceção derruba o processo.

Mais detalhes em [Performance](../performance/).

## O que perguntam em entrevista

- Diferença entre Singleton, Scoped e Transient, e o que acontece quando um singleton depende de um scoped.
- A ordem dos middlewares e por que ela importa.
- Como você valida entrada e onde mora a regra de negócio.
- `IOptions`, `IOptionsSnapshot` e `IOptionsMonitor`.
- O que acontece ao chamar `.Result` numa `Task` dentro de uma requisição.
