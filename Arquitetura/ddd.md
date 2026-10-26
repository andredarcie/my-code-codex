# DDD

Domain-Driven Design é a abordagem de Eric Evans para software com regra de negócio complexa: o código fala a língua do negócio e a estrutura do sistema segue as fronteiras do negócio. Escrevi um artigo inteiro sobre a parte de implementação: [Entenda DDD de uma vez por todas, na implementação](https://dev.to/andredarcie/entenda-ddd-de-uma-vez-por-todas-na-implementacao-pt-br-3776).

DDD tem duas metades. A estratégica decide **onde** cortar o sistema; a tática decide **como** escrever o código dentro de cada pedaço. Muita gente pula a estratégica e fica só com as classes, e aí vira "anemic domain com nome bonito".

## DDD estratégico

### Linguagem ubíqua

O mesmo termo, com o mesmo significado, na conversa com o negócio, no código e no banco. Se o negócio diz "proposta" e o código diz `Quote`, alguém vai traduzir errado num dia ruim.

### Bounded context

A fronteira dentro da qual um termo tem um significado só. "Cliente" no contexto de Vendas tem limite de crédito e histórico de compras; no contexto de Entregas tem endereço e janela de horário. Forçar um único `Cliente` para os dois cria uma classe que todo mundo altera e ninguém entende.

Cada bounded context tem seu modelo, seu banco (ou schema) e, muitas vezes, seu time. É o corte natural para [microsserviços](./microsservicos.md), quando eles fazem sentido.

### Context map

Como os contextos conversam: um publica eventos e o outro consome, um consome a API do outro através de uma camada anticorrupção, dois compartilham um kernel pequeno. Desenhar isso antes de codar evita o acoplamento que só aparece no primeiro incidente.

## DDD tático

| Bloco | O que é | No exemplo do guia |
| --- | --- | --- |
| Entidade | Tem identidade que atravessa o tempo | [Order](./src/Domain/Entities/Order.md) |
| Value Object | Definido só pelos valores, imutável | [Money](./src/Domain/ValueObjects/Money.md) |
| Agregado | Grupo de objetos com uma raiz que garante as invariantes | `Order` com seus `OrderItem` |
| Domain Event | Algo que aconteceu e importa para o negócio | [OrderCreatedEvent](./src/Domain/Events/OrderCreatedEvent.md) |
| Repositório | Coleção de agregados, um por raiz | [IOrderRepository](./src/Domain/Repositories/IOrderRepository.md) |
| Domain Service | Regra que não pertence a uma entidade só | [OrderDomainService](./src/Domain/Services/OrderDomainService.md) |
| Factory | Criação complexa de agregados | [OrderFactory](./src/Domain/Factories/OrderFactory.md) |
| Specification | Regra reutilizável e combinável | [OrderCanBeApprovedSpecification](./src/Domain/Specifications/OrderCanBeApprovedSpecification.md) |

## Regras de agregado que mais evitam bug

1. **Só a raiz é acessada de fora.** Ninguém adiciona `OrderItem` direto na lista; chama `order.AddItem(...)`, que valida.
2. **Uma transação altera um agregado.** Precisa mudar dois? O segundo reage a um domain event, em outra transação.
3. **Referencie outros agregados por id**, não por navegação. `Order` guarda `CustomerId`, não um `Customer` inteiro.
4. **Agregado pequeno.** Um agregado que carrega mil filhos para validar uma regra vira gargalo de performance e de concorrência.

## Entidade rica, não anêmica

```csharp
// Anêmico: qualquer um muda o status para qualquer valor
public class Order
{
    public OrderStatus Status { get; set; }
}

// Rico: a transição tem regra e só acontece pelo método
public class Order
{
    public OrderStatus Status { get; private set; }

    public void Approve()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Apenas pedidos pendentes podem ser aprovados.");

        Status = OrderStatus.Approved;
    }
}
```

## Value Object com record

```csharp
public sealed record Email
{
    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Result<Email> Criar(string valor) =>
        string.IsNullOrWhiteSpace(valor) || !valor.Contains('@')
            ? Result.Fail("E-mail inválido.")
            : Result.Ok(new Email(valor.Trim().ToLowerInvariant()));
}
```

Um `Email` que existe já é válido. Some a validação espalhada por controllers, DTOs e serviços.

## Quando não usar

CRUD de cadastro, painel administrativo, integração que só move dados de um lugar para outro. DDD tático em domínio sem regra é custo sem retorno. O estratégico (linguagem e fronteiras) quase sempre vale.

## O que perguntam em entrevista

- O que é um bounded context, com exemplo.
- Diferença entre entidade e value object.
- O que é um agregado e por que uma transação deve alterar só um.
- Domain event e integration event: qual a diferença.
- Modelo anêmico: o que é e por que é um problema.
