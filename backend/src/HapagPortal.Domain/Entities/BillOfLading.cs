using HapagPortal.Domain.Common;

namespace HapagPortal.Domain.Entities;

public sealed class BillOfLading : BaseAuditableEntity
{
    public required string BLNumber { get; set; }
    public string? BookingNumber { get; set; }
    public required string ShipmentType { get; set; }
    public string? Vessel { get; set; }
    public string? Voyage { get; set; }
    public string? PortOfLoading { get; set; }
    public string? PortOfDischarge { get; set; }
    public string? PlaceOfDelivery { get; set; }
    public DateTime? ETD { get; set; }
    public DateTime? ETA { get; set; }
    public string? Consignee { get; set; }
    public string? Shipper { get; set; }
    public string? NotifyParty { get; set; }
    public decimal FreightAmount { get; set; }
    public required string FreightCurrency { get; set; }
    public required string Status { get; set; }
    public required string Country { get; set; }
    public Guid ClientId { get; set; }

    // Campos aduaneros (DCSA eBL v3). Nullable para no romper el seed/consultas actuales.
    public string? BLType { get; set; }          // Master | House | Grandchild (Máster/Hijo/Nieto)
    public Guid? ParentBLId { get; set; }         // Hijo -> Máster; Nieto -> Hijo
    public bool IsToOrder { get; set; }
    public bool IsSeaWaybill { get; set; }
    public string? VesselImo { get; set; }
    public string? OperatorVoyage { get; set; }
    public string? FreightTerms { get; set; }     // Prepaid | Collect
    public string? Incoterm { get; set; }

    /// <summary>Flete pagado desde el carro o la vista de crédito (Ola D, liberación de NF-03).</summary>
    public DateTime? FreightPaidAt { get; set; }

    // Fase 1 Ola F: datos del sistema de origen (CT-FIS) para la publicación (M2-01) y la emisión (M2-02).

    /// <summary>UN/LOCODE del puerto de descarga (p. ej. CLSAI).</summary>
    public string? PortOfDischargeCode { get; set; }

    /// <summary>UN/LOCODE de la localidad de destino final de la carga (p. ej. CLANF).</summary>
    public string? FinalDestinationCode { get; set; }

    /// <summary>DIFU (estructura de distribución) informado por el origen y la localidad a la que está asociado.</summary>
    public string? DifuCode { get; set; }

    public string? DifuLocationCode { get; set; }

    /// <summary>Documento de transporte (<c>TransportDocumentTypes</c>: BL, SWB o EBL) y plataforma del EBL (p. ej. WAVE).</summary>
    public string? TransportDocumentType { get; set; }

    public string? EblPlatform { get; set; }

    /// <summary>Último estado de emisión conocido (<c>BlIssuanceStatuses</c>) y su fecha en el origen.</summary>
    public string? IssuanceStatus { get; set; }

    public DateTime? IssuanceStatusAt { get; set; }

    public Client Client { get; set; } = null!;
    public BillOfLading? ParentBL { get; set; }
    public ICollection<BLContainer> Containers { get; set; } = [];
    public ICollection<BLParty> Parties { get; set; } = [];
    public ICollection<BLCargoItem> CargoItems { get; set; } = [];
    public ICollection<LocalCharge> LocalCharges { get; set; } = [];
    public ICollection<DemurrageCharge> DemurrageCharges { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<ShipmentRole> ShipmentRoles { get; set; } = [];
}
