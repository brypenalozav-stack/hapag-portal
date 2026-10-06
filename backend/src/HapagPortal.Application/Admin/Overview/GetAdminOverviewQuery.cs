namespace HapagPortal.Application.Admin.Overview;

using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Charges;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Results;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Sección del área de administración (M8-05) disponible para el usuario, con sus pendientes. <c>Code</c> identifica la
/// sección en la interfaz; <c>Counters</c> trae solo los conteos que el permiso del usuario le permite ver.
/// </summary>
public sealed record AdminSectionDto(string Code, string Permission, IReadOnlyDictionary<string, int> Counters);

public sealed record AdminOverviewDto(DateTime GeneratedAt, IReadOnlyList<AdminSectionDto> Sections);

/// <summary>
/// Resumen del área de administración unificada (M8-05): reúne, según los permisos del usuario interno, la revisión de
/// organizaciones y de vínculos con empresa matriz (M8-04, M1-21), la matriz de permisos (M1-11), tarifas y
/// mantenedores (M8-01, M5-03, M5-04), ventanas de bloqueo de pagos (M8-07), Finanzas (M5-06, NF-03), solicitudes de
/// servicios (M2-03), comunicados (M1-26), guías (M1-27), Counter (M8-09), reportería (M9-01) y la «Vista como
/// cliente» (M8-08).
/// </summary>
public sealed record GetAdminOverviewQuery : IQuery<AdminOverviewDto>;

public static class AdminSections
{
    public const string Organizations = "organizations";
    public const string ParentLinks = "parent-links";
    public const string AccessMatrix = "access-matrix";
    public const string Maintainers = "maintainers";
    public const string PaymentBlocks = "payment-blocks";
    public const string Finance = "finance";
    public const string ServiceRequests = "service-requests";
    public const string Announcements = "announcements";
    public const string Guides = "guides";
    public const string Counter = "counter";
    public const string Reports = "reports";
    public const string Impersonation = "impersonation";
    public const string Audit = "audit";
    public const string Users = "users";
}

public sealed class GetAdminOverviewQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetAdminOverviewQuery, AdminOverviewDto>
{
    public async Task<Result<AdminOverviewDto>> Handle(GetAdminOverviewQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var sections = new List<AdminSectionDto>();
        bool Has(string permission) => currentUserService.HasPermission(permission);

        if (Has(AccessPermissions.ReviewOrganizations) || Has(AccessPermissions.CheckOrganizationsAr))
        {
            sections.Add(new AdminSectionDto(AdminSections.Organizations, AccessPermissions.ReviewOrganizations, new Dictionary<string, int>
            {
                ["pendingValidation"] = await dbContext.Clients.CountAsync(c => c.RegistrationStatus == OrganizationStatus.PendingValidation, cancellationToken),
                ["pendingArCheck"] = await dbContext.Clients.CountAsync(c => c.RegistrationStatus == OrganizationStatus.PendingArCheck, cancellationToken),
                ["preCreatedCarriers"] = await dbContext.Clients.CountAsync(c => c.RegistrationStatus == OrganizationStatus.PreCreated, cancellationToken)
            }));
        }

        if (Has(AccessPermissions.ReviewOrganizations))
        {
            sections.Add(new AdminSectionDto(AdminSections.ParentLinks, AccessPermissions.ReviewOrganizations, new Dictionary<string, int>
            {
                ["pending"] = await dbContext.OrganizationParentLinks.CountAsync(l => l.Status == ParentLinkStatus.Pending, cancellationToken),
                ["active"] = await dbContext.OrganizationParentLinks.CountAsync(l => l.Status == ParentLinkStatus.Active, cancellationToken)
            }));
        }

        if (Has(AccessPermissions.ManageAccessMatrix))
            sections.Add(new AdminSectionDto(AdminSections.AccessMatrix, AccessPermissions.ManageAccessMatrix, new Dictionary<string, int>()));

        if (Has(MaintainerPermissions.Manage))
        {
            sections.Add(new AdminSectionDto(AdminSections.Maintainers, MaintainerPermissions.Manage, new Dictionary<string, int>
            {
                ["tariffs"] = await dbContext.Tariffs.CountAsync(t => t.IsActive, cancellationToken),
                ["serviceDefinitions"] = await dbContext.ServiceDefinitions.CountAsync(d => d.IsActive, cancellationToken)
            }));
            sections.Add(new AdminSectionDto(AdminSections.Guides, MaintainerPermissions.Manage, new Dictionary<string, int>
            {
                ["active"] = await dbContext.GuideDefinitions.CountAsync(g => g.IsActive, cancellationToken)
            }));
        }

        if (Has(PaymentPermissions.ManageBlockWindows))
        {
            var today = BusinessCalendar.LocalDate(CountryCodes.Chile, now);
            sections.Add(new AdminSectionDto(AdminSections.PaymentBlocks, PaymentPermissions.ManageBlockWindows, new Dictionary<string, int>
            {
                ["scheduled"] = await dbContext.PaymentBlockWindows.CountAsync(w => w.IsActive && w.EndDate >= today, cancellationToken)
            }));
        }

        if (Has(PaymentPermissions.Finance))
        {
            sections.Add(new AdminSectionDto(AdminSections.Finance, PaymentPermissions.Finance, new Dictionary<string, int>
            {
                ["depositProofsToVerify"] = await dbContext.DepositProofs.CountAsync(p => p.Status == DepositProofStatus.Submitted, cancellationToken),
                ["paymentsPendingVerification"] = await dbContext.Payments.CountAsync(p => p.Status == PaymentStatus.PendingVerification, cancellationToken),
                ["stuckPostPaymentJobs"] = await dbContext.PaymentOutboxMessages.CountAsync(m => m.Status == PaymentOutboxStatus.Stuck, cancellationToken)
            }));
        }

        if (Has(ServiceRequestPermissions.Process))
        {
            sections.Add(new AdminSectionDto(AdminSections.ServiceRequests, ServiceRequestPermissions.Process, new Dictionary<string, int>
            {
                ["pendingApproval"] = await dbContext.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.PendingApproval, cancellationToken),
                ["inProgress"] = await dbContext.ServiceRequests.CountAsync(r => r.Status == ServiceRequestStatus.InProgress, cancellationToken)
            }));
        }

