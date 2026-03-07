namespace Domain.ValueObjects;

// ValueObject: imutável, sem identidade, comparado por valor
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Valor não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Moeda é obrigatória.");

        Amount = amount;
        Currency = currency.ToUpper();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Não é possível somar moedas diferentes.");

        return new Money(Amount + other.Amount, Currency);
    }

    public override bool Equals(object? obj) =>
        obj is Money other && Amount == other.Amount && Currency == other.Currency;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Currency} {Amount:F2}";
}
