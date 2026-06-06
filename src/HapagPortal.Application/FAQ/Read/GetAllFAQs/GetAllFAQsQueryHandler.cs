namespace HapagPortal.Application.FAQ.Read.GetAllFAQs;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetAllFAQsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllFAQsQuery, List<FAQDto>>
{
    public async Task<Result<List<FAQDto>>> Handle(
        GetAllFAQsQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.FAQs
            .AsNoTracking()
            .Where(f => f.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(f => f.Country == request.Country || f.Category == "GENERAL");

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(f => f.Category == request.Category);

        var faqs = await query
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
