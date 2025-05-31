# Repository

> O padrão Repository serve para isolar o domínio da lógica de acesso a dados, agindo como uma coleção de objetos em memória. Ele permite consultar, adicionar e remover objetos sem expor os detalhes do banco de dados. É útil principalmente quando há muitas classes de domínio ou consultas complexas, pois centraliza a lógica de consultas e evita repetição. Ajuda a manter a separação clara entre o domínio e a infraestrutura de dados.

Fonte: [Repository by Martin Fowler](https://martinfowler.com/eaaCatalog/repository.html)

Usar repositórios no .NET serve pra separar a regra de negócio de coisas como banco de dados, mensageria e outros mecanismos de infraestrutura. Assim, o domínio não sabe nem que o EF ou o RabbitMQ existem, o código fica mais limpo, fácil de testar, manter e trocar a tecnologia se precisar.

Usar DbContext ou DbSet direto no domínio acopla sua lógica de negócio à infraestrutura, quebra os princípios do DDD e dificulta testes, manutenção e evolução do sistema.

```csharp
```