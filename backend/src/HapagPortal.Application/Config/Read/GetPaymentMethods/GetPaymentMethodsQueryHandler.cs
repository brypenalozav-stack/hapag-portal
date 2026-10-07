namespace HapagPortal.Application.Config.Read.GetPaymentMethods;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Medios de pago habilitados del país según el mantenedor de M5-03 (antes una lista fija en código).
/// El detalle con monedas y proveedor está en <c>GET /payment-config/methods/available</c>.
/// </summary>
public sealed class GetPaymentMethodsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetPaymentMethodsQuery, List<PaymentMethodResponseDto>>
{
    public async Task<Result<List<PaymentMethodResponseDto>>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var country = (request.Country ?? string.Empty).Trim().ToUpperInvariant();

        var methods = await dbContext.PaymentMethodConfigs.AsNoTracking()
            .Where(m => m.Country == country && m.IsEnabled)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Code)
            .Select(m => new PaymentMethodResponseDto(m.Code, m.Name, m.Description ?? m.Name, m.IsEnabled))
            .ToListAsync(cancellationToken);

        return Result<List<PaymentMethodResponseDto>>.Success(methods);
    }
}
