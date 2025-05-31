Implementar Resiliência (Retry, Timeout, Circuit Breaker) Com Polly

```csharp
services.AddHttpClient<IMinhaApiService, MinhaApiService>()
    .AddTransientHttpErrorPolicy(p => 
        p.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
    .AddTransientHttpErrorPolicy(p => 
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

```