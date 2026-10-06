namespace HapagPortal.Application.Payments.Maintainers;

using FluentValidation;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Maintainers;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Payments.Common;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Instantánea de un medio de pago para el registro de cambios (NF-15).</summary>
public sealed record PaymentMethodSnapshot(
    string Code,
    string Name,
    string? Description,
    string Country,
    string Kind,
    string? ProviderKey,
    IReadOnlyList<string> Currencies,
    bool IsEnabled,
    int DisplayOrder)
{
    public static PaymentMethodSnapshot From(PaymentMethodConfig m) => new(
        m.Code, m.Name, m.Description, m.Country, m.Kind, m.ProviderKey, PaymentMethodCatalog.Currencies(m), m.IsEnabled, m.DisplayOrder);
}

/// <summary>Medios de pago del mantenedor interno (M5-03), incluidos los deshabilitados si se pide.</summary>
public sealed record GetPaymentMethodConfigsQuery(string? Country, bool IncludeDisabled) : IQuery<IReadOnlyList<PaymentMethodDto>>;

/// <summary>Medios habilitados para el cliente en un país y, opcionalmente, una moneda.</summary>
public sealed record GetAvailablePaymentMethodsQuery(string Country, string? Currency) : IQuery<IReadOnlyList<PaymentMethodDto>>;

/// <summary>
/// Alta de un medio de pago (M5-03). Un medio en línea apunta a un proveedor registrado
/// (<c>IPaymentProvider</c> con esa clave): incorporar un proveedor = adaptador + esta configuración.
/// </summary>
public sealed record CreatePaymentMethodCommand(
    string Code,
    string Name,
    string? Description,
    string Country,
    string Kind,
    string? ProviderKey,
    IReadOnlyList<string> Currencies,
    bool IsEnabled,
    int DisplayOrder) : ICommand<PaymentMethodDto>;

public sealed record UpdatePaymentMethodCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Country,
    string Kind,
    string? ProviderKey,
    IReadOnlyList<string> Currencies,
    bool IsEnabled,
    int DisplayOrder) : ICommand<PaymentMethodDto>;

/// <summary>Deshabilita un medio: deja de ofrecerse; su historial se conserva (NF-15).</summary>
public sealed record DisablePaymentMethodCommand(Guid Id) : ICommand;

public sealed record GetPaymentMethodHistoryQuery(Guid Id) : IQuery<IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>>>;

