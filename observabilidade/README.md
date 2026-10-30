# Observabilidade

Quem já ficou de plantão sabe por que observabilidade importa: sem ela, um incidente vira adivinhação. Observabilidade é conseguir responder "por que está lento para este cliente agora?" sem fazer deploy de log novo.

## Os três sinais

| Sinal | Responde | Ferramentas comuns |
| --- | --- | --- |
| **Logs** | o que aconteceu, em detalhe | Serilog, Seq, Elasticsearch e Kibana, Loki |
| **Métricas** | quanto, com que frequência, quão rápido | Prometheus e Grafana, Azure Monitor, CloudWatch |
| **Traces** | por onde a requisição passou e quanto tempo em cada parte | Jaeger, Tempo, Zipkin, Application Insights |

Datadog, New Relic, Dynatrace e Application Insights juntam os três numa plataforma paga, e são comuns nas empresas.

## OpenTelemetry: o padrão

O OpenTelemetry é o padrão aberto para gerar os três sinais. A aplicação instrumenta uma vez e exporta via OTLP para qualquer backend; trocar de Datadog para Grafana vira configuração, não reescrita.

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("pedidos-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("Pedidos"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Pedidos"))
    .UseOtlpExporter(); // endpoint em OTEL_EXPORTER_OTLP_ENDPOINT

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
});
```

Com isso, toda requisição HTTP, chamada externa e consulta SQL vira um span, e os logs saem com o `TraceId`: do log de erro você chega ao trace inteiro.

Para testar local, o dashboard do .NET Aspire roda num container e recebe OTLP sem configurar mais nada.

## Logs estruturados

```csharp
// Ruim: texto interpolado, impossível de filtrar por campo
logger.LogInformation($"Pedido {pedido.Id} criado para {pedido.CustomerId}");

// Bom: message template; PedidoId e ClienteId viram campos pesquisáveis
logger.LogInformation("Pedido {PedidoId} criado para {ClienteId}", pedido.Id, pedido.CustomerId);
```

Em ponto quente, o `LoggerMessage` gerado em tempo de compilação evita alocação:

```csharp
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Limite de pedidos atingido para {ClienteId}")]
    public static partial void LimiteAtingido(ILogger logger, string clienteId);
}
```

Regras de nível:

- **Error:** algo falhou e alguém precisa olhar.
- **Warning:** degradou, mas se recuperou (retry que funcionou, fallback).
- **Information:** eventos de negócio relevantes (pedido criado, pagamento aprovado).
- **Debug:** detalhe para investigar; desligado em produção.

Nunca logue senha, token, cartão ou CPF completo. Ver [Segurança](../seguranca/).

## Métricas de negócio

Métrica técnica diz que a API está de pé; métrica de negócio diz se ela está fazendo o trabalho.

```csharp
public sealed class PedidosMetrics
{
    private readonly Counter<long> _criados;

    public PedidosMetrics(IMeterFactory meters)
    {
        var meter = meters.Create("Pedidos");
        _criados = meter.CreateCounter<long>("pedidos.criados", description: "Pedidos criados");
    }

    public void PedidoCriado(string canal) => _criados.Add(1, new KeyValuePair<string, object?>("canal", canal));
}
```

"Pedidos criados por minuto caiu a zero" é o alerta que pega o bug que nenhum `500` mostrou.

## Health checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: ["ready"]);

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });                  // o processo responde
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") }); // pode receber tráfego
```

O Kubernetes usa o `live` para reiniciar o container e o `ready` para tirar a instância do balanceador. Não coloque dependência externa no `live`: o banco caiu e o Kubernetes reinicia todas as instâncias em loop. Ver [Docker e Kubernetes](../dev-ops/docker.md).

## Alertas que valem a pena

- Alerte em **sintoma** (taxa de erro, latência p99, fila crescendo), não em causa (CPU a 80%).
- Todo alerta precisa de uma ação. Alerta que todo mundo ignora treina o time a ignorar alerta.
- SLO definido com o negócio: "99,9% das requisições abaixo de 500 ms em 30 dias". O alerta dispara quando o orçamento de erro está sendo gasto rápido demais.

## O que perguntam em entrevista

- Diferença entre log, métrica e trace.
- O que é log estruturado e por que importa.
- Como rastrear uma requisição que passa por vários serviços.
- O que você monitora numa API recém-publicada.
- Diferença entre liveness e readiness.
