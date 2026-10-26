# Entrevistas técnicas

O processo para vaga .NET no Brasil costuma ter quatro etapas. Saber o que cada uma avalia muda como você se prepara.

## 1. Triagem com o RH

Avalia: pretensão salarial, disponibilidade, modelo de trabalho, comunicação.

- Tenha a faixa salarial pesquisada e diga um intervalo.
- Explique em dois minutos o que você faz hoje e o que procura. Treine em voz alta.
- Pergunte sobre o processo: quantas etapas, prazo, quem participa.

## 2. Entrevista técnica conversada

Avalia: profundidade nos fundamentos que a vaga pede. As perguntas mais comuns estão no fim de cada página do guia, agrupadas por tema:

- [APIs REST](../backend/apis-rest.md) e [ASP.NET Core](../backend/aspnet-core.md)
- [EF Core](../Persistence%20Patterns/ef-core) e [SQL](../Persistence%20Patterns/sql)
- [SOLID](../design-patterns/solid.md) e [design patterns](../design-patterns/padroes.md)
- [Testes](../testes/), [mensageria](../mensageria/), [segurança](../seguranca/)

Responda com experiência, não com definição. "Singleton vive a aplicação inteira" é decoreba; "uma vez um singleton capturou o DbContext e tivemos erro de concorrência no pico, e foi assim que resolvemos" mostra que você já operou sistema de verdade.

Não sabe? Diga que não sabe e como descobriria. Inventar é pior que admitir.

## 3. Exercício prático

Os formatos mais comuns:

- **Live coding:** um problema pequeno, compartilhando a tela. Pense em voz alta, pergunte sobre os casos de borda antes de codar, comece pela solução simples e melhore depois. Avaliam como você pensa, não só se termina.
- **Desafio para casa:** uma API pequena em alguns dias. Entregue menos funcionalidade com mais qualidade: testes, README com as decisões e como rodar, Docker Compose. Quem avalia abre o README primeiro.
- **Algoritmos:** só 5% dos anúncios citam, mas empresas grandes e vagas internacionais ainda usam. Os temas que mais caem: arrays e strings, hash maps, dois ponteiros, pilha e fila, árvores e grafos com BFS e DFS, e saber dizer a complexidade (Big O) da sua solução.

```csharp
// Clássico: dois números que somam o alvo. O(n) com dicionário em vez de O(n²) com dois laços.
public static (int, int)? DoisQueSomam(int[] numeros, int alvo)
{
    var vistos = new Dictionary<int, int>();
    for (var i = 0; i < numeros.Length; i++)
    {
        if (vistos.TryGetValue(alvo - numeros[i], out var j))
            return (j, i);
        vistos[numeros[i]] = i;
    }
    return null;
}
```

## 4. System design e comportamental (pleno e sênior)

- **System design:** roteiro, contas de guardanapo e problemas clássicos em [System design](../Arquitetura/system-design.md).
- **Comportamental:** responda com o método STAR, com casos reais. Perguntas típicas em [Soft skills](../soft-skills/).

Prepare de cinco a seis histórias suas que cubram: um conflito, um erro em produção, uma decisão técnica difícil, uma entrega sob pressão, algo que você melhorou por iniciativa própria e alguém que você ajudou a crescer. Quase toda pergunta comportamental cabe numa delas.

## Perguntas para fazer ao entrevistador

A entrevista é dos dois lados. Perguntas que dizem muito sobre o time:

- Como é um deploy hoje, do merge até produção?
- Quem fica de plantão e com que frequência?
- Como as prioridades chegam ao time, e quem pode dizer não?
- Qual foi o último incidente grave e o que mudou depois dele?
- Como é a revisão de código?

## Depois da entrevista

Anote o que perguntaram e o que você não soube. Em três processos você tem o mapa do que estudar, e ele quase sempre bate com o ranking da [página das vagas](./).
