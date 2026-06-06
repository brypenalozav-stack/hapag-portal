namespace HapagPortal.Application.Common.Dtos;

public sealed record FAQDto(
    Guid Id,
    string Question,
    string Answer,
    string Category,
    string Country,
    int SortOrder);
