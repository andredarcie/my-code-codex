# SOLID

Cinco princípios de Robert C. Martin para manter o código fácil de mudar. Em entrevista quase sempre perguntam, e a resposta que impressiona não é a definição decorada: é o problema que cada princípio evita.

## S: Single Responsibility

> Uma classe deve ter um, e apenas um, motivo para mudar.

"Motivo para mudar" é uma pessoa ou área do negócio. Se o financeiro e o time de marketing pedem mudanças na mesma classe, ela tem duas responsabilidades.

```csharp
// Três motivos para mudar: regra de cálculo, formato do PDF, servidor de e-mail
public class FaturaService
{
    public decimal Calcular(Fatura f) { ... }
    public byte[] GerarPdf(Fatura f) { ... }
    public void EnviarPorEmail(Fatura f) { ... }
}

// Cada um muda sozinho
public class CalculadoraDeFatura { public decimal Calcular(Fatura f) { ... } }
public class FaturaPdfRenderer { public byte[] Renderizar(Fatura f) { ... } }
public class EnvioDeFatura(IEmailService email) { public Task EnviarAsync(Fatura f, CancellationToken ct) { ... } }
```

## O: Open/Closed

> Aberto para extensão, fechado para modificação.

Toda vez que entra um meio de pagamento novo você abre o mesmo `switch`? Então cada mudança arrisca quebrar os casos que já funcionavam.

```csharp
public interface ICalculadoraDeTaxa
{
    MeioDePagamento Meio { get; }
    decimal Calcular(decimal valor);
}

public sealed class TaxaPix : ICalculadoraDeTaxa
{
    public MeioDePagamento Meio => MeioDePagamento.Pix;
    public decimal Calcular(decimal valor) => 0m;
}

public sealed class TaxaCartao : ICalculadoraDeTaxa
{
    public MeioDePagamento Meio => MeioDePagamento.Cartao;
    public decimal Calcular(decimal valor) => valor * 0.0299m;
}

// Meio novo = classe nova registrada no DI. Nada existente é editado.
public sealed class CalculoDeTaxas(IEnumerable<ICalculadoraDeTaxa> calculadoras)
{
    public decimal Calcular(MeioDePagamento meio, decimal valor) =>
        calculadoras.Single(c => c.Meio == meio).Calcular(valor);
}
```

## L: Liskov Substitution

> Uma subclasse deve poder substituir a classe base sem quebrar quem a usa.

O sintoma é o `NotImplementedException` ou o `if (x is TipoEspecifico)` no código cliente.

```csharp
public class Conta
{
    public virtual void Sacar(decimal valor) { ... }
}

// Viola Liskov: quem recebe uma Conta não espera exceção ao sacar
public class ContaPoupancaBloqueada : Conta
{
    public override void Sacar(decimal valor) => throw new InvalidOperationException();
}
```

A correção é modelar a capacidade separada: nem toda conta é "sacável", então `Sacar` não pertence à base.

## I: Interface Segregation

> Nenhum cliente deve depender de métodos que não usa.

```csharp
// Quem só lê é obrigado a conhecer escrita, exportação e auditoria
public interface IPedidoService
{
    Pedido Buscar(Guid id);
    void Salvar(Pedido p);
    byte[] ExportarCsv();
    void Auditar(Guid id);
}

// Interfaces pequenas, pelo papel de quem consome
public interface ILeituraDePedidos { Task<Pedido?> BuscarAsync(Guid id, CancellationToken ct); }
public interface IExportacaoDePedidos { Task<byte[]> ExportarCsvAsync(CancellationToken ct); }
```

Interface pequena também deixa o teste simples: o mock tem um método, não dez.

## D: Dependency Inversion

> Módulos de alto nível não dependem de módulos de baixo nível. Ambos dependem de abstrações.

É o princípio que sustenta a [arquitetura em camadas](../Arquitetura/N-camadas.md): o caso de uso depende de `IOrderRepository` (definida no Domain), e a Infra implementa. O domínio não sabe que o SQL Server existe.

```csharp
// Application depende da abstração
public sealed class CreateOrderUseCase(IOrderRepository repository, IUnitOfWork unitOfWork) { ... }

// Infra implementa; o DI conecta as pontas
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
```

Inversão de dependência (o princípio) não é o mesmo que injeção de dependência (a técnica). A injeção é só o jeito mais comum de aplicar a inversão.

## Onde SOLID atrapalha

Interface para uma classe que tem uma implementação e nenhum teste que precise trocá-la é cerimônia. Aplique os princípios onde a mudança realmente acontece: regras de negócio, integrações e pontos de extensão. Um CRUD interno de três telas não precisa de doze abstrações.

## O que perguntam em entrevista

- Explique cada letra com um exemplo do seu dia a dia.
- Qual a diferença entre inversão e injeção de dependência.
- Um exemplo de violação de Liskov.
- Quando aplicar SOLID é exagero.
