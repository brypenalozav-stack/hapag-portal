namespace HapagPortal.Application.Customs.Manifests;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;

public sealed record CreateManifestCommand(
    string VesselImo,
    string Voyage,
    string Port,
    string Direction,
    DateTime? EstimatedArrival,
    DateTime? EstimatedDeparture) : ICommand<Guid>;

public sealed class CreateManifestCommandValidator : AbstractValidator<CreateManifestCommand>
{
    public CreateManifestCommandValidator()
    {
        RuleFor(x => x.VesselImo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Voyage).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Port).NotEmpty().Length(5).WithMessage("El puerto debe ser un UN/LOCODE de 5 caracteres.");
        RuleFor(x => x.Direction)
            .Must(d => d == CustomsDirections.Ingreso || d == CustomsDirections.Salida)
            .WithMessage("La dirección debe ser Ingreso o Salida.");
    }
}

public sealed class CreateManifestCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateManifestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateManifestCommand request, CancellationToken cancellationToken)
    {
        var manifest = new CustomsManifest
        {
            VesselImo = request.VesselImo,
            Voyage = request.Voyage,
            Port = request.Port,
            Direction = request.Direction,
            EstimatedArrival = request.EstimatedArrival,
            EstimatedDeparture = request.EstimatedDeparture
        };

        dbContext.CustomsManifests.Add(manifest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(manifest.Id);
    }
}
