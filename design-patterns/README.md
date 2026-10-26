# Clean code

Clean code, SOLID e boas práticas aparecem em quatro de cada dez vagas. O que a vaga quer dizer com isso é simples: código que outra pessoa lê, entende e altera sem medo. Não é sobre seguir regra cega, é sobre reduzir o custo da próxima mudança.

Nesta seção:

- [SOLID](./solid.md), com exemplos em C# do que cada princípio evita.
- [Design patterns](./padroes.md) que eu realmente uso no backend: Strategy, Decorator, Factory, Specification e Result.

## Nomes

O nome é a documentação que sempre está atualizada.

```csharp
// Ruim
var d = DateTime.Now.Subtract(p.Dt).Days;
if (d > 30 && p.St == 1) { ... }

// Bom
var diasDesdeACriacao = (DateTime.UtcNow - pedido.CriadoEm).Days;
if (diasDesdeACriacao > PrazoDeExpiracaoEmDias && pedido.EstaPendente) { ... }
```

- Classe é substantivo (`PedidoRepository`), método é verbo (`AprovarAsync`).
- Booleano pergunta: `EstaAtivo`, `PodeAprovar`, `TemEstoque`.
- Sem abreviação que só você entende. `cli` pode ser cliente, client ou command line.
- Escolha um idioma para o domínio e mantenha. Misturar `Order` com `Pedido` no mesmo sistema confunde mais que qualquer um dos dois.

## Funções pequenas, um nível de abstração

O método público conta a história; os privados cuidam dos detalhes.

```csharp
public async Task<Result> FecharMesAsync(int ano, int mes, CancellationToken ct)
{
    var faturas = await BuscarFaturasAbertasAsync(ano, mes, ct);
    var total = CalcularTotal(faturas);
    await RegistrarFechamentoAsync(ano, mes, total, ct);
    return Result.Ok();
}
```

Sinais de que a função precisa ser quebrada: comentário separando "blocos", mais de três níveis de indentação, nome com "E" (`ValidarESalvar`), parâmetro booleano que muda o comportamento.

## Parâmetro booleano esconde dois métodos

```csharp
// O leitor não sabe o que "true" significa
var pedido = await repo.GetByIdAsync(id, true);

// Dois nomes explícitos
var pedido = await repo.GetByIdNoTrackingAsync(id);
```

É o mesmo raciocínio do [Repository com métodos separados para tracking](../Persistence%20Patterns/).

## Guard clauses no lugar de if aninhado

```csharp
public Result Aprovar(Pedido pedido, Usuario usuario)
{
    if (pedido is null) return Result.Fail("Pedido não encontrado.");
    if (!usuario.PodeAprovar) return Result.Fail("Usuário sem permissão.");
    if (pedido.Status != OrderStatus.Pending) return Result.Fail("Só pedidos pendentes podem ser aprovados.");

    pedido.Approve();
    return Result.Ok();
}
```

## Comentários

Comente o **porquê**, nunca o **o quê**. Se precisa explicar o que o código faz, o código está mal escrito.

```csharp
// Ruim: repete o código
// incrementa o contador
contador++;

// Bom: explica uma decisão que o código não mostra
// O gateway devolve 200 com erro no corpo quando o cartão é recusado (doc v2, seção 4.3).
if (resposta.Status == "DECLINED") ...
```

## Imutabilidade por padrão

`record` para DTOs e eventos, `init` para propriedades que não mudam, `private set` nas entidades. Objeto que ninguém consegue alterar por fora é objeto que não quebra invariante.

```csharp
public sealed record PedidoCriado(Guid PedidoId, string ClienteId, DateTime OcorreuEm);
```

## O que perguntam em entrevista

- O que é código limpo para você, com um exemplo de refatoração que já fez.
- Como você nomeia métodos e classes.
- Quando um comentário é útil.
- Como você lida com uma classe de 2.000 linhas que ninguém quer mexer.
