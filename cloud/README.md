# Cloud

Hoje quase todo serviço roda em cloud. Ninguém espera que um dev backend seja especialista em infraestrutura, mas espera que saiba publicar o próprio serviço, escolher o recurso certo e não deixar uma conta de cinco dígitos no fim do mês.

## Azure ou AWS

No ecossistema .NET brasileiro, Azure é muito comum em empresas que já usam Microsoft; AWS domina em fintechs, varejo e startups. GCP aparece menos. Os conceitos se traduzem quase um para um:

| Preciso de | Azure | AWS |
| --- | --- | --- |
| Rodar container sem gerenciar cluster | Container Apps | ECS com Fargate, App Runner |
| Kubernetes gerenciado | AKS | EKS |
| Rodar a API sem container | App Service | Elastic Beanstalk |
| Função por evento | Azure Functions | Lambda |
| SQL gerenciado | Azure SQL, PostgreSQL Flexible Server | RDS, Aurora |
| NoSQL | Cosmos DB | DynamoDB |
| Cache | Azure Cache for Redis | ElastiCache |
| Fila e tópico | Service Bus, Event Hubs | SQS e SNS, MSK (Kafka) |
| Arquivos | Blob Storage | S3 |
| Segredos | Key Vault | Secrets Manager |
| Identidade da aplicação | Managed Identity | IAM Role |
| Gateway de API | API Management | API Gateway |
| Observabilidade | Azure Monitor, Application Insights | CloudWatch, X-Ray |

## Como escolher onde rodar a API

1. **Container Apps ou ECS Fargate:** o padrão hoje para API e worker. Escala sozinho (o Container Apps, inclusive a zero) e você só entrega a imagem.
2. **App Service:** o mais simples para uma API .NET sem container.
3. **Kubernetes (AKS, EKS):** quando a empresa já tem plataforma e time para operar. Montar um cluster para três serviços é custo sem retorno.
4. **Functions ou Lambda:** processamento por evento e tarefas agendadas. Para API com tráfego constante, cold start e limite de tempo atrapalham.

## Identidade em vez de senha

O serviço se autentica no banco, no Key Vault e no storage com a própria identidade, sem senha na configuração:

```csharp
// Azure: funciona local (az login) e em produção (Managed Identity)
var credencial = new DefaultAzureCredential();
var blobs = new BlobServiceClient(new Uri("https://pedidosarquivos.blob.core.windows.net"), credencial);
```

Na AWS, o equivalente é a IAM Role da tarefa ou do pod; o SDK busca as credenciais sozinho.

## Infraestrutura como código

Recurso criado na mão no portal é recurso que ninguém sabe recriar. Terraform é o mais pedido e funciona nas duas clouds; Bicep (Azure) e CloudFormation ou CDK (AWS) são as opções nativas.

```hcl
resource "azurerm_container_app" "pedidos_api" {
  name                         = "pedidos-api"
  resource_group_name          = azurerm_resource_group.pedidos.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"

  template {
    min_replicas = 1
    max_replicas = 10

    container {
      name   = "api"
      image  = "pedidosacr.azurecr.io/pedidos-api:1.4.0"
      cpu    = 0.5
      memory = "1Gi"
    }
  }
}
```

## Custo

- Escala para baixo fora do horário (ou a zero, em ambiente de teste).
- Ambiente de homologação não precisa do mesmo tamanho de produção.
- Tag em todo recurso (time, projeto, ambiente) para saber quem gasta o quê.
- Alerta de orçamento antes do fim do mês, não a fatura como surpresa.
- Transferência de dados entre regiões e para fora da cloud custa; logs em excesso também.

## Certificações

Ajudam a passar no filtro do RH, principalmente em consultoria, e organizam o estudo. Para backend, as de desenvolvedor: a trilha de developer da Microsoft para Azure e a AWS Certified Developer Associate. Confira o código vigente antes de estudar, porque os exames mudam de nome e de conteúdo. Não substituem ter publicado e operado um serviço de verdade.

## O que perguntam em entrevista

- Como você publicaria esta API na cloud, do código ao ar.
- Onde ficam os segredos e como a aplicação acessa o banco sem senha.
- Diferença entre escalar vertical e horizontal.
- Serverless ou container para este caso, e por quê.
- Como você evita que a conta da cloud dispare.