        if (Has(AdministrationPermissions.ManageAnnouncements))
        {
            sections.Add(new AdminSectionDto(AdminSections.Announcements, AdministrationPermissions.ManageAnnouncements, new Dictionary<string, int>
            {
                ["current"] = await dbContext.Announcements.CountAsync(a =>
                    a.Status == AnnouncementStatus.Published && a.ValidFrom <= now && (a.ValidTo == null || a.ValidTo > now), cancellationToken),
                ["drafts"] = await dbContext.Announcements.CountAsync(a => a.Status == AnnouncementStatus.Draft, cancellationToken)
            }));
        }

        if (Has(AdministrationPermissions.ManageCounter))
        {
            sections.Add(new AdminSectionDto(AdminSections.Counter, AdministrationPermissions.ManageCounter, new Dictionary<string, int>
            {
                ["records"] = await dbContext.CounterRecords.CountAsync(cancellationToken),
                ["syncFailed"] = await dbContext.CounterRecords.CountAsync(r => r.SyncStatus == CounterSyncStatus.Failed, cancellationToken)
            }));
        }

        if (Has(AdministrationPermissions.ViewTransactionsReport))
            sections.Add(new AdminSectionDto(AdminSections.Reports, AdministrationPermissions.ViewTransactionsReport, new Dictionary<string, int>()));

        if (Has(AdministrationPermissions.UseImpersonation) && !currentUserService.IsImpersonating)
        {
            sections.Add(new AdminSectionDto(AdminSections.Impersonation, AdministrationPermissions.UseImpersonation, new Dictionary<string, int>
            {
                ["activeSessions"] = await dbContext.ImpersonationSessions.CountAsync(
                    s => s.Status == ImpersonationStatus.Active && s.ExpiresAt > now, cancellationToken)
            }));
        }

        if (Has("audit.view"))
            sections.Add(new AdminSectionDto(AdminSections.Audit, "audit.view", new Dictionary<string, int>()));

        if (Has("users.manage"))
            sections.Add(new AdminSectionDto(AdminSections.Users, "users.manage", new Dictionary<string, int>()));

        return Result<AdminOverviewDto>.Success(new AdminOverviewDto(now, sections));
    }
}
