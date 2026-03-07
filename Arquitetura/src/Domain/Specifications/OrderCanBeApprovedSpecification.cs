namespace Domain.Specifications;

// Specification: encapsula uma regra de negócio reutilizável e combinável
public class OrderCanBeApprovedSpecification
{
    public bool IsSatisfiedBy(Order order) =>
        order.Status == OrderStatus.Pending && order.Total > 0;
}

// Exemplo de composição de specifications
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other) =>
        new AndSpecification<T>(this, other);
}

public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T entity) =>
        _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
}
