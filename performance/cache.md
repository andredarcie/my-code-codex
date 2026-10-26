# Cache

Cache é a otimização com melhor retorno e a que mais gera bug: dado velho na tela, invalidação esquecida, cache que derruba o banco quando expira. A frase de Phil Karlton continua certa: há duas coisas difíceis em computação, invalidar cache e dar nome às coisas.

## Quando vale

- Leitura muito mais frequente que escrita (catálogo, configuração, permissões).
- O dado tolera alguns segundos ou minutos de atraso.
- A fonte é cara ou lenta (consulta pesada, API externa com limite de uso).

Saldo, estoque do último item e qualquer coisa que decide dinheiro em tempo real: não, ou com muito cuidado.

## Onde guardar

| | `IMemoryCache` | `IDistributedCache` (Redis) | `HybridCache` |
| --- | --- | --- | --- |
| Onde fica | memória da instância | servidor separado | os dois: memória primeiro, Redis depois |
| Velocidade | nanossegundos | ~1 ms (rede) | nanossegundos no acerto local |
| Várias instâncias | cada uma com o seu | compartilhado | compartilhado, com cópia local |
| Proteção contra stampede | não | não | sim |

Com uma instância só, `IMemoryCache` resolve. Com várias, Redis. O `HybridCache` (pacote `Microsoft.Extensions.Caching.Hybrid`, estável desde 2025) junta os dois e é o que uso em projeto novo.

## HybridCache

```csharp
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddHybridCache(o =>
{
    o.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),          // no Redis
        LocalCacheExpiration = TimeSpan.FromMinutes(1) // na memória da instância
    };
});
```

```csharp
public sealed class CatalogoService(HybridCache cache, AppDbContext db)
{
    public async Task<ProdutoDto?> BuscarAsync(Guid id, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"produto:{id}",
            async token => await db.Products
                .Where(p => p.Id == id)
                .Select(p => new ProdutoDto(p.Id, p.Nome, p.Preco))
                .FirstOrDefaultAsync(token),
            cancellationToken: ct);

    public async Task AtualizarPrecoAsync(Guid id, decimal preco, CancellationToken ct)
    {
        // ... salva no banco
        await cache.RemoveAsync($"produto:{id}", ct);
    }
}
```

## Estratégias

- **Cache-aside** (o exemplo acima): lê do cache; se não tem, busca na fonte e guarda. A mais comum.
- **Write-through:** escreve no cache junto com o banco. Leitura sempre quente, escrita mais lenta.
- **Decorator no repositório:** o cache fica invisível para quem usa. É o [CachedOrderRepository](../Arquitetura/src/Infra/Cache/CachedOrderRepository.md) do guia.

## Invalidação

1. **Expiração (TTL):** a mais simples e a que sempre deve existir, mesmo com as outras. Escolha o TTL pelo atraso que o negócio aceita.
2. **Remoção na escrita:** apagou ou alterou, remove a chave. Só funciona se toda escrita passa pelo mesmo código.
3. **Por evento:** outro serviço alterou o dado e publica um evento; quem tem cache escuta e remove.
4. **Por tag:** o `HybridCache` aceita tags (`RemoveByTagAsync("catalogo")`) para invalidar um grupo de uma vez.

## Os três problemas clássicos

- **Stampede (dogpile):** a chave popular expira e mil requisições vão ao banco ao mesmo tempo. O `HybridCache` deixa só uma buscar e as outras esperam o resultado. Sem ele, use um lock por chave ou renove antes de expirar.
- **Penetração:** consultas por ids que não existem sempre passam direto pelo cache. Guarde também o "não existe" por pouco tempo.
- **Avalanche:** muitas chaves criadas juntas expiram juntas. Some um tempo aleatório ao TTL.

## Chaves

- Prefixo por tipo e versão: `v2:produto:{id}`. Mudou o formato do DTO, mude a versão e o cache antigo fica para trás sem erro de desserialização.
- Nunca dado do usuário na chave sem normalizar.
- Cache de dado por usuário leva o usuário na chave, ou um vê o dado do outro.

## O que perguntam em entrevista

- Quando não usar cache.
- Como você invalida um cache distribuído.
- O que é cache stampede e como evitar.
- Diferença entre cache em memória e distribuído numa aplicação com várias instâncias.
