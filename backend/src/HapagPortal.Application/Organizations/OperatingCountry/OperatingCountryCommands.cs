namespace HapagPortal.Application.Organizations.OperatingCountry;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>País de operación vigente del usuario y los disponibles para su organización (M1-04).</summary>
public sealed record GetOperatingCountryQuery : IQuery<OperatingCountryDto>;

/// <summary>Cambia el país de operación; solo entre los países en que opera la organización (M1-04).</summary>
public sealed record SetOperatingCountryCommand(string Country) : ICommand<OperatingCountryDto>;

public sealed class SetOperatingCountryCommandValidator : AbstractValidator<SetOperatingCountryCommand>
{
    public SetOperatingCountryCommandValidator()
    {
        RuleFor(x => x.Country)
            .NotEmpty()
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithMessage("Country must be 'CL' or 'BO'.");
    }
}

public sealed class GetOperatingCountryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetOperatingCountryQuery, OperatingCountryDto>
{
    public async Task<Result<OperatingCountryDto>> Handle(
        GetOperatingCountryQuery request,
        CancellationToken cancellationToken)
    {
        var context = await OperatingCountryResolver.LoadAsync(dbContext, currentUserService, cancellationToken);
        return context.IsFailure
            ? Result<OperatingCountryDto>.Failure(context.Error)
            : Result<OperatingCountryDto>.Success(OperatingCountryResolver.ToDto(context.Value.User, context.Value.Available));
    }
}

public sealed class SetOperatingCountryCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<SetOperatingCountryCommand, OperatingCountryDto>
{
    public async Task<Result<OperatingCountryDto>> Handle(
        SetOperatingCountryCommand request,
        CancellationToken cancellationToken)
    {
        var context = await OperatingCountryResolver.LoadAsync(dbContext, currentUserService, cancellationToken);
        if (context.IsFailure)
            return Result<OperatingCountryDto>.Failure(context.Error);

        var (user, available) = context.Value;

        if (!available.Contains(request.Country))
            return Result<OperatingCountryDto>.Failure(DomainErrors.Organization.CountryNotAvailable);

        if (user.Country != request.Country)
        {
            user.Country = request.Country;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<OperatingCountryDto>.Success(OperatingCountryResolver.ToDto(user, available));
    }
}

public sealed record OperatingCountryContext(User User, IReadOnlyList<string> Available);

public static class OperatingCountryResolver
{
    public static async Task<Result<OperatingCountryContext>> LoadAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
            return Result<OperatingCountryContext>.Failure(Error.Unauthorized);

        var userId = currentUserService.UserId.Value;
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<OperatingCountryContext>.Failure(DomainErrors.User.NotFound(userId));

        var organization = user.ClientId is null
            ? null
            : await dbContext.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == user.ClientId.Value, cancellationToken);

        // Usuarios internos (sin organización o de Hapag-Lloyd) eligen entre todos los países.
        IReadOnlyList<string> available = organization is null || organization.OrganizationType == OrganizationTypes.Internal
            ? CountryCodes.ValidCountries
            : OrganizationMapper.OperatingCountriesOf(organization);

        return Result<OperatingCountryContext>.Success(new OperatingCountryContext(user, available));
    }

    public static OperatingCountryDto ToDto(User user, IReadOnlyList<string> available)
    {
        var current = available.Contains(user.Country) ? user.Country : available[0];
        return new OperatingCountryDto(current, available, available.Count > 1);
    }
}
