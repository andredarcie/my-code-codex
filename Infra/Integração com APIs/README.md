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
.AddTransientHttpErrorPolicy(p => 
    p.OrResult<HttpResponseMessage>(r => 
        r.StatusCode == HttpStatusCode.RequestTimeout || 
        (int)r.StatusCode >= 500)
    .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(200)));
```

### Retry (Tentativas Automáticas)
Se uma chamada falha (por ex. timeout, 500, etc.), ela é tentada de novo automaticamente.

```csharp
.AddTransientHttpErrorPolicy(policy => 
    policy.WaitAndRetryAsync(3, tentativa => TimeSpan.FromSeconds(2)));
```

## Contrato
O SLA (Service Level Agreement) serve para definir claramente as garantias de desempenho, disponibilidade e suporte de uma API, protegendo o cliente contra falhas e deixando explícito o que a empresa se compromete a entregar. Ele ajuda ao estabelecer expectativas concretas — como tempo de resposta, uptime mínimo e limites de requisição — e fornece base para cobranças, compensações e decisões técnicas, caso a API não funcione como prometido.

Calculos:
uptime = (tempo total - indisponibilidade) / tempo total

### Stack
```csharp
[.NET API]
   ↓
[Serilog + Prometheus-net]
   ↓
[Prometheus ←→ Grafana]
   ↓
[Alertas configurados no Grafana]

+ NBomber para testes de SLA
+ Polly para resiliência
+ AspNetCoreRateLimit para controle de uso
```

Exemplo:
```csharp
{
  "uptime": "99.9%",
  "max_response_time_ms": 500,
  "rate_limit": "1000 RPM",
  "error_rate_max": "0.1%",
  "support": {
    "critical_response_time": "1h",
    "standard_response_time": "24h"
  },
  "compensation": {
    "uptime_below_99.5%": "10% service credit"
  }
}
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

### Stack observabilidade FOSS para .NET

```csharp
version: '3.8'

services:

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    networks:
      - monitoring

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    depends_on:
      - prometheus
    networks:
      - monitoring

  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: jaeger
    ports:
      - "16686:16686"   # UI
      - "6831:6831/udp" # Agent UDP
      - "14268:14268"   # Collector HTTP
    networks:
      - monitoring

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:7.17.10
    container_name: elasticsearch
    environment:
      - discovery.type=single-node
      - ES_JAVA_OPTS=-Xms512m -Xmx512m
    ports:
      - "9200:9200"
    networks:
      - monitoring

  kibana:
    image: docker.elastic.co/kibana/kibana:7.17.10
    container_name: kibana
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch
    networks:
      - monitoring

networks:
  monitoring:
    driver: bridge
```

### Ferramentas

- Prometheus: coleta métricas da API (tempo, erros, uso).
- Grafana: mostra gráficos e envia alertas com base nas métricas.
- Jaeger: rastreia o caminho e tempo de execução das requisições.
- Kibana: exibe logs da aplicação (erros, infos, etc.).

### Cache de Dados
Cache de chamadas externas (quando possível)
APIs externas lentas ou com limite de uso devem ser cacheadas com política TTL.

Sugestão:

IMemoryCache ou IDistributedCache com hash da requisição como chave.

Cache de 30s–5min já salva SLA e custo.

### APIM
Azure API Management