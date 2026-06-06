namespace HapagPortal.Application.FAQ.Read.GetAllFAQs;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record GetAllFAQsQuery(
    string? Country,
    string? Category) : IQuery<List<FAQDto>>;
