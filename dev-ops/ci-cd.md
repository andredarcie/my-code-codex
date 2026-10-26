# CI/CD

Integração contínua: todo push compila e roda os testes, e a branch principal está sempre verde. Entrega contínua: o que passou pelo pipeline pode ir para produção com um clique, ou sozinho. É o que separa "funciona na minha máquina" de "está no ar".

## Um pipeline completo no GitHub Actions

{% raw %}
```yaml
name: pedidos-api

on:
  pull_request:
  push:
    branches: [main]

env:
  IMAGE: ghcr.io/${{ github.repository }}/pedidos-api

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release -warnaserror
      - run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
      - run: dotnet list package --vulnerable --include-transitive

  publish:
    needs: build
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - run: |
          docker build -t $IMAGE:${{ github.sha }} .
          docker push $IMAGE:${{ github.sha }}

  deploy-homologacao:
    needs: publish
    runs-on: ubuntu-latest
    environment: homologacao
    steps:
      - run: echo "implanta $IMAGE:${{ github.sha }} em homologação"

  deploy-producao:
    needs: deploy-homologacao
    runs-on: ubuntu-latest
    environment: producao   # aprovação manual configurada no environment
    steps:
      - run: echo "implanta $IMAGE:${{ github.sha }} em produção"
```
{% endraw %}

Os testes de integração com Testcontainers rodam no mesmo `dotnet test`: o runner Ubuntu já tem Docker. Ver [Testes de integração](../testes/integracao.md).

## Princípios

- **Construa uma vez, implante muitas.** A mesma imagem, identificada pelo commit, passa por homologação e produção. Só a configuração muda. Recompilar para cada ambiente é testar uma coisa e publicar outra.
- **Pipeline rápido.** Mais de dez minutos e as pessoas param de esperar o resultado. Cache de pacotes NuGet, testes em paralelo, jobs independentes.
- **Falhou, para.** Teste quebrado na `main` é a prioridade do time, não um aviso amarelo.
- **Segredo nunca no YAML.** Secrets do GitHub ou, melhor, OIDC com a cloud: o pipeline se autentica na Azure ou na AWS sem nenhuma chave guardada.
- **Migrations como etapa,** antes do deploy da aplicação e compatíveis com a versão anterior. Ver [EF Core](../Persistence%20Patterns/ef-core).

## Estratégias de deploy

| Estratégia | Como funciona | Rollback |
| --- | --- | --- |
| Rolling | troca as instâncias aos poucos | redeploy da versão anterior |
| Blue-green | sobe o ambiente novo inteiro e vira o tráfego de uma vez | virar o tráfego de volta, em segundos |
| Canary | manda 5% do tráfego para a versão nova e observa as métricas | tirar os 5% |
| Feature flag | o código vai desligado; liga por usuário ou por percentual | desligar a flag, sem deploy |

## Azure DevOps

Muitas empresas .NET usam Azure DevOps Pipelines em vez do GitHub Actions. Os conceitos são os mesmos (stages, jobs, steps, environments com aprovação), com YAML de outra sintaxe. Quem entende um aprende o outro em uma tarde.

## O que perguntam em entrevista

- Diferença entre integração contínua, entrega contínua e implantação contínua.
- O que roda no pipeline de um pull request.
- Como você faria rollback de um deploy que deu errado.
- Blue-green e canary: diferenças.
- Como o pipeline acessa a cloud sem guardar credenciais.
