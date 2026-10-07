namespace HapagPortal.Application.Admin.Organizations;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Application.Common.Models;
using HapagPortal.Application.Organizations.Common;
using HapagPortal.Application.Organizations.Users;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>Organizaciones registradas, filtrables por estado, tipo y texto (portal interno, M8-04/M8-06).</summary>
public sealed record SearchOrganizationsQuery(
    string? Status,
    string? OrganizationType,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<OrganizationReviewItemDto>>;

/// <summary>Detalle de una organización con sus usuarios y documentos de respaldo.</summary>
public sealed record GetOrganizationReviewQuery(Guid Id) : IQuery<OrganizationReviewDetailDto>;

public sealed class SearchOrganizationsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<SearchOrganizationsQuery, PagedResult<OrganizationReviewItemDto>>
{
    public async Task<Result<PagedResult<OrganizationReviewItemDto>>> Handle(
        SearchOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = dbContext.Clients
            .AsNoTracking()
            .Where(c => c.OrganizationType != OrganizationTypes.Internal);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.RegistrationStatus == request.Status);

        if (!string.IsNullOrWhiteSpace(request.OrganizationType))
            query = query.Where(c => c.OrganizationType == request.OrganizationType);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.TaxId.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                (c.MatchCode != null && c.MatchCode.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var organizations = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = organizations.Select(c => c.Id).ToList();
        var userCounts = (await dbContext.Users
                .AsNoTracking()
                .Where(u => u.ClientId != null && ids.Contains(u.ClientId.Value))
                .Select(u => u.ClientId!.Value)
                .ToListAsync(cancellationToken))
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = organizations.Select(c => new OrganizationReviewItemDto(
            c.Id,
            c.Name,
            c.TaxId,
            c.TaxIdType,
            c.Country,
            c.OrganizationType,
            c.RegistrationStatus,
            c.MatchCode,
            c.Email,
            c.Phone,
            userCounts.GetValueOrDefault(c.Id),
            c.CreatedAt)).ToList();

        return Result<PagedResult<OrganizationReviewItemDto>>.Success(
            new PagedResult<OrganizationReviewItemDto>(items, total, page, pageSize));
    }
}

public sealed class GetOrganizationReviewQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetOrganizationReviewQuery, OrganizationReviewDetailDto>
{
    public async Task<Result<OrganizationReviewDetailDto>> Handle(
        GetOrganizationReviewQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (organization is null)
            return Result<OrganizationReviewDetailDto>.Failure(DomainErrors.Organization.NotFound(request.Id));

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.ClientId == organization.Id)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        var profiles = await OrganizationProfileAssigner.ProfilesByUserAsync(
            dbContext, users.Select(u => u.Id).ToList(), cancellationToken);

        var documents = await dbContext.OrganizationDocuments
            .AsNoTracking()
            .Where(d => d.ClientId == organization.Id)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<OrganizationReviewDetailDto>.Success(OrganizationReviewMapper.ToDetail(
            organization,
            users.Select(u => OrganizationUserMapper.ToDto(u, profiles.GetValueOrDefault(u.Id))).ToList(),
            documents.Select(OrganizationMapper.ToDto).ToList()));
    }
}

public static class OrganizationReviewMapper
{
    public static OrganizationReviewDetailDto ToDetail(
        Domain.Entities.Client c,
        IReadOnlyList<OrganizationUserDto> users,
        IReadOnlyList<OrganizationDocumentDto> documents) =>
        new(
            c.Id,
            c.Name,
            c.TaxId,
            c.TaxIdType,
            c.Country,
            c.OrganizationType,
            c.RegistrationStatus,
            c.MatchCode,
            OrganizationMapper.OperatingCountriesOf(c),
            c.Email,
            c.Phone,
            c.Address,
            c.City,
            c.CreatedAt,
            c.ValidatedAt,
            c.ValidatedBy,
            c.ArCheckedAt,
            c.ArCheckedBy,
            c.ArReference,
            c.ApprovedAt,
            c.RejectedAt,
            c.ReviewNotes,
            users,
            documents);
}
