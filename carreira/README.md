# O que as vagas pedem

Este guia é ordenado por dados, não por intuição. O [Radar de vagas dev](https://andredarcie.github.io/job/) leu 1.209 anúncios de desenvolvedor em 9 boards, reunidos em julho de 2026, procurando 50 requisitos que vão além da linguagem e do framework. C# e .NET ficam fora da conta: são o ponto de partida, não o diferencial.

## O ranking

| Requisito | Vagas que pedem | Onde está no guia |
| --- | --- | --- |
| APIs e integrações | 55,4% | [Backend](../backend/), [APIs externas](../Infra/Integração%20com%20APIs/) |
| Banco de dados e SQL | 53,3% | [EF Core](../Persistence%20Patterns/ef-core), [SQL](../Persistence%20Patterns/sql) |
| Trabalho em equipe | 49,3% | [Soft skills](../soft-skills/) |
| Clean code, SOLID e boas práticas | 41,4% | [Clean code](../design-patterns/), [SOLID](../design-patterns/solid.md) |
| Cloud (AWS, Azure, GCP) | 41,4% | [Cloud](../cloud/) |
| Arquitetura e system design | 39,8% | [Arquitetura](../Arquitetura/), [System design](../Arquitetura/system-design.md) |
| Experiência prévia comprovada | 38,7% | portfólio e histórico, abaixo |
| Git | 37,2% | [Git](../dev-ops/git.md) |
| Comunicação | 36,5% | [Comunicação escrita](../soft-skills/comunicacao.md) |
| Performance e escala | 34,8% | [Performance](../performance/) |
| Observabilidade | 32,8% | [Observabilidade](../observabilidade/) |
| CI/CD | 31,9% | [CI/CD](../dev-ops/ci-cd.md) |
| Dados e IA no dia a dia | 29,4% | ferramentas de IA, abaixo |
| Containers e Kubernetes | 28,5% | [Docker e Kubernetes](../dev-ops/docker.md) |
| Metodologias ágeis | 28,4% | Scrum e Kanban no dia a dia do time |
| Testes automatizados e TDD | 24,7% | [Testes](../testes/) |
| Mensageria e eventos | 18,2% | [Mensageria](../mensageria/) |
| Inglês | 18,0% | abaixo |
| Code review | 17,5% | [Code review](../soft-skills/code-review.md) |
| Microsserviços | 16,6% | [Microsserviços](../Arquitetura/microsservicos.md) |
| Segurança e conformidade | 7,9% | [Segurança](../seguranca/) |
| Algoritmos e estruturas de dados | 5,2% | [Entrevistas técnicas](./entrevistas.md) |

Uma vaga pede vários requisitos ao mesmo tempo, por isso a soma passa de 100%.

## O que os números dizem

- **O básico bem feito vence o exótico.** APIs, banco de dados e trabalho em equipe aparecem em metade das vagas. Microsserviços, em uma de cada seis.
- **Comportamento pesa tanto quanto técnica.** Trabalho em equipe e comunicação estão entre os dez primeiros.
- **Testes aparecem mais nas vagas em português** (41,5%) do que nas em inglês (12,7%). Nas vagas em inglês, quem aparece mais é arquitetura (44,7% contra 33%).
- **Algoritmos quase não aparecem no anúncio** (5,2%), mas continuam pesando em processos de empresas grandes. O anúncio diz o que o time faz; a entrevista às vezes testa outra coisa.

## Júnior, pleno e sênior

A diferença não é tempo de casa, é o escopo do problema que você resolve sozinho. Escrevi a versão longa em [A diferença entre desenvolvedor júnior, pleno e sênior](https://dev.to/andredarcie/a-diferenca-entre-desenvolvedor-junior-pleno-e-senior-na-minha-opiniao-pt-br-659).

| | Resolve sozinho | Precisa de ajuda para |
| --- | --- | --- |
| Júnior | tarefas bem definidas | quebrar o problema e decidir o caminho |
| Pleno | funcionalidades inteiras, do banco à API | decisões que afetam outros times |
| Sênior | problemas ambíguos, de ponta a ponta, incluindo produção | quase nada técnico; o desafio vira alinhar pessoas |

## Inglês

Aparece em 18% das vagas, mas abre as vagas que pagam em dólar ou euro. Ler documentação é o mínimo; para vaga internacional, conversar numa daily e defender uma decisão técnica numa entrevista de uma hora. A forma mais eficiente que conheço: fazer em inglês o que você já faz (README, mensagens de commit, descrições de PR) e praticar conversação técnica toda semana.

## IA no dia a dia

Três de cada dez vagas já citam dados ou IA. Para o dev backend isso significa saber trabalhar com agentes de código sem terceirizar o julgamento: escrever os testes que definem o comportamento, revisar o que o agente gerou como revisaria o PR de um colega e entender o suficiente para saber quando ele está errado. Escrevi sobre isso em [O mínimo que você precisa saber para desenvolver software com IA](https://dev.to/andredarcie/o-minimo-que-voce-precisa-saber-para-desenvolver-software-com-ia-1dc9).

## Portfólio

"Experiência prévia comprovada" aparece em quase 40% das vagas. Sem experiência formal, a prova é o que você publicou:

- Um projeto pequeno, completo e publicado vale mais que dez tutoriais pela metade: API com testes, Docker, pipeline e README que explica as decisões.
- GitHub com histórico de commits legível.
- Textos técnicos: explicar algo por escrito mostra domínio e comunicação ao mesmo tempo.

## O que perguntam em entrevista

As perguntas técnicas de cada tema estão no fim de cada página do guia. O processo inteiro, com o que esperar de cada etapa, está em [Entrevistas técnicas](./entrevistas.md).
