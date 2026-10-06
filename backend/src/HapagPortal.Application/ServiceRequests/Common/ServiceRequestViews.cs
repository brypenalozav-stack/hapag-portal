namespace HapagPortal.Application.ServiceRequests.Common;

using System.Text.Json;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Results;
using HapagPortal.Domain.ServiceRequests;
using Microsoft.EntityFrameworkCore;

/// <summary>Vistas de una solicitud para el cliente y para la bandeja interna.</summary>
public static class ServiceRequestViews
{
    public static async Task<ServiceRequestDetailDto> DetailAsync(
        IApplicationDbContext dbContext,
        ServiceRequest request,
        bool clientView,
        CancellationToken cancellationToken)
    {
        var definition = await dbContext.ServiceDefinitions.AsNoTracking()
            .FirstAsync(d => d.Id == request.DefinitionId, cancellationToken);
        var organizationName = await dbContext.Clients.AsNoTracking()
            .Where(c => c.Id == request.OrganizationId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var events = await dbContext.ServiceRequestEvents.AsNoTracking()
            .Where(e => e.ServiceRequestId == request.Id)
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.Sequence)
            .ToListAsync(cancellationToken);
        var attachments = await dbContext.ServiceRequestAttachments.AsNoTracking()
            .Where(a => a.ServiceRequestId == request.Id)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);
        var links = await dbContext.ServiceRequestCharges.AsNoTracking()
            .Where(l => l.ServiceRequestId == request.Id)
            .ToListAsync(cancellationToken);
        var chargeIds = links.Select(l => l.LocalChargeId).ToList();
        var charges = await dbContext.LocalCharges.AsNoTracking()
            .Where(c => chargeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        Payment? payment = request.PaymentId is { } paymentId
            ? await dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            : null;

        using var values = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.InputValuesJson) ? "{}" : request.InputValuesJson);

        return new ServiceRequestDetailDto(
            request.Id,
            request.RequestNumber,
            definition.Id,
            definition.Code,
            definition.NameEs,
            definition.NameEn,
            request.OrganizationId,
            organizationName,
            request.RequestedByEmail,
            request.OnBehalfOfClientId,
            request.BillOfLadingId,
            request.BlNumber,
            request.BookingNumber,
            request.Country,
            request.Operation,
            BusinessCalendar.TimeZoneId(request.Country),
            Containers(request),
            ServiceInputSchema.Parse(definition.InputSchemaJson) ?? [],
            values.RootElement.Clone(),
            new ServiceBillingDataDto(request.BillingTaxId, request.BillingName, request.BillingAddress, request.BillingEmail, request.BillingActivity),
            definition.BillingDataRequired,
            definition.TariffAcceptanceRequired,
            ServiceRequestWorkflow.ReadQuote(request),
            request.TariffAcceptedAt,
            request.Status,
            request.StatusChangedAt,
            request.AssignedTeam,
            definition.ApprovalTeam,
            definition.FulfillmentTeam,
            definition.RequiresOutputDocument,
            request.ResolutionNotes,
            request.CreatedAt,
            request.SubmittedAt,
            request.ApprovedAt,
            request.RejectedAt,
            request.PaidAt,
            request.CompletedAt,
            request.CancelledAt,
            charges.Select(c => new ServiceRequestChargeDto(
                c.Id, PayableItemTypes.LocalCharge, c.ChargeType, c.Amount, c.TaxAmount, c.TotalAmount, c.Currency, c.Status,
                links.First(l => l.LocalChargeId == c.Id).Generated)).ToList(),
            payment?.Id,
            payment?.PaymentNumber,
            payment?.ReceiptNumber,
            CanEdit: clientView && request.Status == ServiceRequestStatus.Draft,
            CanSubmit: clientView && request.Status == ServiceRequestStatus.Draft,
            CanCancel: clientView && ServiceRequestStatus.Cancellable.Contains(request.Status),
            attachments.Select(ToDto).ToList(),
            events.Select(e => new ServiceRequestEventDto(e.Id, e.FromStatus, e.ToStatus, e.OccurredAt, e.ActorName, e.ActorKind, e.Notes)).ToList());
    }

    public static async Task<IReadOnlyList<ServiceRequestSummaryDto>> SummariesAsync(
        IApplicationDbContext dbContext,
        IReadOnlyList<ServiceRequest> requests,
        CancellationToken cancellationToken)
    {
        var definitionIds = requests.Select(r => r.DefinitionId).Distinct().ToList();
        var definitions = await dbContext.ServiceDefinitions.AsNoTracking()
            .Where(d => definitionIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);
        var organizationIds = requests.Select(r => r.OrganizationId).Distinct().ToList();
        var organizations = await dbContext.Clients.AsNoTracking()
            .Where(c => organizationIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return requests.Select(r =>
        {
            var definition = definitions.GetValueOrDefault(r.DefinitionId);
            return new ServiceRequestSummaryDto(
                r.Id, r.RequestNumber, r.DefinitionCode, definition?.NameEs ?? r.DefinitionCode, definition?.NameEn ?? r.DefinitionCode,
                r.BlNumber, r.BookingNumber, r.Country, r.Operation, r.Status, r.AssignedTeam, r.TotalAmount, r.Currency, r.IsExempt,
                organizations.GetValueOrDefault(r.OrganizationId), r.RequestedByEmail, r.CreatedAt, r.StatusChangedAt);
        }).ToList();
    }

    public static ServiceRequestAttachmentDto ToDto(ServiceRequestAttachment a) =>
        new(a.Id, a.FieldKey, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAt, a.UploadedBy);

    public static IReadOnlyList<string> Containers(ServiceRequest request) =>
        (request.ContainerNumbers ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Convierte un fallo (incluidos los de validación por campo) al tipo de resultado del handler.</summary>
    public static Result<T> Fail<T>(Result result) =>
        result is IValidationResult validation
            ? ValidationResult<T>.WithErrors(validation.Errors)
            : Result<T>.Failure(result.Error);
}