internal static class PaymentMethodFields
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, PaymentMethodSnapshot> fields)
    {
        validator.RuleFor(x => fields(x).Code)
            .NotEmpty()
            .Matches("^[A-Z0-9_]{2,40}$")
            .WithName("Code")
            .WithMessage("Code must be 2-40 characters: A-Z, 0-9 or _.");
        validator.RuleFor(x => fields(x).Name).NotEmpty().MaximumLength(100).WithName("Name");
        validator.RuleFor(x => fields(x).Description).MaximumLength(300).WithName("Description");
        validator.RuleFor(x => fields(x).Country)
            .Must(c => CountryCodes.ValidCountries.Contains(c))
            .WithName("Country")
            .WithMessage("Country must be CL or BO.");
        validator.RuleFor(x => fields(x).Kind)
            .Must(k => PaymentMethodKinds.All.Contains(k))
            .WithName("Kind")
            .WithMessage("Kind must be Online or Deposit.");
        validator.RuleFor(x => fields(x))
            .Must(f => f.Kind == PaymentMethodKinds.Online
                ? f.ProviderKey is not null && PaymentProviderKeys.All.Contains(f.ProviderKey)
                : f.ProviderKey is null)
            .WithName("ProviderKey")
            .WithMessage($"Online methods require a provider ({string.Join(", ", PaymentProviderKeys.All)}); deposit methods have none.");
        validator.RuleFor(x => fields(x).Currencies)
            .NotEmpty()
            .WithName("Currencies");
        validator.RuleForEach(x => fields(x).Currencies)
            .Matches("^[A-Z]{3}$")
            .WithName("Currencies")
            .WithMessage("Each currency must be an ISO 4217 code.");
        validator.RuleFor(x => fields(x).DisplayOrder).InclusiveBetween(0, 10000).WithName("DisplayOrder");
    }

    public static PaymentMethodSnapshot Normalize(
        string code, string name, string? description, string country, string kind, string? providerKey,
        IReadOnlyList<string>? currencies, bool isEnabled, int displayOrder) =>
        new(
            (code ?? string.Empty).Trim().ToUpperInvariant(),
            (name ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            (country ?? string.Empty).Trim().ToUpperInvariant(),
            (kind ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(providerKey) ? null : providerKey.Trim(),
            PaymentMethodCatalog.ParseCurrencies(string.Join(',', currencies ?? [])),
            isEnabled,
            displayOrder);

    public static void Write(PaymentMethodConfig method, PaymentMethodSnapshot fields)
    {
        method.Code = fields.Code;
        method.Name = fields.Name;
        method.Description = fields.Description;
        method.Country = fields.Country;
        method.Kind = fields.Kind;
        method.ProviderKey = fields.ProviderKey;
        method.Currencies = PaymentMethodCatalog.FormatCurrencies(fields.Currencies);
        method.IsEnabled = fields.IsEnabled;
        method.DisplayOrder = fields.DisplayOrder;
    }

    /// <summary>El proveedor de un medio en línea debe estar registrado en el contenedor.</summary>
    public static Result CheckProvider(IPaymentProviderResolver resolver, PaymentMethodSnapshot fields) =>
        fields.Kind == PaymentMethodKinds.Online && (fields.ProviderKey is null || resolver.Resolve(fields.ProviderKey) is null)
            ? Result.Failure(DomainErrors.PaymentMethodConfig.ProviderRequired)
            : Result.Success();
}

public sealed class CreatePaymentMethodCommandValidator : AbstractValidator<CreatePaymentMethodCommand>
{
    public CreatePaymentMethodCommandValidator()
    {
        PaymentMethodFields.Apply(this, x => PaymentMethodFields.Normalize(
            x.Code, x.Name, x.Description, x.Country, x.Kind, x.ProviderKey, x.Currencies, x.IsEnabled, x.DisplayOrder));
    }
}

public sealed class UpdatePaymentMethodCommandValidator : AbstractValidator<UpdatePaymentMethodCommand>
{
    public UpdatePaymentMethodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        PaymentMethodFields.Apply(this, x => PaymentMethodFields.Normalize(
            x.Code, x.Name, x.Description, x.Country, x.Kind, x.ProviderKey, x.Currencies, x.IsEnabled, x.DisplayOrder));
    }
}

public sealed class GetPaymentMethodConfigsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentMethodConfigsQuery, IReadOnlyList<PaymentMethodDto>>
{
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(GetPaymentMethodConfigsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentMethodConfigs.AsNoTracking();
        if (!request.IncludeDisabled)
            query = query.Where(m => m.IsEnabled);
        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToUpperInvariant();
            query = query.Where(m => m.Country == country);
        }

        var methods = await query
            .OrderBy(m => m.Country)
            .ThenBy(m => m.DisplayOrder)
            .ThenBy(m => m.Code)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMethodDto> items = methods.Select(PaymentMethodCatalog.ToDto).ToList();
        return Result<IReadOnlyList<PaymentMethodDto>>.Success(items);
    }
}

public sealed class GetAvailablePaymentMethodsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAvailablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(GetAvailablePaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var methods = await PaymentMethodCatalog.AvailableAsync(
            dbContext,
            (request.Country ?? string.Empty).Trim().ToUpperInvariant(),
            request.Currency?.Trim().ToUpperInvariant(),
            cancellationToken);

        IReadOnlyList<PaymentMethodDto> items = methods.Select(PaymentMethodCatalog.ToDto).ToList();
        return Result<IReadOnlyList<PaymentMethodDto>>.Success(items);
    }
}

