# Docker e Kubernetes

Do dev backend se espera escrever um Dockerfile decente, rodar as dependências locais com Compose e entender o suficiente de Kubernetes para não brigar com quem opera o cluster.

## Dockerfile multi-stage para .NET

```dockerfile
# Etapa 1: compila com o SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar só os .csproj primeiro aproveita o cache do restore
COPY ["src/Pedidos.Api/Pedidos.Api.csproj", "src/Pedidos.Api/"]
COPY ["src/Pedidos.Application/Pedidos.Application.csproj", "src/Pedidos.Application/"]
COPY ["src/Pedidos.Domain/Pedidos.Domain.csproj", "src/Pedidos.Domain/"]
COPY ["src/Pedidos.Infra/Pedidos.Infra.csproj", "src/Pedidos.Infra/"]
RUN dotnet restore "src/Pedidos.Api/Pedidos.Api.csproj"

COPY . .
RUN dotnet publish "src/Pedidos.Api/Pedidos.Api.csproj" -c Release -o /app --no-restore

# Etapa 2: só o runtime, sem SDK, sem shell, sem root
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pedidos.Api.dll"]
```

- **Multi-stage:** a imagem final não leva SDK nem código-fonte. Sai de mais de 800 MB para perto de 120 MB.
- **Imagem chiseled:** Ubuntu sem shell e sem gerenciador de pacotes. Menos superfície de ataque e menos vulnerabilidade no scan.
- **Usuário sem root:** as imagens .NET já trazem o usuário `app`; desde o .NET 8 a porta padrão é 8080 justamente para rodar sem root.
- **`.dockerignore`** com `bin/`, `obj/`, `.git/` e `**/appsettings.Development.json`, senão o `COPY . .` leva lixo e invalida o cache.

O SDK também gera a imagem sem Dockerfile: `dotnet publish -t:PublishContainer`. Bom para começar; o Dockerfile dá mais controle.

## Compose para o ambiente local

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Senha_Local_123"
    ports: ["1433:1433"]

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  kafka:
    image: apache/kafka:3.9.0
    ports: ["9092:9092"]
```

Um `docker compose up -d` e qualquer pessoa do time roda o projeto no primeiro dia. A senha acima é só para a máquina local; nunca a reaproveite. O .NET Aspire é a alternativa em C# ao Compose para orquestrar o ambiente de desenvolvimento.

## Kubernetes: o mínimo

| Objeto | Para que serve |
| --- | --- |
| Pod | um ou mais containers rodando juntos |
| Deployment | mantém N réplicas do pod e faz rolling update |
| Service | endereço estável para os pods de um Deployment |
| Ingress | expõe HTTP para fora do cluster |
| ConfigMap e Secret | configuração e segredos injetados no pod |
| HorizontalPodAutoscaler | aumenta e diminui réplicas pela carga |

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pedidos-api
spec:
  replicas: 3
  selector:
    matchLabels: { app: pedidos-api }
  template:
    metadata:
      labels: { app: pedidos-api }
    spec:
      containers:
        - name: api
          image: pedidosacr.azurecr.io/pedidos-api:1.4.0
          ports: [{ containerPort: 8080 }]
          resources:
            requests: { cpu: 250m, memory: 256Mi }
            limits: { memory: 512Mi }
          readinessProbe:
            httpGet: { path: /health/ready, port: 8080 }
          livenessProbe:
            httpGet: { path: /health/live, port: 8080 }
```

O que mais causa problema numa aplicação .NET no cluster:

- **Sem `requests` e `limits`:** o pod é o primeiro a ser despejado, ou consome o nó inteiro. O .NET respeita o limite de memória do container para ajustar o GC.
- **Liveness checando o banco:** o banco oscila e o Kubernetes reinicia todas as réplicas. Dependência externa vai no readiness. Ver [Observabilidade](../observabilidade/).
- **Desligamento abrupto:** o pod recebe `SIGTERM` e tem 30 segundos. O ASP.NET Core termina as requisições em andamento; `BackgroundService` precisa respeitar o `CancellationToken`.
- **Estado em memória:** com três réplicas, a sessão guardada na memória de uma some quando a requisição cai em outra.

## O que perguntam em entrevista

- Diferença entre imagem e container, e entre container e máquina virtual.
- Por que usar multi-stage build.
- Diferença entre liveness e readiness probe.
- Como a aplicação recebe configuração e segredos no Kubernetes.
- O que acontece com as requisições em andamento durante um deploy.
