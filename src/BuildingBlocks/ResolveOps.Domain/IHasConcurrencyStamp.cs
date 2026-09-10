namespace ResolveOps.Domain;

public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}
