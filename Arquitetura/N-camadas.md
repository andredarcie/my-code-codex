# Arquitetura N-Camadas

# Domain
> Não depende de outra

Contém a lógica central e regras de negócio puras da aplicação, totalmente independente de frameworks, infraestrutura ou tecnologia externa, facil de testar.

Só depende de .NET puro (System.*). Nada de EF, nada de HTTP, nada de infra, nada de usar libs externas.

- Entidade (as invariantes de negócio vivem dentro dos seus métodos)
- ValueObject (inclui enums de domínio quando carregam comportamento ou validação)
- Domain Service (usado quando uma regra de negócio não pertence claramente a uma única entidade)
- Repositorios (Interface)
- Agregados (Aggregate Root)
- Domain Events
- Specification (encapsula regras de negócio reutilizáveis e combináveis)
- Factory (responsável por criar Agregados complexos)

# Application
> Depende da Domain

Coordena os casos de uso da aplicação, orquestrando o domínio e definindo interfaces para dependências externas, sem conhecer detalhes de implementação.

- CasoDeUso
- DTO
- InterfaceDeServicoExterno
- Integration Events
- IEventPublisher (Interface)
- IUnitOfWork (Interface)
- ICacheService (Interface)

Recomendo usar FluentResults  
X Não indico usar AutoMapper / Mapster   
X Não indico usar MediatR

# Infra
> Depende da Domain e da Application

Implementa os contratos definidos nas camadas superiores, lidando com detalhes técnicos como persistência, serviços externos e infraestrutura.

- DbContext
- Repositorios (Implementação)
- UnitOfWork (Implementação do IUnitOfWork)
- CacheService (Implementação do ICacheService)
- CachedRepository (Decorator do repositório)
- ServicoExterno
- EventPublisher (Implementação do IEventPublisher)
- Consumers (ex: KafkaConsumer, recebe mensagem e chama CasoDeUso)

## Ferramentais Uteis
- Entity Framework Core
- Dapper (se for usar micro-ORM)
- HttpClient / Refit : chamadas HTTP para serviços externos.
- Serilog / NLog / log4net : logging.
- SendGrid / MailKit / SmtpClient : envio de e-mail.
- Polly : retry e resiliente para chamadas externas.
- MassTransit / Confluent.Kafka : mensageria.

# Presentation/API
> Depende da Application

Expõe os recursos da aplicação ao mundo externo (ex: via HTTP), recebendo requisições, validando dados e delegando o processamento para a Application.

- Controllers
- Request/Response Dtos
- Configs
- Middlewares

## Ferramentas Uteis
- ASP.NET Core WebAPI
- Swashbuckle.AspNetCore (Swagger)
- FluentValidation : para validação automática de modelos.
- JwtBearer : autenticação com JWT.
- ExceptionHandlerMiddleware

# Diagrama

<table border="1" cellpadding="12" cellspacing="0" width="100%">
  <tr>
    <td align="center" width="50%">
      <strong>Presentation/API</strong><br><br>
      <a href="./src/Presentation/Controllers/OrderController">Controller</a><br>
      <a href="./src/Presentation/DTOs/CreateOrderRequest">Request / Response DTO</a><br>
      <a href="./src/Presentation/Middlewares/ExceptionHandlerMiddleware">Middleware</a>
    </td>
    <td align="center" width="50%">
      <strong>Infra</strong><br><br>
      <a href="./src/Infra/Persistence/AppDbContext">DbContext</a><br>
      <a href="./src/Infra/Repositories/OrderRepository">Repositório (Implementação)</a><br>
      <a href="./src/Infra/Persistence/UnitOfWork">Unit of Work</a><br>
      <a href="./src/Infra/Cache/CacheService">Cache Service</a><br>
      <a href="./src/Infra/Cache/CachedOrderRepository">Cached Repository (Decorator)</a><br>
      <a href="./src/Infra/Services/EmailService">Serviço Externo</a><br>
      <a href="./src/Infra/Messaging/EventPublisher">Event Publisher</a><br>
      <a href="./src/Infra/Messaging/KafkaConsumer">Consumer (Kafka)</a>
    </td>
  </tr>
</table>

<div align="center">↓ &nbsp; ambos dependem &nbsp; ↓</div>

<table border="1" cellpadding="12" cellspacing="0" width="100%">
  <tr>
    <td align="center">
      <strong>Application</strong><br><br>
      <a href="./src/Application/UseCases/CreateOrderUseCase">Caso de Uso</a> &nbsp;·&nbsp;
      <a href="./src/Application/UseCases/CreateOrderUseCase">DTO</a> &nbsp;·&nbsp;
      <a href="./src/Application/Events/OrderCreatedIntegrationEvent">Integration Event</a> &nbsp;·&nbsp;
      <a href="./src/Application/Interfaces/IEventPublisher">Interface de Event Publisher</a> &nbsp;·&nbsp;
      <a href="./src/Application/Interfaces/IEmailService">Interface de Serviço Externo</a> &nbsp;·&nbsp;
      <a href="./src/Application/Interfaces/IUnitOfWork">IUnitOfWork</a> &nbsp;·&nbsp;
      <a href="./src/Application/Interfaces/ICacheService">ICacheService</a>
    </td>
  </tr>
</table>

<div align="center">↓ &nbsp; depende &nbsp; ↓</div>

<table border="1" cellpadding="12" cellspacing="0" width="100%">
  <tr>
    <td align="center">
      <strong>Domain</strong><br><br>
      <a href="./src/Domain/Entities/Order">Entidade</a> &nbsp;·&nbsp;
      <a href="./src/Domain/ValueObjects/Money">Value Object</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Entities/Order">Agregado (Aggregate Root)</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Events/OrderCreatedEvent">Domain Event</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Repositories/IOrderRepository">Repositório (Interface)</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Services/OrderDomainService">Domain Service</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Factories/OrderFactory">Factory</a> &nbsp;·&nbsp;
      <a href="./src/Domain/Specifications/OrderCanBeApprovedSpecification">Specification</a>
    </td>
  </tr>
</table>