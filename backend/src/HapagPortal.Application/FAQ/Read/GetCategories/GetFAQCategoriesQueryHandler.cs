namespace HapagPortal.Application.FAQ.Read.GetCategories;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed class GetFAQCategoriesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetFAQCategoriesQuery, List<string>>
{
    public async Task<Result<List<string>>> Handle(
        GetFAQCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await dbContext.FAQs
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Select(f => f.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        return Result<List<string>>.Success(categories);
    }
}
