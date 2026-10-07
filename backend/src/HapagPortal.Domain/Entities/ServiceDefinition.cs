using HapagPortal.Domain.Common;
using HapagPortal.Domain.Constants;

namespace HapagPortal.Domain.Entities;

/// <summary>
/// Definición estándar de un concepto de cobro on demand (M2-03 importación, M2-04 exportación): una vez
/// configurados sus reglas, datos requeridos, tarifa y condiciones, el servicio se habilita en el portal sin
/// desarrollo propio. Se administra desde el área interna con registro de cambios (NF-15).
/// <list type="bullet">
/// <item>Aplicabilidad: países, operaciones, estados del BL, ventana del embarque (por ejemplo exportación
/// después del zarpe), contenedores y acción de la matriz M1-11 (<see cref="ActionCode"/>).</item>
/// <item>Datos: formulario tipado (<see cref="InputSchemaJson"/>), datos de facturación y aceptación de tarifa.</item>
/// <item>Cobro: tarifa vigente del mantenedor del concepto (con tramos por tiempo desde un hito o por un dato
/// ingresado), o cargos del sistema de origen con exenciones de Nexus; IVA del país si corresponde.</item>
/// <item>Flujo: aprobación previa de un equipo interno y prestación posterior al pago.</item>
/// </list>
/// </summary>
public sealed class ServiceDefinition : BaseAuditableEntity
{
    public required string Code { get; set; }
    public required string NameEs { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionEs { get; set; }
    public string? DescriptionEn { get; set; }

    /// <summary>Operaciones (IMPORT, EXPORT) y países (CL, BO) separados por coma.</summary>
    public required string Operations { get; set; }
    public required string Countries { get; set; }

    /// <summary>Referencia con que se solicita (<see cref="ServiceReferenceTypes"/>: BL o booking).</summary>
    public string ReferenceType { get; set; } = ServiceReferenceTypes.Bl;

    /// <summary>Estados del BL en que aplica, separados por coma; nulo = cualquiera.</summary>
    public string? RequiredBlStatuses { get; set; }

    public string AvailabilityWindow { get; set; } = ServiceAvailabilityWindows.Always;
    public bool RequiresContainers { get; set; }

    /// <summary>Falso: una sola solicitud vigente (no rechazada ni anulada) por BL.</summary>
    public bool AllowMultiplePerBl { get; set; } = true;

    /// <summary>Formulario: lista JSON de campos tipados (<see cref="ServiceInputFieldTypes"/>).</summary>
    public string InputSchemaJson { get; set; } = "[]";

    public bool BillingDataRequired { get; set; }
    public bool TariffAcceptanceRequired { get; set; }

    public string PricingMode { get; set; } = ServicePricingModes.None;
    public string? ChargeConceptCode { get; set; }

    /// <summary>Código de tarifa del concepto (nulo = la primera vigente) y el de fuera de plazo.</summary>
    public string? TariffCode { get; set; }
    public string? LateTariffCode { get; set; }

    public string QuantityMode { get; set; } = ServiceQuantityModes.PerRequest;

    /// <summary>Campo numérico del formulario que es la medida de los tramos (por ejemplo, horas de atraso).</summary>
    public string? MeasureFieldKey { get; set; }

    public string Milestone { get; set; } = ServiceMilestones.None;
    public int MilestoneOffsetHours { get; set; }
    public string? DeadlineRuleCode { get; set; }
    public string TimingRule { get; set; } = ServiceTimingRules.None;

    /// <summary>Aplica el impuesto del país (<c>TaxConfiguration</c>) al cargo generado.</summary>
    public bool Taxable { get; set; } = true;

    /// <summary>Concepto de exención de Nexus que excluye el cobro (por ejemplo XOM, M3-10).</summary>
    public string? ExemptionConcept { get; set; }

    /// <summary>Los contenedores del embarcador (SOC) no se cobran (excepción por unidad, M3-10).</summary>
    public bool ExcludeShipperOwnedContainers { get; set; }

    public string ApprovalTeam { get; set; } = ServiceTeams.None;
    public string FulfillmentTeam { get; set; } = ServiceTeams.None;

    /// <summary>El equipo interno debe adjuntar un documento de salida al completar.</summary>
    public bool RequiresOutputDocument { get; set; }

    public required string ActionCode { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
