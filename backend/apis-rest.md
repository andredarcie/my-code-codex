# APIs REST

Uma API é um contrato. Quem consome não lê o seu código: lê a rota, o verbo, o status code e o corpo da resposta. Se esses quatro estão certos e previsíveis, a integração sai em uma tarde.

## Recursos e verbos

Rota é substantivo no plural, verbo é o método HTTP. Ação que não cabe em CRUD vira sub-recurso ou verbo explícito no fim.

| Operação | Rota | Sucesso |
| --- | --- | --- |
| Listar pedidos | `GET /pedidos?status=pendente&pagina=1` | `200 OK` |
| Buscar um pedido | `GET /pedidos/{id}` | `200 OK` ou `404 Not Found` |
| Criar pedido | `POST /pedidos` | `201 Created` com header `Location` |
| Substituir | `PUT /pedidos/{id}` | `200 OK` ou `204 No Content` |
| Alterar parte | `PATCH /pedidos/{id}` | `200 OK` |
| Remover | `DELETE /pedidos/{id}` | `204 No Content` |
| Aprovar | `POST /pedidos/{id}/aprovacao` | `200 OK` |

Evite `POST /criarPedido` e `GET /pedidos/deletar/10`. Verbo na rota esconde a semântica que o HTTP já dá de graça: cache, retry seguro, logs de proxy.

## Status codes que importam

- `400 Bad Request`: o JSON veio quebrado ou faltou campo obrigatório.
- `401 Unauthorized`: não autenticado (sem token ou token inválido).
- `403 Forbidden`: autenticado, mas sem permissão.
- `404 Not Found`: o recurso não existe.
- `409 Conflict`: conflito de estado (pedido já aprovado, versão desatualizada).
- `422 Unprocessable Entity`: o formato está certo, mas a regra de negócio recusou.
- `429 Too Many Requests`: estourou o rate limit.
- `500`: bug seu. Nunca devolva 500 para erro de validação.

Retornar `200` com `{ "sucesso": false }` no corpo é o erro mais comum que vejo em code review. Quebra monitoramento, quebra retry e obriga o cliente a parsear o corpo para saber se deu certo.

## Erros com ProblemDetails

O formato padrão de erro HTTP é a RFC 9457 (Problem Details). O ASP.NET Core já fala esse formato:

```csharp
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
```

Para erros de negócio que você mesmo retorna:

```csharp
return TypedResults.Problem(
    title: "Pedido já aprovado",
    detail: $"O pedido {id} foi aprovado em {pedido.AprovadoEm:dd/MM/yyyy}.",
    statusCode: StatusCodes.Status409Conflict);
```

E para exceções não tratadas, um `IExceptionHandler` centraliza o log e a resposta, no lugar do middleware escrito à mão:

```csharp
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Erro não tratado em {Path}", http.Request.Path);

        http.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await http.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "Erro interno",
            Instance = http.Request.Path
        }, ct);

        return true;
    }
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

Nunca devolva `ex.Message` ou stack trace para o cliente. Isso vaza estrutura interna e é achado clássico de pentest.

## Paginação

Lista sem paginação é incidente marcado para o dia em que a tabela crescer.

- **Offset** (`?pagina=3&tamanho=50`): simples, permite pular para a página 40, mas fica lento em tabela grande e "pula" itens quando alguém insere no meio.
- **Cursor** (`?depois=2026-10-20T10:00:00Z_8812`): estável e rápido em qualquer volume, ideal para feed e sincronização. Não permite pular páginas.

```csharp
public sealed record Pagina<T>(IReadOnlyList<T> Itens, string? ProximoCursor);
```

Sempre coloque um teto no tamanho da página (`Math.Min(tamanho, 100)`).

## Versionamento

Mudança que quebra contrato (remover campo, mudar tipo, mudar significado) exige nova versão. Adicionar campo opcional não exige.

```csharp
// pacote Asp.Versioning.Http
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1);
    o.ReportApiVersions = true;
});

var v1 = app.NewVersionedApi("Pedidos").MapGroup("/v{version:apiVersion}/pedidos").HasApiVersion(1);
```

Versão na URL (`/v1/pedidos`) é a mais fácil de operar: aparece no log, no gateway e no navegador.

## Idempotência

Rede falha no meio. O cliente reenviou o `POST /pagamentos` e agora existem duas cobranças. A defesa é a chave de idempotência:

1. O cliente gera um `Idempotency-Key` (um GUID) por operação e o reenvia nas tentativas.
2. O servidor grava a chave junto com a resposta na mesma transação da operação.
3. Se a chave chegar de novo, devolve a resposta gravada, sem processar outra vez.

`GET`, `PUT` e `DELETE` já são idempotentes por definição. `POST` e `PATCH` não são, e é neles que a chave importa. Em sistema de pagamento isso não é detalhe: é requisito.

## Contrato primeiro

Documente com OpenAPI e trate o documento como parte da entrega. A partir do .NET 9 o próprio framework gera:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi(); // /openapi/v1.json
```

Para a interface de leitura, Scalar ou Swagger UI apontando para esse JSON. O Swashbuckle saiu do template padrão no .NET 9, mas continua funcionando em projeto existente.

## O que perguntam em entrevista

- Diferença entre `PUT` e `PATCH`, e quais verbos são idempotentes.
- Quando usar 400, 409 e 422.
- Como evitar cobrança duplicada quando o cliente faz retry.
- Offset ou cursor para paginar 50 milhões de registros.
- Como evoluir um contrato sem quebrar os clientes atuais.
