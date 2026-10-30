# Testes

Com agentes de IA escrevendo código, teste deixou de ser opcional: é a única forma de saber se o que foi gerado faz o que deveria. Escrevi sobre isso em [TDD Is No Longer Optional in the World of AI Agents](https://dev.to/andredarcie/tdd-is-no-longer-optional-in-the-world-of-ai-agents-5b1e).

## A pirâmide, na prática

- **Unitários** (muitos, milissegundos): domínio e casos de uso, com as dependências substituídas.
- **Integração** (alguns, segundos): API de verdade, banco de verdade em container. Ver [Testes de integração](./integracao.md).
- **Ponta a ponta** (poucos, minutos): os fluxos que dão dinheiro, no ambiente mais parecido com produção.

Em backend, os testes de integração da API são os que mais me dão confiança por linha escrita. Se o tempo é curto, comece por eles.

## Stack

| Para | Uso | Observação |
| --- | --- | --- |
| Framework | xUnit | o mais usado em .NET; NUnit e MSTest resolvem igual |
| Dublês | NSubstitute | sintaxe limpa; o Moq continua muito presente nos projetos legados |
| Asserções | Shouldly ou asserts do xUnit | o FluentAssertions passou a ter licença comercial na versão 8 (2025) |
| Integração | `WebApplicationFactory` + Testcontainers | banco real, descartável |
| Dados de teste | Bogus | quando o volume ajuda |
| Cobertura | Coverlet + ReportGenerator | o coletor do Coverlet já vem no template do xUnit |

## Teste de domínio: sem mock nenhum

O agregado não depende de nada, então o teste é só C#:

```csharp
public class OrderTests
{
    [Fact]
    public void Approve_PedidoPendente_MudaParaAprovado()
    {
        var order = Order.Create("cliente-1", [OrderItem.Create("sku-1", 10m, 2)]);

        order.Approve();

        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void Approve_PedidoJaAprovado_LancaDomainException()
    {
        var order = Order.Create("cliente-1", [OrderItem.Create("sku-1", 10m, 2)]);
        order.Approve();

        Assert.Throws<DomainException>(() => order.Approve());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_SemCliente_LancaDomainException(string customerId) =>
        Assert.Throws<DomainException>(() => Order.Create(customerId, [OrderItem.Create("sku-1", 10m, 1)]));
}
```

## Teste de caso de uso: dublês nas bordas

```csharp
public class CreateOrderUseCaseTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();

    private CreateOrderUseCase CriarSut() =>
        new(_repository, _unitOfWork, new OrderDomainService(), new OrderFactory(), _publisher);

    [Fact]
    public async Task ExecuteAsync_ClienteNoLimite_NaoSalvaNemPublica()
    {
        // O OrderDomainService aceita até 3 pedidos pendentes por cliente
        _repository.GetByCustomerIdAsync("cliente-1", Arg.Any<CancellationToken>())
            .Returns(PedidosPendentes("cliente-1", quantidade: 3));

        var resultado = await CriarSut().ExecuteAsync(NovoPedido("cliente-1"));

        Assert.True(resultado.IsFailed);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<OrderCreatedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AbaixoDoLimite_SalvaUmaVezEPublica()
    {
        _repository.GetByCustomerIdAsync("cliente-1", Arg.Any<CancellationToken>())
            .Returns(PedidosPendentes("cliente-1", quantidade: 2));

        var resultado = await CriarSut().ExecuteAsync(NovoPedido("cliente-1"));

        Assert.True(resultado.IsSuccess);
        Assert.Equal(20m, resultado.Value.Total);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    private static IEnumerable<Order> PedidosPendentes(string clienteId, int quantidade) =>
        Enumerable.Range(0, quantidade)
            .Select(_ => Order.Create(clienteId, [OrderItem.Create("sku-1", 10m, 1)]))
            .ToList();

    private static CreateOrderInput NovoPedido(string clienteId) =>
        new(clienteId, [new OrderItemInput("sku-1", 10m, 2)]);
}
```

Mocke só o que sai do processo (banco, fila, HTTP, relógio). Mockar classes do próprio domínio deixa o teste acoplado à implementação: qualquer refatoração quebra tudo sem que o comportamento tenha mudado.

## Boas práticas que mais cobro em code review

- **Nome que conta a história:** `Metodo_Cenario_ResultadoEsperado`.
- **Arrange, Act, Assert** separados por linha em branco. Um Act por teste.
- **Teste determinístico:** nada de `DateTime.Now` direto no código; injete `TimeProvider` (nativo desde o .NET 8) e use `FakeTimeProvider` no teste.
- **Teste independente:** nenhum teste depende da ordem ou do estado deixado por outro.
- **Teste o comportamento, não a implementação:** verificar que `_repository.Received(1).AddAsync(...)` foi chamado é menos útil do que verificar o resultado.

## TDD

Escreva o teste que falha, faça passar do jeito mais simples, refatore. O ganho não é ter teste, é o design: código difícil de testar é código acoplado, e o TDD mostra isso antes de existir.

Com agentes de IA, o ciclo fica ainda melhor: você escreve (ou revisa) os testes que definem o comportamento e deixa o agente implementar até ficarem verdes.

## Cobertura

Cobertura mede o que foi executado, não o que foi verificado. Um teste sem `Assert` dá 100% de cobertura e zero de confiança. Uso a cobertura para achar código de domínio sem teste, não como meta. Se o time exige número, 80% no Domain e na Application é razoável; exigir 80% em controllers e configuração gera teste inútil.

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## O que perguntam em entrevista

- Diferença entre teste unitário e de integração, e quando usar cada um.
- Mock, stub e fake: qual a diferença.
- Como testar um código que usa `DateTime.Now`.
- O que é TDD e quando você aplica.
- Cobertura de 100% garante qualidade?
