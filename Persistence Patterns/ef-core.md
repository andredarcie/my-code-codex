# EF Core

Em .NET, banco de dados quase sempre significa EF Core no dia a dia e Dapper onde o desempenho aperta. O EF Core é produtivo, mas esconde o SQL: quem não olha o SQL gerado descobre os problemas em produção.

## Veja o SQL que ele gera

```csharp
builder.Services.AddDbContext<AppDbContext>(o =>
{
    o.UseSqlServer(builder.Configuration.GetConnectionString("Pedidos"));

    if (builder.Environment.IsDevelopment())
        o.LogTo(Console.WriteLine, LogLevel.Information).EnableSensitiveDataLogging();
});
```

Para uma consulta só, `query.ToQueryString()` devolve o SQL sem executar.

## Tracking

Consulta que só lê não precisa de change tracking. É memória e CPU desperdiçados:

```csharp
var pedidos = await db.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == clienteId)
    .ToListAsync(ct);
```

No repositório, deixo isso explícito com métodos separados (`GetByIdAsync` e `GetByIdNoTrackingAsync`), como expliquei em [Repository](./).

## Projete, não carregue a entidade inteira

Tela de listagem não precisa de 40 colunas:

```csharp
var resumo = await db.Orders
    .Where(o => o.Status == OrderStatus.Pending)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new PedidoResumo(o.Id, o.CustomerId, o.Total))
    .Take(50)
    .ToListAsync(ct);
```

`Select` com projeção já dispensa o `AsNoTracking`: o EF não rastreia DTO.

## N+1

O problema mais comum de performance com ORM: uma consulta para a lista e mais uma para cada item.

```csharp
// N+1 com lazy loading: 1 consulta de pedidos + 1 por pedido para os itens
foreach (var pedido in await db.Orders.ToListAsync(ct))
    total += pedido.Items.Sum(i => i.Price);

// Uma consulta com JOIN
var pedidos = await db.Orders.Include(o => o.Items).ToListAsync(ct);
```

Deixo lazy loading desligado (é o padrão do EF Core). Carregar relacionamento tem que ser decisão explícita.

### Cartesian explosion

`Include` de duas coleções na mesma consulta multiplica as linhas (10 itens vezes 10 pagamentos = 100 linhas por pedido). `AsSplitQuery()` faz uma consulta por coleção:

```csharp
var pedidos = await db.Orders
    .Include(o => o.Items)
    .Include(o => o.Payments)
    .AsSplitQuery()
    .ToListAsync(ct);
```

## Atualização e exclusão em lote

Desde o EF Core 7, sem carregar as entidades:

```csharp
await db.Orders
    .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < limite)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Expired), ct);
```

Atenção: `ExecuteUpdate` e `ExecuteDelete` vão direto ao banco, fora do change tracking e fora do `SaveChanges`. Regra de negócio do agregado não roda.

## Concorrência otimista

Dois usuários aprovam o mesmo pedido ao mesmo tempo. Sem controle, o último ganha e ninguém percebe.

```csharp
public class Order
{
    [Timestamp]
    public byte[] RowVersion { get; private set; } = [];
}
```

O `SaveChanges` passa a incluir a versão no `WHERE`; se outra transação alterou antes, lança `DbUpdateConcurrencyException`, que a API traduz em `409 Conflict`.

## Migrations em produção

- Gere o script e revise: `dotnet ef migrations script --idempotent`.
- Nunca `Database.Migrate()` na subida da aplicação com várias instâncias: elas competem pela mesma migração. Rode a migração como etapa do pipeline (script ou `dotnet ef migrations bundle`).
- Mudança destrutiva em duas fases: primeiro adiciona a coluna nova e passa a escrever nas duas; depois de todos os deploys, remove a antiga. Assim o rollback não quebra.

## Configuração fora da entidade

Mapeamento com `IEntityTypeConfiguration`, na Infra, para o domínio continuar sem atributos de banco quando possível:

```csharp
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(o => o.Id);
        b.Property(o => o.CustomerId).HasMaxLength(64).IsRequired();
        b.Property(o => o.Total).HasPrecision(18, 2);
        b.HasMany(o => o.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Items).HasField("_items");
        b.HasIndex(o => new { o.CustomerId, o.Status });
    }
}
```

`decimal` sem precisão definida é aviso que vira centavo perdido.

## Quando usar Dapper

Relatórios, consultas com SQL complexo, pontos quentes onde cada milissegundo conta. EF Core para escrita e para o grosso das leituras; Dapper para o resto. Os dois convivem bem no mesmo projeto, inclusive na mesma conexão.

```csharp
var linhas = await conexao.QueryAsync<VendaPorDia>(
    """
    SELECT CAST(CreatedAt AS date) AS Dia, SUM(Total) AS Total
    FROM Orders
    WHERE CreatedAt >= @Inicio
    GROUP BY CAST(CreatedAt AS date)
    """,
    new { Inicio = inicio });
```

## O que perguntam em entrevista

- O que é o problema N+1 e como resolver.
- `AsNoTracking`: o que faz e quando usar.
- Diferença entre `IQueryable` e `IEnumerable` numa consulta do EF.
- Como lidar com dois usuários editando o mesmo registro.
- Como rodar migrations com segurança num sistema com várias instâncias.
