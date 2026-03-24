namespace RLApp.Domain.Common;

/// <summary>
/// Base class for domain specifications (business rules).
/// Used for validation of invariants and domain constraints.
/// </summary>
public abstract class Specification
{
    public string Message { get; protected set; }

    protected Specification(string message)
    {
        Message = message;
    }

    public abstract bool IsSatisfiedBy(object candidate);

    public Specification And(Specification other) => new AndSpecification(this, other);
    public Specification Or(Specification other) => new OrSpecification(this, other);
    public Specification Not() => new NotSpecification(this);

    private class AndSpecification : Specification
    {
        private readonly Specification _spec1;
        private readonly Specification _spec2;

        public AndSpecification(Specification spec1, Specification spec2)
            : base($"{spec1.Message} AND {spec2.Message}")
        {
            _spec1 = spec1;
            _spec2 = spec2;
        }

        public override bool IsSatisfiedBy(object candidate)
            => _spec1.IsSatisfiedBy(candidate) && _spec2.IsSatisfiedBy(candidate);
    }

    private class OrSpecification : Specification
    {
        private readonly Specification _spec1;
        private readonly Specification _spec2;

        public OrSpecification(Specification spec1, Specification spec2)
            : base($"({spec1.Message} OR {spec2.Message})")
        {
            _spec1 = spec1;
            _spec2 = spec2;
        }

        public override bool IsSatisfiedBy(object candidate)
            => _spec1.IsSatisfiedBy(candidate) || _spec2.IsSatisfiedBy(candidate);
    }

    private class NotSpecification : Specification
    {
        private readonly Specification _spec;

        public NotSpecification(Specification spec)
            : base($"NOT ({spec.Message})")
        {
            _spec = spec;
        }

        public override bool IsSatisfiedBy(object candidate)
            => !_spec.IsSatisfiedBy(candidate);
    }
}
