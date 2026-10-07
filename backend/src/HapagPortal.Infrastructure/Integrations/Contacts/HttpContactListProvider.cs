using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

namespace HapagPortal.Infrastructure.Integrations.Contacts;

/// <summary>
/// Cliente Real del registro de contactos (CT-CONTACTS, P0060, propuesta Ola I):
/// <c>GET contacts/{matchCode}/distribution-lists?country=</c> y <c>PUT contacts/{matchCode}/distribution-lists/{reportType}</c>
/// con <c>Idempotency-Key</c>. Una cuenta sin listas (404) devuelve una lista vacía.
/// </summary>
public sealed class HttpContactListProvider(HttpClient httpClient, ISecretResolver secretResolver) : IContactListProvider
{
    private static readonly IntegrationEndpoint Endpoint = new(
        IntegrationSystems.Contacts, SecretTypes.ContactsApiKey, "X-Api-Key", IntegrationHttp.CamelCaseJson);

    public async Task<Result<IReadOnlyList<ContactDistributionList>>> GetListsAsync(
        string matchCode,
        string country,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<DistributionListsPayload>(
            httpClient, secretResolver, Endpoint, "getDistributionLists", HttpMethod.Get,
            IntegrationHttp.WithQuery($"contacts/{IntegrationHttp.Segment(matchCode)}/distribution-lists", ("country", country)),
            cancellationToken);

        if (result.IsFailure)
            return Result<IReadOnlyList<ContactDistributionList>>.Failure(result.Error);

        IReadOnlyList<ContactDistributionList> lists = result.Value?.Lists.Select(l => l.ToList()).ToList() ?? [];
        return Result<IReadOnlyList<ContactDistributionList>>.Success(lists);
    }

    public async Task<Result<ContactListUpdateResult>> UpdateListAsync(
        string matchCode,
        string country,
        string reportType,
        IReadOnlyList<string> emails,
        string updatedBy,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var result = await IntegrationHttp.SendAsync<UpdatePayload>(
            httpClient, secretResolver, Endpoint, "putDistributionList", HttpMethod.Put,
            IntegrationHttp.WithQuery(
                $"contacts/{IntegrationHttp.Segment(matchCode)}/distribution-lists/{IntegrationHttp.Segment(reportType)}", ("country", country)),
            cancellationToken,
            new UpdateBody(emails, updatedBy),
            idempotencyKey);

        if (result.IsFailure)
            return Result<ContactListUpdateResult>.Failure(result.Error);

        return result.Value is null
            ? Result<ContactListUpdateResult>.Failure(DomainErrors.Integration.InvalidResponse(IntegrationSystems.Contacts))
            : Result<ContactListUpdateResult>.Success(new ContactListUpdateResult(result.Value.List.ToList(), result.Value.ChangeReference));
    }

    private sealed record UpdateBody(IReadOnlyList<string> Emails, string UpdatedBy);

    private sealed record DistributionListsPayload(string MatchCode, IReadOnlyList<ListPayload> Lists);

    private sealed record UpdatePayload(ListPayload List, string ChangeReference);

    private sealed record ListPayload(string ReportType, IReadOnlyList<string> Emails, DateTime? UpdatedAt = null, string? UpdatedBy = null)
    {
        public ContactDistributionList ToList() => new(ReportType, Emails, UpdatedAt, UpdatedBy);
    }
}
