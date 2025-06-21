# Repository

> O padrão Repository serve para isolar o domínio da lógica de acesso a dados, agindo como uma coleção de objetos em memória. Ele permite consultar, adicionar e remover objetos sem expor os detalhes do banco de dados. É útil principalmente quando há muitas classes de domínio ou consultas complexas, pois centraliza a lógica de consultas e evita repetição. Ajuda a manter a separação clara entre o domínio e a infraestrutura de dados.

Fonte: [Repository by Martin Fowler](https://martinfowler.com/eaaCatalog/repository.html)

Usar repositórios no .NET serve pra separar a regra de negócio de coisas como banco de dados, mensageria e outros mecanismos de infraestrutura. Assim, o domínio não sabe nem que o EF ou o RabbitMQ existem, o código fica mais limpo, fácil de testar, manter e trocar a tecnologia se precisar.

Usar DbContext ou DbSet direto no domínio acopla sua lógica de negócio à infraestrutura, quebra os princípios do DDD e dificulta testes, manutenção e evolução do sistema.

Fiz testes e minha conclusão pessoal foi:
> Tentei evitar repositório criando interfaces pra `DbContext` e até `DbSet`, mas no fim o domínio continuava contaminado pelo EF Core, e ficou mais complicado do que simplesmente seguir o padrão certo desde o início.

Interface/Contrato na camada de Domain  
*Domain/Interfaces/IRepository.cs*
```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> ListAsync();
    Task<T?> GetByIdNoTrackingAsync(Guid id);
    Task<IEnumerable<T>> ListNoTrackingAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

Implementação concreta na camada de infra:    
*Infrastructure/Persistence/Repositories/Repository.cs*
```csharp
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _dbSet = context.Set<T>();
    }

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public async Task<T?> GetByIdNoTrackingAsync(Guid id) =>
        await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<T>> ListAsync() => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> ListNoTrackingAsync() =>
        await _dbSet.AsNoTracking().ToListAsync();

    public void Update(T entity) => _dbSet.Update(entity);
}
```

## Tracking

Em projetos que seguem boas práticas de Clean Code e arquitetura limpa, é recomendado manter métodos separados para consultas com e sem tracking no repositório, usando nomes explícitos como `GetByIdAsync` e `GetByIdNoTrackingAsync`. Essa abordagem evita ambiguidade no código, facilita a leitura e reduz o risco de erros sutis causados por parâmetros booleanos como `track = false`, que alteram silenciosamente o comportamento de métodos. Além disso, separando os métodos, você favorece a clareza semântica e segue o princípio de que cada função deve ter uma responsabilidade única, conforme defendido por especialistas como Uncle Bob e adotado em projetos de referência como o eShopOnWeb da Microsoft.