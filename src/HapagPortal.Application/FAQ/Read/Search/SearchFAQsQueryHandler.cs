namespace HapagPortal.Application.FAQ.Read.Search;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class SearchFAQsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchFAQsQuery, List<FAQDto>>
{
    public async Task<Result<List<FAQDto>>> Handle(
        SearchFAQsQuery request,
        CancellationToken cancellationToken)
    {
        var faqs = await dbContext.FAQs
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Where(f => f.Question.Contains(request.SearchText) || f.Answer.Contains(request.SearchText))
            .OrderBy(f => f.SortOrder)
            .Select(f => new FAQDto(
                f.Id,
                f.Question,
                f.Answer,
                f.Category,
                f.Country,
                f.SortOrder))
            .ToListAsync(cancellationToken);

        return Result<List<FAQDto>>.Success(faqs);
    }
}
