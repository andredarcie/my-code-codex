# DomainException

Exceção base para violações de regras de negócio. Capturada no middleware da Presentation e retornada como `422 Unprocessable Entity`.

```csharp
namespace Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```
