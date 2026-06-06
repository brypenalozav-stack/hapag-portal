namespace HapagPortal.Application.FAQ.Commands.Create;

using HapagPortal.Application.Common.Dtos;
using HapagPortal.Application.Common.Messaging;

public sealed record CreateFAQCommand(
    string Question,
    string Answer,
    string Category,
    string Country,
    int SortOrder) : ICommand<FAQDto>;
