namespace HapagPortal.Domain.Common;

public abstract class GuidEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
