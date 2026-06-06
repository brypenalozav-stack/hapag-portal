namespace HapagPortal.Application.FAQ.Read.GetCategories;

using HapagPortal.Application.Common.Messaging;

public sealed record GetFAQCategoriesQuery() : IQuery<List<string>>;
