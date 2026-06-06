namespace HapagPortal.Application.FAQ.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Results;

public sealed class CreateFAQCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateFAQCommand, FAQDto>
{
    public async Task<Result<FAQDto>> Handle(
        CreateFAQCommand request,
        CancellationToken cancellationToken)
    {
        var faq = new Domain.Entities.FAQ
        {
            Question = request.Question,
            Answer = request.Answer,
            Category = request.Category,
            Country = request.Country,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        dbContext.FAQs.Add(faq);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FAQDto>.Success(new FAQDto(
            faq.Id,
            faq.Question,
            faq.Answer,
            faq.Category,
            faq.Country,
            faq.SortOrder));
    }
}
