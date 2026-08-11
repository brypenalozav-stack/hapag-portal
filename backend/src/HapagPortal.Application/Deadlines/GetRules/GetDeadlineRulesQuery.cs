namespace HapagPortal.Application.Deadlines.GetRules;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Deadlines.Common;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

public sealed record GetDeadlineRulesQuery : IQuery<List<DeadlineRuleDto>>;

public sealed class GetDeadlineRulesQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetDeadlineRulesQuery, List<DeadlineRuleDto>>
{
    public async Task<Result<List<DeadlineRuleDto>>> Handle(
        GetDeadlineRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await dbContext.DeadlineRules.AsNoTracking()
            .OrderBy(r => r.Code)
            .Select(r => new DeadlineRuleDto(
                r.Id, r.Code, r.Name, r.BaseEvent, r.OffsetHours, r.AtRiskWindowHours,
                r.Direction, r.Country, r.BLType, r.Severity, r.Source, r.Certainty, r.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<DeadlineRuleDto>>.Success(rules);
    }
}
