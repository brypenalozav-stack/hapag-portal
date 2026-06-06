namespace HapagPortal.Application.FAQ.Read.Search;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record SearchFAQsQuery(string SearchText) : IQuery<List<FAQDto>>;
