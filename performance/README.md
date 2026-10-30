# Performance

Em performance e escala, o que se espera não é micro-otimização: é saber medir, achar o gargalo de verdade e não criar os problemas clássicos que derrubam API sob carga.

## Meça antes de otimizar

Palpite de gargalo erra mais do que acerta. A ordem que sigo:

1. **Métricas de produção:** latência p95 e p99 por endpoint, não a média. Ver [Observabilidade](../observabilidade/).
2. **Tracing** da requisição lenta: quase sempre o tempo está no banco ou numa chamada externa, não no C#.
3. **Profiling** quando o tempo está mesmo no processo: `dotnet-trace`, `dotnet-counters`, o profiler do Visual Studio ou do Rider.
4. **Benchmark** para comparar duas implementações: BenchmarkDotNet, nunca `Stopwatch` num loop.

```csharp
[MemoryDiagnoser]
public class SerializacaoBenchmark
{
    private readonly Pedido _pedido = PedidoDeExemplo.Criar();

    [Benchmark(Baseline = true)]
    public string Newtonsoft() => JsonConvert.SerializeObject(_pedido);

    [Benchmark]
    public string SystemTextJson() => JsonSerializer.Serialize(_pedido);
}
```

## async/await sem armadilhas

`async` não deixa nada mais rápido; libera a thread enquanto espera I/O, e é isso que deixa o servidor atender mais requisições ao mesmo tempo.

- **Nunca bloqueie:** `.Result`, `.Wait()` e `GetAwaiter().GetResult()` seguram a thread. Sob carga, o pool esgota e a API para de responder (thread pool starvation), mesmo com CPU sobrando.
- **Repasse o `CancellationToken`** até a última chamada.
- **Paralelize I/O independente:**

```csharp
// Sequencial: 300 ms + 200 ms
var cliente = await clientes.BuscarAsync(id, ct);
var limite = await credito.ConsultarAsync(id, ct);

// Em paralelo: ~300 ms
var clienteTask = clientes.BuscarAsync(id, ct);
var limiteTask = credito.ConsultarAsync(id, ct);
await Task.WhenAll(clienteTask, limiteTask);
```

Cuidado: o mesmo `DbContext` não aceita duas consultas ao mesmo tempo. Paralelismo vale para chamadas a serviços diferentes.

- **CPU pesada não é async:** cálculo longo bloqueia do mesmo jeito. Leve para um worker ou uma fila.

## Os gargalos mais comuns em API .NET

1. **Consulta sem índice ou N+1.** Ver [EF Core](../Persistence%20Patterns/ef-core) e [SQL](../Persistence%20Patterns/sql).
2. **Carregar demais:** entidade inteira para mostrar três campos, lista sem paginação.
3. **Chamada externa sem timeout:** uma dependência lenta prende todas as threads. Ver [Integração com APIs externas](../Infra/Integração%20com%20APIs/).
4. **`new HttpClient()` por requisição:** esgota sockets. Use `IHttpClientFactory`.
5. **Log demais em produção:** nível Debug serializando objetos grandes em cada requisição.
6. **Falta de cache** em dado lido mil vezes e alterado uma. Ver [Cache](./cache.md).

## Alocação e GC

Em API comum, alocação raramente é o gargalo. Em ponto quente (parsing, serialização, alto volume), vale:

- `StringBuilder` para concatenar em loop.
- `Span<T>` e `ReadOnlySpan<char>` para fatiar sem copiar.
- `ArrayPool<T>.Shared` para buffers temporários grandes.
- Evitar LINQ em laço muito quente; um `for` simples aloca menos.
- `[MemoryDiagnoser]` no BenchmarkDotNet mostra quanto cada versão aloca.

## Resposta HTTP

- **Compressão:** `AddResponseCompression()`, se o proxy na frente já não comprime.
- **Output cache:** resposta inteira em cache no servidor, nativo desde o .NET 7.

```csharp
builder.Services.AddOutputCache(o => o.AddPolicy("catalogo", p => p.Expire(TimeSpan.FromSeconds(30))));
app.UseOutputCache();
app.MapGet("/produtos", ListarProdutos).CacheOutput("catalogo");
```

## Teste de carga

Antes de uma data de pico, não depois. k6 (scripts em JavaScript) e NBomber (em C#) são os mais usados. Defina a meta antes: "500 requisições por segundo com p95 abaixo de 300 ms". Teste de carga sem meta é só um gráfico bonito.

## O que perguntam em entrevista

- Diferença entre concorrência e paralelismo, e o que o `async` resolve.
- O que é thread pool starvation e como acontece.
- Como você investiga um endpoint que ficou lento em produção.
- Quando usar `Task.WhenAll`.
- Por que não instanciar `HttpClient` a cada requisição.
