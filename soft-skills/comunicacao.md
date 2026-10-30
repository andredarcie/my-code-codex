# Comunicação escrita

Em time remoto, a maior parte do trabalho acontece por escrito: o PR, o ticket, a mensagem no chat, o documento de decisão. Escrever bem é a habilidade que mais multiplica o resto.

## Descrição de pull request

```markdown
## O que muda
Aprovação de pedidos passa a exigir a permissão `pedidos:aprovar`.

## Por quê
Hoje qualquer usuário autenticado aprova pedidos (incidente INC-412).

## Como testar
1. `dotnet test` cobre os casos novos em `PedidosEndpointsTests`.
2. Em homologação, um usuário sem a permissão recebe 403 em `POST /pedidos/{id}/aprovacao`.

## Riscos
O app de backoffice precisa enviar o token com a nova claim. Combinado com o time de identidade para liberar antes do deploy.
```

O revisor lê em dois minutos e sabe onde olhar. Daqui a um ano, quem rodar `git blame` entende a decisão.

## ADR (Architecture Decision Record)

Uma página por decisão difícil de desfazer, guardada no repositório (`docs/adr/0007-kafka-para-eventos-de-pedido.md`):

```markdown
# 7. Kafka para os eventos de pedido

Status: aceita (2026-03-10)

## Contexto
Três serviços precisam reagir à criação de pedidos, e o volume de pico é de 2 mil por segundo.

## Decisão
Publicar os eventos de pedido no Kafka, tópico `pedidos.eventos`, chave = id do pedido.

## Alternativas consideradas
- RabbitMQ: atende o volume, mas não permite reprocessar o histórico quando um serviço novo chega.
- Chamada HTTP direta: acopla os serviços e perde eventos quando um deles está fora.

## Consequências
O time passa a operar Kafka (ou contratar gerenciado). Consumidores precisam ser idempotentes.
```

## Mensagem assíncrona

Escreva para quem vai ler sem poder te perguntar nada.

```text
Ruim:
  oi, tem um minuto?

Bom:
  Oi! O endpoint de pedidos está devolvendo 500 em homologação desde as 14h
  quando o cliente não tem endereço (trace no link). Acho que é a migration de ontem.
  Consegue olhar até amanhã? Se for urgente pro seu lado, eu reverto a migration.
```

Contexto, o que você precisa, até quando e o que você já tentou. Uma mensagem completa economiza três idas e voltas e uma reunião.

## Post-mortem sem culpados

Depois de um incidente: linha do tempo, impacto (quantos usuários, por quanto tempo), causa raiz, o que funcionou, o que não funcionou e ações com dono e prazo. O foco é o sistema que permitiu o erro, não a pessoa que apertou o botão. Time que pune o erro aprende a esconder erro.

## Documentação que alguém lê

- O README responde: o que é, como rodar local em cinco minutos, como testar, como publicar.
- Documente o porquê e o que não é óbvio; o "como" o código já mostra.
- Diagrama simples (caixas e setas) vale mais que dez parágrafos. Mermaid no próprio Markdown fica versionado junto do código.

## O que perguntam em entrevista

- Como você documenta uma decisão técnica.
- Como você explicaria um problema técnico para alguém de negócio.
- Como você comunica um atraso.
- Conte de um incidente e como foi a comunicação durante ele.
