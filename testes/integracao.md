# Testes de integração

O teste que sobe a API inteira em memória, com banco de verdade num container, e faz requisições HTTP como um cliente faria. Pega o que o teste unitário não vê: a rota errada, a serialização, o mapeamento do EF Core, a migration, a autorização.

## O que você precisa

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.MsSql
```

A API precisa expor o `Program` para o projeto de teste. Com top-level statements, uma linha no fim do `Program.cs`:

```csharp
public partial class Program { }
```

## A fábrica da aplicação

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(_sql.GetConnectionString()));

            // O que sai do processo e não está no container vira dublê
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, EmailServiceFake>();
        });
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    // Implementação explícita: o DisposeAsync da própria fábrica (que derruba a API) continua sendo chamado
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
```

O container sobe uma vez para a classe de teste (`IClassFixture`) ou para a coleção inteira (`ICollectionFixture`), não uma vez por teste.

## O teste

Os testes abaixo exercitam os endpoints de [ASP.NET Core na prática](../backend/aspnet-core.md): `POST /pedidos` valida com FluentValidation e devolve `201` com o header `Location`.

```csharp
public class PedidosEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_PedidoValido_Retorna201ComLocation()
    {
        var request = new CreateOrderRequest("cliente-1", [new("sku-1", 10m, 2)]);

        var resposta = await _client.PostAsJsonAsync("/pedidos", request);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);

        var criado = await _client.GetFromJsonAsync<PedidoResponse>(resposta.Headers.Location);
        Assert.Equal(20m, criado!.Total);
    }

    [Fact]
    public async Task Post_SemItens_Retorna400ComProblemDetails()
    {
        var resposta = await _client.PostAsJsonAsync("/pedidos", new CreateOrderRequest("cliente-1", []));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await resposta.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Items", problema!.Errors.Keys);
    }
}
```

## Autenticação nos testes

Em vez de gerar JWT de verdade, registre um esquema de autenticação de teste que aceita qualquer requisição com as claims que você definir. Assim o teste cobre a autorização (o `403` de quem não tem permissão) sem depender do provedor de identidade.

## Banco limpo entre testes

Testes que escrevem precisam começar de um estado conhecido:

- **Respawn:** apaga os dados de todas as tabelas entre os testes, rápido e sem recriar o schema.
- **Dados únicos por teste:** cada teste usa ids próprios (`cliente-{Guid}`), e nenhum depende do que o outro criou. É o que mais uso.
- **Transação revertida:** funciona para repositório, mas não para a API, porque a requisição abre a própria conexão.

## Por que não o provider InMemory

O `UseInMemoryDatabase` do EF Core não é um banco relacional: não tem transação de verdade, não valida chave estrangeira, não roda SQL. O teste passa e a produção quebra. A própria documentação da Microsoft desaconselha usá-lo para teste. Com Testcontainers, o banco do teste é o mesmo da produção.

## No pipeline

Testcontainers precisa de Docker. Os runners do GitHub Actions com Ubuntu já têm; no Azure DevOps, use agentes Linux. Ver [CI/CD](../dev-ops/ci-cd.md).

## O que perguntam em entrevista

- Como você testa uma API de ponta a ponta sem depender de ambiente compartilhado.
- Por que não usar banco em memória nos testes.
- Como você isola os dados entre testes.
- O que é o `WebApplicationFactory`.
