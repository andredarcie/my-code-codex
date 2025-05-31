Na maioria dos casos:

Domain -> não depende de nada  
Application -> depende do Domain  
Infra -> implementa interfaces do Domain  
Presentation/API -> depende da Application

Domain -> 
    Entidade
    ValueObject
    InterfaceDeRepositorio
    Enum
    ServicoDeDominio

Application -> 
    CasoDeUso
    DTO
    InterfaceDeServicoExterno

Infra -> 
    DbContext
    Repositorio
    ServicoExterno

Presentation/API -> 
    Controller
    Request
    Response
    Config
    Middleware
