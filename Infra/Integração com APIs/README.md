# Guia de Integração com APIs externas
Esse guia é para quando você quer integrar com alguma API externa.

## Utilize IHttpClientFactory
Evite instanciar HttpClient diretamente (risco de socket exhaustion). Use IHttpClientFactory com injeção de dependência:

```csharp
services.AddHttpClient<INomeServico, NomeServico>();
```
 Refit usa IHttpClientFactory desde que você registre o cliente via AddRefitClient()

## Biblioteca de interface REST
Refit é uma biblioteca que cria implementações de interfaces REST automaticamente, usando atributos de métodos. Simplifica o consumo de APIs REST.

```csharp
public interface IMinhaApi {
    [Get("/usuarios/{id}")]
    Task<Usuario> GetUsuarioAsync(int id);
}

var api = RestService.For<IMinhaApi>("https://api.exemplo.com");
var usuario = await api.GetUsuarioAsync(1);
```

## Lidar com JSON 
System.Text.Json biblioteca nativa para serialização e desserialização de JSON (a partir do .NET Core 3.0). É mais performática que Newtonsoft.Json.

```csharp
var json = JsonSerializer.Serialize(obj);
var obj = JsonSerializer.Deserialize<MeuTipo>(json);
```

Alerta: Não usar Newtonsoft.Json.      
Ele é mais lento, consome mais memória e não integra nativamente com o pipeline de serialização do .NET moderno (System.Text.Json), que é otimizado, suportado oficialmente e suficiente para a maioria dos casos.

Autenticação via Bearer Token

## Resiliência
Implemente retry, timeout, circuit breaker com Polly:

```csharp
services.AddHttpClient("MinhaApi")
    .AddTransientHttpErrorPolicy(p => 
        p.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2)))
    .AddTransientHttpErrorPolicy(p => 
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

### Retry (Tentativas Automáticas)
Se uma chamada falha (por ex. timeout, 500, etc.), ela é tentada de novo automaticamente.

```csharp
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(3, tentativa => TimeSpan.FromSeconds(2)));
```

## Contrato
SLA significa Service Level Agreement — em português, Acordo de Nível de Serviço.

Exemplo:
```csharp
SLA da API de Pagamentos:
- Disponibilidade: 99,95% mensal
- Tempo máximo de resposta: 800ms (para 90% das requisições)
- Tempo de resposta a chamados críticos: até 2h úteis
```

### Timeouts realistas
Se o SLA diz que 95% das respostas vêm em até 800ms:

```csharp
client.Timeout = TimeSpan.FromMilliseconds(1000); // não coloque 10s se o SLA garante 800ms
```

### Retries compatíveis com taxa de erro
Se o SLA diz que a API pode falhar em até 1% das requisições, configure retries suaves:

```csharp
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(2, _ => TimeSpan.FromMilliseconds(200)));
```

Mais do que isso pode ser overkill ou agravar o problema com efeito cascata (ataque DDoS acidental).