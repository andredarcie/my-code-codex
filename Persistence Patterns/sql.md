# SQL

ORM não dispensa SQL. Quem sabe ler um plano de execução resolve em dez minutos o incidente que o resto do time investiga por dois dias. Os exemplos usam SQL Server, o banco mais comum nas vagas .NET no Brasil; o raciocínio vale para PostgreSQL.

## Índices

Índice é o que transforma "ler a tabela inteira" em "ir direto à linha". Sem ele, a consulta piora junto com o crescimento da tabela.

```sql
-- A consulta mais frequente da tela de pedidos
SELECT Id, Total, CreatedAt
FROM Orders
WHERE CustomerId = @CustomerId AND Status = 'Pending'
ORDER BY CreatedAt DESC;

-- O índice que atende: igualdade primeiro, ordenação depois, colunas lidas no INCLUDE
CREATE INDEX IX_Orders_Customer_Status_CreatedAt
    ON Orders (CustomerId, Status, CreatedAt DESC)
    INCLUDE (Total);
```

Regras que mais uso:

- A ordem das colunas importa: o índice `(CustomerId, Status)` não ajuda um filtro só por `Status`.
- Função na coluna mata o índice: `WHERE YEAR(CreatedAt) = 2026` lê tudo; `WHERE CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01'` usa o índice.
- Índice tem custo de escrita. Tabela com vinte índices insere devagar.
- `LIKE '%texto'` (curinga no começo) não usa índice comum.

## Plano de execução

No SQL Server Management Studio ou no Azure Data Studio, "Include Actual Execution Plan". Procure:

- **Table Scan / Clustered Index Scan** em tabela grande: falta índice ou o filtro não é usável.
- **Key Lookup** repetido milhares de vezes: o índice acha a linha mas precisa voltar à tabela; inclua as colunas no `INCLUDE`.
- **Estimativa muito diferente do real:** estatísticas desatualizadas.

## Transações e isolamento

| Nível | Evita | Custo |
| --- | --- | --- |
| Read Uncommitted | nada (lê dado não confirmado) | quase nenhum bloqueio |
| Read Committed (padrão) | leitura suja | bloqueios curtos |
| Repeatable Read | leitura que muda no meio da transação | mais bloqueio |
| Serializable | linhas fantasmas | bloqueio de faixa, risco de deadlock |
| Snapshot | leitor e escritor não se bloqueiam | usa o tempdb para versões |

No SQL Server, ligar `READ_COMMITTED_SNAPSHOT` no banco costuma acabar com boa parte dos bloqueios entre leitura e escrita. Evite `NOLOCK` como solução de performance: ele lê dado não confirmado e até linhas duplicadas.

Transação curta. Nunca chame API externa com transação aberta: o banco fica segurando bloqueio enquanto a rede decide.

## Deadlock

Duas transações, cada uma esperando um recurso que a outra segura. O banco mata uma delas. Para reduzir:

- Acessar as tabelas sempre na mesma ordem.
- Transações curtas e com índices certos (menos linhas bloqueadas).
- Tratar o erro 1205 com retry: o EF Core faz isso com `EnableRetryOnFailure()`.

## Parâmetros, sempre

```csharp
// SQL injection: nunca concatene entrada do usuário
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";

// Parametrizado com Dapper
await conexao.QueryAsync<User>("SELECT * FROM Users WHERE Email = @Email", new { Email = email });

// Parametrizado com EF Core: a interpolação vira parâmetro
await db.Users.FromSql($"SELECT * FROM Users WHERE Email = {email}").ToListAsync(ct);
```

O `FromSql` e o `ExecuteSql` do EF Core transformam a string interpolada em parâmetros. `FromSqlRaw` com concatenação não.

## Consultas que valem saber de cor

```sql
-- Top 3 pedidos por cliente (janela)
SELECT * FROM (
    SELECT o.*, ROW_NUMBER() OVER (PARTITION BY CustomerId ORDER BY Total DESC) AS Posicao
    FROM Orders o
) x
WHERE Posicao <= 3;

-- Clientes sem pedido
SELECT c.Id
FROM Customers c
LEFT JOIN Orders o ON o.CustomerId = c.Id
WHERE o.Id IS NULL;

-- Duplicados
SELECT Email, COUNT(*) FROM Users GROUP BY Email HAVING COUNT(*) > 1;
```

## SQL ou NoSQL

Relacional por padrão: transação, integridade e consulta flexível. NoSQL quando o acesso é sempre pela mesma chave e o volume é enorme (DynamoDB, Cosmos DB), quando o documento varia muito de forma (MongoDB) ou para busca textual (Elasticsearch). Escolher NoSQL "porque escala" sem saber o padrão de acesso é como os projetos acabam com joins feitos em C#.

## O que perguntam em entrevista

- Como você descobre por que uma consulta está lenta.
- O que é um índice composto e por que a ordem das colunas importa.
- Diferença entre `INNER JOIN` e `LEFT JOIN`, e `WHERE` e `HAVING`.
- Níveis de isolamento e o que é um deadlock.
- Como evitar SQL injection.
