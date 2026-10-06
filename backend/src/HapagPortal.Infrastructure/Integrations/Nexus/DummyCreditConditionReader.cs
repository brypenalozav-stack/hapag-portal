using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Results;
using Microsoft.Extensions.Logging;

namespace HapagPortal.Infrastructure.Integrations.Nexus;

/// <summary>
/// Condiciones comerciales simuladas de Nexus (CT-NEXUS). Determinista: <c>76000002-2</c> tiene crédito
/// a 30 días para LOCAL_CHARGES y MHD con un cupo de CLP 5.000.000 (extensión propuesta, Ola H); <c>76000003-3</c>
/// es FFWW sin crédito; <c>76000001-1</c> existe sin crédito; cualquier otro RUT no tiene condiciones
/// (<c>Success(null)</c>).
/// </summary>
public sealed class DummyCreditConditionReader(ILogger<DummyCreditConditionReader> logger) : ICreditConditionReader
{
    public Task<Result<CustomerConditions?>> GetConditionsAsync(
        string taxId,
        string? matchCode,
        CancellationToken cancellationToken = default)
    {
        CustomerConditions? conditions = null;

        if (DummyNexusData.IsTaxId(taxId, DummyNexusData.CreditTaxId))
        {
            conditions = new CustomerConditions(
                DummyNexusData.CreditTaxId,
                matchCode,
                IsFreightForwarder: false,
                Credit: new CreditCondition(
                    ["LOCAL_CHARGES", "MHD"], 30, DummyNexusData.ValidFrom, null,
                    DummyNexusData.CreditLimit, DummyNexusData.CreditLimitCurrency));
        }
        else if (DummyNexusData.IsTaxId(taxId, DummyNexusData.FreightForwarderTaxId))
        {
            conditions = new CustomerConditions(DummyNexusData.FreightForwarderTaxId, matchCode, IsFreightForwarder: true, Credit: null);
        }
        else if (DummyNexusData.IsTaxId(taxId, DummyNexusData.ExemptTaxId))
        {
            conditions = new CustomerConditions(DummyNexusData.ExemptTaxId, matchCode, IsFreightForwarder: false, Credit: null);
        }

        logger.LogDebug(
            "Condiciones Nexus (dummy) - TaxId: {TaxId}, Found: {Found}",
            taxId, conditions is not null);

        return Task.FromResult(Result<CustomerConditions?>.Success(conditions));
    }
}
