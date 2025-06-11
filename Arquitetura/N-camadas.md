# Arquitetura N-Camadas

# Domain
> Não depende de outra

Contém a lógica central e regras de negócio puras da aplicação, totalmente independente de frameworks, infraestrutura ou tecnologia externa, facil de testar.

Só depende de .NET puro (System.*). Nada de EF, nada de HTTP, nada de infra, nada de usar libs externas.

- Entidade
- ValueObject
- Enum
- Domain Service (Usado quando uma regra de negócio não pertence claramente a uma única entidade.)
- Repositorios (Interface)
- Agregados (Aggregate Root)
- Domain Events
- Invariante de negócio (Dentro de métodos de entidade ou serviços de domínio.)
 
# Application
> Depende da Domain

Coordena os casos de uso da aplicação, orquestrando o domínio e definindo interfaces para dependências externas, sem conhecer detalhes de implementação.

- CasoDeUso
- DTO
- InterfaceDeServicoExterno

Recomendo usar FluentResults  
X Não indico usar AutoMapper / Mapster   
X Não indico usar MediatR

# Infra
> Depende da Domain

Implementa os contratos definidos nas camadas superiores, lidando com detalhes técnicos como persistência, serviços externos e infraestrutura.

- DbContext
- Repositorios (Implementação)
- ServicoExterno

## Ferramentais Uteis
- Entity Framework Core
- Dapper (se for usar micro-ORM)
- HttpClient / Refit – chamadas HTTP para serviços externos.
- Serilog / NLog / log4net – logging.
- SendGrid / MailKit / SmtpClient – envio de e-mail.
- Polly – retry e resiliente para chamadas externas.

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
- FluentValidation – para validação automática de modelos.
- JwtBearer – autenticação com JWT.
- ExceptionHandlerMiddleware