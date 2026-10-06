namespace HapagPortal.Domain.Constants;

/// <summary>
/// Permisos internos de la Fase 2 Ola I (área de administración, M8-05). Se asignan solo a roles de Hapag-Lloyd
/// (Administrador/SuperAdmin por comodín); la impersonación exige además que quien la usa sea un usuario interno.
/// </summary>
public static class AdministrationPermissions
{
    /// <summary>Entrada al área de administración y su resumen (M8-05).</summary>
    public const string AccessAdminArea = "admin-area.access";

    /// <summary>«Vista como cliente» (M8-08), sujeta a las restricciones de seguridad de la sesión.</summary>
    public const string UseImpersonation = "impersonation.use";

    /// <summary>Publicación y vigencia de los comunicados (M1-26).</summary>
    public const string ManageAnnouncements = "announcements.manage";

    /// <summary>Registro de Counter Bolivia/Ultramar (M8-09).</summary>
    public const string ManageCounter = "counter.manage";

    /// <summary>Reportería general de transacciones y excepciones (M9-01).</summary>
    public const string ViewTransactionsReport = "transactions-report.view";
}

/// <summary>Operación a la que va dirigido un comunicado (M1-26, M2-07).</summary>
public static class AnnouncementOperations
{
    public const string Import = "Import";
    public const string Export = "Export";
    public const string Both = "Both";

    public static readonly string[] All = [Import, Export, Both];
}

public static class AnnouncementSeverities
{
    public const string Info = "Info";
    public const string Important = "Important";

    public static readonly string[] All = [Info, Important];
}

/// <summary>Estado de publicación de un comunicado (M1-26). Solo <see cref="Published"/> y vigente se muestra.</summary>
public static class AnnouncementStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Unpublished = "Unpublished";

    public static readonly string[] All = [Draft, Published, Unpublished];
}

/// <summary>A quién se ofrece una guía (M1-27).</summary>
public static class GuideAudiences
{
    public const string Client = "Client";
    public const string Internal = "Internal";
    public const string All = "All";

    public static readonly string[] Values = [Client, Internal, All];
}

/// <summary>Estado de una guía para el usuario (M1-27). <see cref="Reset"/> solo se usa para volver a ofrecerla.</summary>
public static class GuideStates
{
    public const string Completed = "Completed";
    public const string Dismissed = "Dismissed";
    public const string Reset = "Reset";

    public static readonly string[] Settable = [Completed, Dismissed, Reset];
}

public static class ImpersonationStatus
{
    public const string Active = "Active";
    public const string Ended = "Ended";
    public const string Expired = "Expired";
}

public static class ImpersonationEndReasons
{
    public const string Manual = "Manual";
    public const string Logout = "Logout";
    public const string Expired = "Expired";
    public const string Replaced = "Replaced";
    public const string Admin = "Admin";
}

/// <summary>
/// Claims del token de una sesión de «Vista como cliente» (M8-08): el sujeto (<c>sub</c>) es el usuario cliente y estos
/// identifican la sesión y al actor interno que la inició.
/// </summary>
public static class ImpersonationClaims
{
    public const string SessionId = "imp_sid";
    public const string ActorUserId = "imp_act";
    public const string ActorEmail = "imp_act_email";
}

/// <summary>Registro de auditoría (<c>AuditLogs</c>) de la impersonación (M8-08, NF-14).</summary>
public static class ImpersonationAuditActions
{
    public const string EntityName = "ImpersonationSession";
    public const string Started = "Started";
    public const string Ended = "Ended";
    public const string Request = "Request";
    public const string BlockedWrite = "BlockedWrite";
}

/// <summary>Sincronización de un registro de Counter con Nexus (M8-09).</summary>
public static class CounterSyncStatus
{
    public const string Pending = "Pending";
    public const string Synced = "Synced";
    public const string Failed = "Failed";

    public static readonly string[] All = [Pending, Synced, Failed];
}

/// <summary>
/// Tipos de reporte con lista de distribución en el registro de contactos (M1-06, P0060): aviso de arribo, copias de
/// BL, facturas, free time, demurrage y confirmación de booking. Coinciden con el contrato CT-CONTACTS.
/// </summary>
public static class ContactReportTypes
{
    public const string ArrivalNotice = "ARRIVAL_NOTICE";
    public const string BlCopies = "BL_COPIES";
    public const string Invoices = "INVOICES";
    public const string FreeTime = "FREE_TIME";
    public const string Demurrage = "DEMURRAGE";
    public const string BookingConfirmation = "BOOKING_CONFIRMATION";

    public static readonly string[] All = [ArrivalNotice, BlCopies, Invoices, FreeTime, Demurrage, BookingConfirmation];

    /// <summary>Máximo de correos por lista.</summary>
    public const int MaxEmails = 20;
}

public static class ContactListChangeStatus
{
    public const string Propagated = "Propagated";
    public const string Failed = "Failed";
}

public static class CarrierPreRegistrationStatus
{
    public const string Pending = "Pending";
    public const string Activated = "Activated";
}

/// <summary>Estado del vínculo entre una filial y su empresa matriz (M1-21).</summary>
public static class ParentLinkStatus
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Rejected = "Rejected";
    public const string Removed = "Removed";

    public static readonly string[] All = [Pending, Active, Rejected, Removed];

    /// <summary>Estados que impiden pedir otro vínculo.</summary>
    public static readonly string[] Open = [Pending, Active];
}
