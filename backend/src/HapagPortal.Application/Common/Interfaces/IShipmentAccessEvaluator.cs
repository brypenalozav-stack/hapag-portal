namespace HapagPortal.Application.Common.Interfaces;

using HapagPortal.Application.Common.Access;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Shipments;

/// <summary>
/// Punto único de autorización por embarque (M1-11, NF-05). Toda consulta o acción sobre un BL,
/// sus cargos, demurrage o pagos debe pasar por aquí; la interfaz solo oculta opciones.
/// Ola B suma los accesos otorgados vigentes (M1-12 a M1-15), las ampliaciones entre roles (M1-16),
/// el acceso abierto por número de BL (M1-17) y la autoasociación (M1-18).
/// </summary>
public interface IShipmentAccessEvaluator
{
    /// <summary>Contexto del usuario actual: organización, estado y perfil.</summary>
    Task<AccessScope> GetScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restringe la consulta a los embarques accesibles por el alcance (vacía si no opera): propios,
    /// recibidos por un acceso otorgado vigente y autoasociados con el acceso abierto del titular activo.
    /// El acceso abierto sin asociación no entra aquí: solo se consulta por número (<see cref="FilterOpenAccess"/>).
    /// </summary>
    IQueryable<BillOfLading> FilterAccessible(IQueryable<BillOfLading> source, AccessScope scope);

    /// <summary>
    /// Embarques cuyo titular tiene activo el acceso abierto por número de BL (M1-17). Solo debe usarse
    /// con un número de BL exacto ingresado por el usuario, nunca para listar.
    /// </summary>
    IQueryable<BillOfLading> FilterOpenAccess(IQueryable<BillOfLading> source, AccessScope scope);

    /// <summary>
    /// Roles efectivos de la organización sobre cada BL indicado: los propios del embarque y, si
    /// accede por un acceso otorgado o autoasociado, Tercero (o el rol asignado por booking).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRolesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default);

    /// <summary>Origen del acceso a cada BL (<c>ShipmentAccessSources</c>).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetAccessSourcesAsync(
        AccessScope scope,
        IReadOnlyCollection<BillOfLading> billsOfLading,
        CancellationToken cancellationToken = default);

    /// <summary>Permisos efectivos sobre un BL; sin roles, accesos ni administración, ninguna acción.</summary>
    Task<ShipmentPermissionSet> EvaluateAsync(
        AccessScope scope,
        BillOfLading billOfLading,
        CancellationToken cancellationToken = default);

    /// <summary>Matriz de M1-11 vigente (para validar otorgamientos).</summary>
    Task<AccessMatrixSnapshot> GetMatrixAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Decisión de publicación del BL según las reglas de DIFU de destino final (M2-01). Los BL no publicados
    /// quedan fuera de <see cref="FilterAccessible"/>, <see cref="FilterOpenAccess"/> y <see cref="EvaluateAsync"/>
    /// para los clientes; el administrador interno los ve con el motivo.
    /// </summary>
    Task<PublicationDecision> GetPublicationAsync(BillOfLading billOfLading, CancellationToken cancellationToken = default);
}

/// <summary>
/// Alcance del usuario. <see cref="IsOperational"/> exige usuario activo, vinculación aprobada y
/// organización aprobada. <see cref="IsAdmin"/> es el perfil interno con visibilidad total (M8-06).
/// <see cref="CanOperate"/> refleja el perfil del usuario (M1-02): sin él solo consulta.
/// </summary>
public sealed record AccessScope(
    Guid? UserId,
    Guid? OrganizationId,
    string? OrganizationType,
    bool IsAdmin,
    bool IsOperational,
    bool CanOperate)
{
    public static readonly AccessScope None = new(null, null, null, false, false, false);
}

/// <summary>Acceso otorgado vigente que aporta acciones a la organización sobre el BL.</summary>
public sealed record ShipmentGrantAccess(
    Guid GrantId,
    Guid GrantorOrganizationId,
    bool IsMandate,
    DateTime? ValidTo,
    IReadOnlyList<string> Actions);

/// <summary>Resultado de evaluar la matriz para un BL.</summary>
public sealed record ShipmentPermissionSet(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> AllowedActions,
    bool IsAdmin,
    bool CanOperate)
{
    public static readonly ShipmentPermissionSet None = new([], [], false, false) { AccessSource = "None" };

    /// <summary>Origen del acceso: Own, Grant, Parent, SelfAssociated, OpenAccess, Admin o None.</summary>
    public string AccessSource { get; init; } = "Own";

    /// <summary>Filial de origen cuando el BL se ve como empresa matriz (M1-21).</summary>
    public Guid? OriginOrganizationId { get; init; }

    /// <summary>Acciones de los roles propios del embarque (sin accesos otorgados ni acceso abierto).</summary>
    public IReadOnlyList<string> OwnActions { get; init; } = [];

    /// <summary>Accesos otorgados vigentes que aportan acciones (M1-12).</summary>
    public IReadOnlyList<ShipmentGrantAccess> Grants { get; init; } = [];

    /// <summary>Visto por acceso abierto y todavía sin asociar: puede autoasociarse (M1-18).</summary>
    public bool CanSelfAssociate { get; init; }

    /// <summary>
    /// Agencia de aduanas o transportista que ve el BL solo por acceso abierto: debe asociarse antes
    /// de incorporar sus cargos al carro de compra (M1-18, M5-01).
    /// </summary>
    public bool RequiresAssociationForPayment { get; init; }

    public bool HasAccess => IsAdmin || Roles.Count > 0;

    /// <summary>El rol (o la administración) permite la acción o el dato.</summary>
    public bool Can(string actionCode) => IsAdmin || AllowedActions.Contains(actionCode);

    /// <summary>Permite la acción y el perfil del usuario puede operar (pagar, solicitar, generar).</summary>
    public bool CanExecute(string actionCode) => Can(actionCode) && CanOperate;

    /// <summary>
    /// Acceso otorgado bajo el que se ejecutaría la acción, cuando los roles propios no la habilitan
    /// (NF-14: la operación identifica al mandatario y al mandante). Nulo si es propia.
    /// </summary>
    public ShipmentGrantAccess? GrantFor(string actionCode) =>
        IsAdmin || OwnActions.Contains(actionCode)
            ? null
            : Grants.FirstOrDefault(g => g.Actions.Contains(actionCode));
}