public sealed class CreatePaymentMethodCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPaymentProviderResolver providerResolver)
    : ICommandHandler<CreatePaymentMethodCommand, PaymentMethodDto>
{
    public async Task<Result<PaymentMethodDto>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var fields = PaymentMethodFields.Normalize(
            request.Code, request.Name, request.Description, request.Country, request.Kind, request.ProviderKey,
            request.Currencies, request.IsEnabled, request.DisplayOrder);

        var provider = PaymentMethodFields.CheckProvider(providerResolver, fields);
        if (provider.IsFailure)
            return Result<PaymentMethodDto>.Failure(provider.Error);

        var exists = await dbContext.PaymentMethodConfigs.AnyAsync(m => m.Country == fields.Country && m.Code == fields.Code, cancellationToken);
        if (exists)
            return Result<PaymentMethodDto>.Failure(DomainErrors.PaymentMethodConfig.AlreadyExists);

        var method = new PaymentMethodConfig { Code = fields.Code, Name = fields.Name, Country = fields.Country, Kind = fields.Kind };
        PaymentMethodFields.Write(method, fields);
        dbContext.PaymentMethodConfigs.Add(method);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentMethod, method.Id, MaintainerActions.Created,
            null, PaymentMethodSnapshot.From(method), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentMethodDto>.Success(PaymentMethodCatalog.ToDto(method));
    }
}

public sealed class UpdatePaymentMethodCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPaymentProviderResolver providerResolver)
    : ICommandHandler<UpdatePaymentMethodCommand, PaymentMethodDto>
{
    public async Task<Result<PaymentMethodDto>> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await dbContext.PaymentMethodConfigs.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (method is null)
            return Result<PaymentMethodDto>.Failure(DomainErrors.PaymentMethodConfig.NotFound(request.Id));

        var fields = PaymentMethodFields.Normalize(
            request.Code, request.Name, request.Description, request.Country, request.Kind, request.ProviderKey,
            request.Currencies, request.IsEnabled, request.DisplayOrder);

        var provider = PaymentMethodFields.CheckProvider(providerResolver, fields);
        if (provider.IsFailure)
            return Result<PaymentMethodDto>.Failure(provider.Error);

        var duplicate = await dbContext.PaymentMethodConfigs.AnyAsync(
            m => m.Id != method.Id && m.Country == fields.Country && m.Code == fields.Code, cancellationToken);
        if (duplicate)
            return Result<PaymentMethodDto>.Failure(DomainErrors.PaymentMethodConfig.AlreadyExists);

        var previous = PaymentMethodSnapshot.From(method);
        PaymentMethodFields.Write(method, fields);

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentMethod, method.Id, MaintainerActions.Updated,
            previous, PaymentMethodSnapshot.From(method), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<PaymentMethodDto>.Success(PaymentMethodCatalog.ToDto(method));
    }
}

public sealed class DisablePaymentMethodCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : ICommandHandler<DisablePaymentMethodCommand>
{
    public async Task<Result> Handle(DisablePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var method = await dbContext.PaymentMethodConfigs.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (method is null)
            return Result.Failure(DomainErrors.PaymentMethodConfig.NotFound(request.Id));

        if (!method.IsEnabled)
            return Result.Success();

        var previous = PaymentMethodSnapshot.From(method);
        method.IsEnabled = false;

        MaintainerChangeLogger.Log(
            dbContext, currentUserService, MaintainerNames.PaymentMethod, method.Id, MaintainerActions.Deactivated,
            previous, PaymentMethodSnapshot.From(method), DateTime.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class GetPaymentMethodHistoryQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentMethodHistoryQuery, IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>>>
{
    public async Task<Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>>>> Handle(
        GetPaymentMethodHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PaymentMethodConfigs.AsNoTracking().AnyAsync(m => m.Id == request.Id, cancellationToken);
        if (!exists)
            return Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>>>.Failure(DomainErrors.PaymentMethodConfig.NotFound(request.Id));

        var changes = await dbContext.MaintainerChangeLogs.AsNoTracking()
            .Where(c => c.Maintainer == MaintainerNames.PaymentMethod && c.EntityId == request.Id)
            .OrderByDescending(c => c.ChangedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>> items = changes
            .Select(c => new PaymentMaintainerChangeDto<PaymentMethodSnapshot>(
                c.Id, c.EntityId, c.Action, c.ChangedAt, c.ChangedBy, c.ChangedByUserId,
                MaintainerChangeLogger.Read<PaymentMethodSnapshot>(c.PreviousValue),
                MaintainerChangeLogger.Read<PaymentMethodSnapshot>(c.NewValue)))
            .ToList();

        return Result<IReadOnlyList<PaymentMaintainerChangeDto<PaymentMethodSnapshot>>>.Success(items);
    }
}
