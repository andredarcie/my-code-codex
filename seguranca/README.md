# Segurança

Segurança raramente vem escrita na descrição da tarefa, mas é cobrada sempre: num code review, numa auditoria ou num incidente. Em banco e fintech, junto com a LGPD, é pré-requisito. O ponto de partida é o [OWASP API Security Top 10](https://owasp.org/API-Security/): a maioria dos ataques a APIs cai em poucas categorias.

## Autenticação com JWT

A API não emite token: valida tokens emitidos por um provedor de identidade (Entra ID, Auth0, Keycloak, Cognito, Duende IdentityServer).

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://login.example.com/";  // de onde vêm as chaves públicas
        o.Audience = "pedidos-api";                  // o token tem que ser para esta API
    });
```

O que validar sempre: assinatura, emissor (`iss`), audiência (`aud`) e expiração (`exp`). Com `Authority` configurado, o middleware busca as chaves e valida os quatro.

Erros que vejo com frequência:

- Gerar JWT na própria API com uma chave simétrica fixa no `appsettings.json`.
- Token de acesso com validade de dias. Use minutos e refresh token.
- Guardar dado sensível no payload: o JWT é assinado, não criptografado. Qualquer um lê.

## Autorização por política

Role é grosseira demais para a maioria das regras. Política com claims expressa a permissão:

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PodeAprovarPedido", p => p.RequireClaim("permissao", "pedidos:aprovar"));

grupo.MapPost("/{id:guid}/aprovacao", AprovarAsync).RequireAuthorization("PodeAprovarPedido");
```

## BOLA: o ataque mais comum em APIs

Broken Object Level Authorization é o primeiro do OWASP API Top 10. O usuário autenticado troca o id na URL e lê o pedido de outro cliente:

```csharp
// Vulnerável: qualquer usuário logado lê qualquer pedido
var pedido = await db.Orders.FindAsync([id], ct);

// Correto: o filtro pelo dono faz parte da consulta
var clienteId = user.FindFirstValue("cliente_id");
var pedido = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == clienteId, ct);
if (pedido is null) return TypedResults.NotFound(); // 404, não 403: não confirme que o recurso existe
```

Estar autenticado não é estar autorizado para aquele objeto. Todo endpoint que recebe um id precisa dessa checagem.

## Segredos

- Nunca no código nem no `appsettings.json` versionado.
- Em desenvolvimento: `dotnet user-secrets`.
- Em produção: Azure Key Vault, AWS Secrets Manager ou variáveis injetadas pela plataforma; de preferência com identidade gerenciada, sem senha nenhuma na configuração.
- Vazou no Git? Rotacione a credencial. Apagar o commit não adianta: o histórico e os forks já têm.

```csharp
builder.Configuration.AddAzureKeyVault(new Uri("https://pedidos-kv.vault.azure.net/"), new DefaultAzureCredential());
```

## Entrada do usuário

- **SQL injection:** sempre parâmetros. Ver [SQL](../Persistence%20Patterns/sql).
- **Mass assignment:** nunca faça bind do request direto na entidade. Um `IsAdmin: true` no JSON vira administrador. Use DTO de entrada com só os campos permitidos.
- **Upload:** valide tipo pelo conteúdo, limite tamanho, gere o nome do arquivo no servidor, guarde fora da pasta pública.
- **Desserialização:** não use `TypeNameHandling` do Newtonsoft com dado externo. O System.Text.Json não tem esse risco por padrão.

## Rate limiting

Nativo desde o .NET 7, sem pacote externo:

```csharp
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("por-minuto", l =>
    {
        l.PermitLimit = 100;
        l.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
grupo.RequireRateLimiting("por-minuto");
```

Em endpoint de login e de recuperação de senha, limite por IP e por usuário: é a defesa contra força bruta.

## Transporte e cabeçalhos

- HTTPS em todo lugar, com `UseHsts()` em produção.
- CORS com a lista exata de origens. `AllowAnyOrigin` junto com credenciais nem é permitido pelo navegador, e `AllowAnyOrigin` sozinho raramente é o que você quer.
- Não exponha versão de servidor nem stack trace. O `ProblemDetails` de produção não leva `ex.Message`.

## Dados pessoais e LGPD

- Colete só o necessário e saiba onde cada dado pessoal está.
- Não registre CPF, cartão, senha ou token em log. Mascare na origem.
- Criptografia em trânsito (TLS) e em repouso (TDE no banco, criptografia do storage).
- Trilha de auditoria para acesso a dado sensível.
- Saiba apagar ou anonimizar os dados de um titular quando ele pedir.

## Dependências

```bash
dotnet list package --vulnerable --include-transitive
```

No pipeline, Dependabot ou Renovate abrindo PR de atualização, e a análise de vulnerabilidades do próprio NuGet barrando o build.

## O que perguntam em entrevista

- Diferença entre autenticação e autorização.
- Como funciona o JWT e o que precisa ser validado.
- O que é BOLA (ou IDOR) e como evitar.
- Onde você guarda segredos em produção.
- Como proteger um endpoint de login contra força bruta.
