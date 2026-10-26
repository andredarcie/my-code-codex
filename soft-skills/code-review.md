# Code review

Code review aparece em uma de cada seis vagas, e em quase toda empresa é rotina diária. É onde o conhecimento circula, onde o bug é pego mais barato e onde o seu jeito de trabalhar em equipe fica mais visível. Tenho skills de revisão automatizada para agentes de IA em [My Skills](https://andredarcie.github.io/my-skills/); o roteiro abaixo é o mesmo que uso como revisor humano.

## Como revisar

### A ordem em que olho

1. **Entendi o objetivo?** Se a descrição não diz, pergunte antes de ler código.
2. **A solução faz sentido?** Abordagem errada se aponta antes de comentar nome de variável.
3. **Correção:** casos de borda, nulos, concorrência, transação, retry, idempotência.
4. **Segurança:** autorização por objeto, dado sensível em log, entrada sem validação. Ver [Segurança](../seguranca/).
5. **Testes:** cobrem o comportamento novo? Quebrariam se o código estivesse errado?
6. **Legibilidade:** nomes, tamanho das funções, código morto.
7. **Estilo:** o que o formatador e o analisador não pegam. Se pegam, não comente.

### Como escrever o comentário

- **Pergunte em vez de afirmar** quando não tem certeza: "o que acontece aqui se o pedido vier sem itens?"
- **Explique o porquê:** "prefiro `AsNoTracking` aqui porque é só leitura e a lista pode ter milhares de itens" ensina; "use AsNoTracking" só manda.
- **Diga o peso** do comentário: bloqueante, sugestão ou detalhe. Muitos times usam prefixos como `nit:` para o que não bloqueia.
- **Elogie o que está bom.** Não por educação: para o autor saber o que repetir.
- **Sobre o código, nunca sobre a pessoa.** "Este método está difícil de seguir", não "você escreveu confuso".

### O que não fazer

- Aprovar sem ler porque o PR é grande. Peça para dividir.
- Segurar o PR por gosto pessoal. Se não é bug, não é risco e não fere o padrão do time, é sugestão.
- Reescrever a solução do outro nos comentários. Se a abordagem toda está errada, converse por chamada.
- Deixar o PR esperando dias. Revisão rápida é parte do trabalho, não interrupção dele.

## Como receber revisão

- PR pequeno e com boa descrição recebe revisão melhor e mais rápida. Ver [Comunicação escrita](./comunicacao.md).
- Revise o próprio PR antes de pedir revisão: metade dos comentários some.
- Comentário no código não é ataque. Responda a todos, mesmo que seja "feito" ou "prefiro manter porque X".
- Discordou? Explique uma vez com argumento. Persistiu o impasse, decida por chamada ou com uma terceira pessoa; trinta comentários na mesma thread não convencem ninguém.

## Automatize o que é mecânico

Formatação, estilo e regras simples não deveriam consumir atenção humana:

- `dotnet format` no pipeline.
- `.editorconfig` com as regras do time e analisadores (`<AnalysisLevel>latest-recommended</AnalysisLevel>`).
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` para o aviso não virar ruído ignorado.
- Revisão por agente de IA como primeira passada, com um humano decidindo.

## O que perguntam em entrevista

- O que você olha primeiro num code review.
- Como você dá um comentário sobre um problema sério sem desanimar o autor.
- Conte de uma vez em que discordou de um comentário recebido.
- Como você lida com um PR de 3.000 linhas.
