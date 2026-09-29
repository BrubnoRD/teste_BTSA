namespace TransferenciasFinanceiras.Api.Model;

public abstract class Entidade
{
    public Guid Id { get; protected set; }

    protected Entidade()
    {
    }

    protected Entidade(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entidade outra || outra.GetType() != GetType())
        {
            return false;
        }

        return Id == outra.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
