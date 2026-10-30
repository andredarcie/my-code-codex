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
O SLA (Service Level Agreement) serve para definir claramente as garantias de desempenho, disponibilidade e suporte de uma API, protegendo o cliente contra falhas e deixando explícito o que a empresa se compromete a entregar. Ele ajuda ao estabelecer expectativas concretas (como tempo de resposta, uptime mínimo e limites de requisição) e fornece base para cobranças, compensações e decisões técnicas, caso a API não funcione como prometido.

Calculos:
uptime = (tempo total - indisponibilidade) / tempo total

### Stack
```text
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
```json
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

```yaml
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

Cache de 30s a 5min já salva SLA e custo.

### APIM (Azure API Management)

O Azure API Management é um gateway gerenciado que fica entre quem chama e quem responde. Em vez de cada serviço falar direto com a API do parceiro, todos falam com o APIM, e ele encaminha. Serve nos dois sentidos:

- **Saída (consumir APIs externas):** um ponto só para as credenciais do parceiro, o limite de chamadas que o contrato permite, o cache e o log de tudo que sai. Trocar de fornecedor ou de URL vira mudança de configuração no gateway, não deploy em cinco serviços.
- **Entrada (expor as suas APIs):** autenticação, rate limit por cliente, versões e um portal de documentação para quem consome, sem reescrever isso em cada API.

#### As peças

| Peça | O que é |
| --- | --- |
| API | a definição importada do OpenAPI, apontando para o backend real |
| Operação | cada endpoint da API, com as próprias políticas se precisar |
| Produto | um pacote de APIs que se publica junto, com termos de uso |
| Assinatura | a chave que dá acesso a um produto, enviada no header `Ocp-Apim-Subscription-Key` |
| Named value | configuração e segredos usados nas políticas, de preferência referenciando o Key Vault |
| Backend | o destino real, com URL, credencial e circuit breaker configurados uma vez |
| Revisão e versão | revisão muda sem quebrar quem consome; versão (`v1`, `v2`) é contrato novo |

#### Políticas

O comportamento é configurado em políticas XML, que rodam em quatro momentos: `inbound` (chegando), `backend` (indo para o destino), `outbound` (voltando) e `on-error`. Elas se aplicam em escopo global, de produto, de API ou de operação, e o `<base />` decide onde entra a política do escopo de cima.

Um exemplo de saída para um parceiro com limite de chamadas e resposta cacheável:

{% raw %}
```xml
<policies>
    <inbound>
        <base />
        <!-- quem chama se identifica com Entra ID, não com a chave do parceiro -->
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
            <openid-config url="https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration" />
            <audiences><audience>api://gateway-parceiros</audience></audiences>
        </validate-jwt>
        <!-- o contrato do parceiro permite 100 chamadas por minuto: o gateway segura antes -->
        <rate-limit-by-key calls="100" renewal-period="60" counter-key="parceiro-frete" />
        <cache-lookup vary-by-developer="false" vary-by-developer-groups="false" />
        <set-backend-service backend-id="parceiro-frete" />
        <!-- a chave do parceiro vem do Key Vault e nunca passa pelo código da aplicação -->
        <set-header name="x-api-key" exists-action="override">
            <value>{{parceiro-frete-api-key}}</value>
        </set-header>
    </inbound>
    <backend>
        <retry condition="@(context.Response.StatusCode >= 500)" count="2" interval="1" first-fast-retry="true">
            <forward-request timeout="5" />
        </retry>
    </backend>
    <outbound>
        <base />
        <cache-store duration="120" />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```
{% endraw %}

Outras políticas que uso com frequência: `quota-by-key` (limite por dia ou mês, não por minuto), `authentication-managed-identity` (o APIM chama um backend Azure com a própria identidade, sem segredo), `set-query-parameter` e `set-body` (adaptar o contrato do parceiro ao seu), `mock-response` (devolver um exemplo enquanto o backend não existe) e `ip-filter`.

#### Como fica no código

Do lado da aplicação, nada muda além da URL base e da autenticação: o `HttpClient` tipado aponta para o APIM em vez do parceiro, e o Polly continua valendo para falhas entre a aplicação e o gateway.

```csharp
builder.Services.AddHttpClient<IFreteClient, FreteClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Apim:BaseUrl"]!); // https://minha-empresa.azure-api.net/frete/
    c.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", builder.Configuration["Apim:SubscriptionKey"]);
})
.AddStandardResilienceHandler();
```

Cuidado com retry nos dois lugares: se o APIM já tenta de novo e a aplicação também, uma falha vira até nove chamadas ao parceiro. Escolha um lugar para o retry (eu deixo no gateway quando o backend é externo) e, na aplicação, só timeout e circuit breaker.

#### Observabilidade

O APIM manda log e métricas para o Application Insights e o Azure Monitor: latência do gateway separada da latência do backend, erros por operação e por assinatura. É a primeira coisa que olho quando um parceiro "está lento": o gráfico mostra se o tempo está no gateway ou do lado de lá.

#### Camadas (tiers)

| Tier | Quando |
| --- | --- |
| Developer | desenvolvimento e testes; não tem SLA, não usar em produção |
| Consumption | serverless, cobra por chamada; bom para tráfego baixo ou irregular, mas sem rede privada e com cold start |
| Basic v2 e Standard v2 | produção comum; o Standard v2 integra com VNet para falar com backends privados |
| Premium (e Premium v2) | várias regiões, zonas de disponibilidade, VNet completa e gateway self-hosted |

O gateway self-hosted é um container do APIM que roda no seu Kubernetes ou on-premises, controlado pelo mesmo portal: útil quando o backend não pode sair da rede interna.

#### Quando não usar

- **Um serviço só, chamando uma API só:** o `HttpClient` com Polly resolve, e o APIM vira custo e um salto de rede a mais.
- **Comunicação entre microsserviços dentro do cluster:** gateway é para a borda. Lá dentro, chamada direta, service mesh ou YARP.
- **Regra de negócio:** política é para segurança, roteamento e adaptação de contrato. Lógica de domínio escondida em XML de gateway não tem teste, não tem code review decente e ninguém acha depois.

Alternativas: YARP (proxy reverso em .NET, dentro da sua aplicação), AWS API Gateway, Kong e o Application Gateway do Azure, que é balanceador com WAF e não gerencia APIs.

## Resiliência no .NET moderno

Desde o .NET 8, o pacote `Microsoft.Extensions.Http.Resilience` (construído sobre o Polly v8) traz uma configuração padrão com timeout total, retry com espera exponencial, circuit breaker e timeout por tentativa, numa linha:

```csharp
builder.Services.AddHttpClient<IMinhaApiService, MinhaApiService>(c =>
        c.BaseAddress = new Uri("https://api.example.com"))
    .AddStandardResilienceHandler();
```

E para ajustar ao SLA do parceiro:

```csharp
.AddStandardResilienceHandler(o =>
{
    o.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(800);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(3);
    o.Retry.MaxRetryAttempts = 2;
});
```

É o que uso em projeto novo. As políticas com `AddTransientHttpErrorPolicy` acima usam a API antiga do Polly (v7), que você vai encontrar na maioria dos sistemas existentes.

## O que perguntam em entrevista

- Por que não instanciar `HttpClient` diretamente.
- Diferença entre retry, timeout e circuit breaker, e em que ordem combinar.
- Por que retry sem limite pode derrubar o parceiro (e você junto).
- Como o SLA do parceiro influencia o seu timeout.
- Quando cachear a resposta de uma API externa.
- Para que serve um API gateway como o Azure API Management, e quando ele é exagero.
- Onde colocar o retry quando existe um gateway entre a aplicação e o parceiro.
