namespace HapagPortal.Application.Common.Dtos;

public sealed record ClientResponseDto(
    Guid Id,
    string Name,
    string Email,
    string TaxId,
    string? Phone,
    string Country,
    string Type,
    string Role,
    bool IsActive,
    DateTime CreatedAt)
{
    /// <summary>
    /// Convenience factory for handlers that only have Client entity data (no user/role context).
    /// </summary>
    public static ClientResponseDto FromClient(
        Domain.Entities.Client client,
        string role = "USER") =>
        new(
            client.Id,
            client.Name,
            client.Email,
            client.TaxId,
            client.Phone,
            client.Country,
            client.ClientType == "Agent" ? "AGENT" : "CLIENT",
            role,
            client.IsActive,
            client.CreatedAt);
}
