namespace HapagPortal.Application.ChargeRules.Charges;

using FluentValidation;
using HapagPortal.Application.ChargeRules.Common;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Common.Messaging;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Domain.Errors;
using HapagPortal.Domain.Results;

/// <summary>
/// Aplica las condiciones de Nexus a los recargos del BL (M4-02, M3-01): los conceptos exentos quedan
/// en estado <c>Exempt</c> con su trazabilidad a la condición informada (<see cref="AppliedExemption"/>).
/// Si todos los cargos aplicables están exentos, el proceso queda completo sin pasar por el carro y sin
/// boleta de valor cero; si no, devuelve los cargos que el carro (Ola D) puede agregar.
/// </summary>
public sealed record ApplyChargeRulesCommand(string BlNumber) : ICommand<ApplyChargeRulesResultDto>;

public sealed class ApplyChargeRulesCommandValidator : AbstractValidator<ApplyChargeRulesCommand>
{
    public ApplyChargeRulesCommandValidator()
    {
        RuleFor(x => x.BlNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class ApplyChargeRulesCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IShipmentAccessEvaluator accessEvaluator,
    IChargeRulesService chargeRulesService)
    : ICommandHandler<ApplyChargeRulesCommand, ApplyChargeRulesResultDto>
{
    public async Task<Result<ApplyChargeRulesResultDto>> Handle(ApplyChargeRulesCommand request, CancellationToken cancellationToken)
    {
        var loaded = await ShipmentChargeContextLoader.LoadAsync(
            dbContext, accessEvaluator, request.BlNumber, forOperation: true, include: null, cancellationToken);
        if (loaded.IsFailure)
            return Result<ApplyChargeRulesResultDto>.Failure(loaded.Error);

        var context = loaded.Value;

        if (!context.Permissions.CanExecute(ShipmentActionCodes.PayMandatoryLocalCharges))
            return Result<ApplyChargeRulesResultDto>.Failure(Error.Forbidden);

        var evaluation = await chargeRulesService.EvaluateAsync(
            context.BillOfLading, context.Payer, context.Permissions, cancellationToken);

        if (!evaluation.Result.RulesAvailable)
            return Result<ApplyChargeRulesResultDto>.Failure(DomainErrors.ChargeRules.ConditionsUnavailable);

        if (evaluation.Result.Charges.All(c => c.Outcome == ChargeOutcomes.Paid))
            return Result<ApplyChargeRulesResultDto>.Failure(DomainErrors.ChargeRules.NoChargesToApply);

        var now = DateTime.UtcNow;
        var charges = evaluation.Charges.ToDictionary(c => c.Id);

        foreach (var exemption in evaluation.Exemptions)
        {
            var charge = charges[exemption.ChargeId];
            var trace = exemption.Trace;

            if (exemption.FullyExempt)
                charge.Status = ChargeStatus.Exempt;

            dbContext.AppliedExemptions.Add(new AppliedExemption
            {
                BillOfLadingId = context.BillOfLading.Id,
                LocalChargeId = charge.Id,
                ConceptCode = charge.ChargeType,
                ExemptParty = trace.Party,
                PartyTaxId = trace.TaxId,
                PartyMatchCode = trace.MatchCode,
                ExemptAmount = trace.ExemptAmount,
                Currency = charge.Currency,
                ConditionAmount = trace.ConditionAmount,
                ConditionCurrency = trace.ConditionCurrency,
                ConditionValidFrom = trace.ValidFrom,
                ConditionValidTo = trace.ValidTo,
                Source = trace.Source,
                PayerClientId = context.Payer.Id,
                AppliedByUserId = currentUserService.UserId,
                AppliedAt = now
            });
        }

        if (evaluation.Exemptions.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        var exemptIds = evaluation.Exemptions.Select(e => e.ChargeId).ToHashSet();
        var result = evaluation.Result with
        {
            Charges = evaluation.Result.Charges
                .Select(c => exemptIds.Contains(c.ChargeId)
                    ? c with
                    {
                        Status = charges[c.ChargeId].Status,
                        Exemption = c.Exemption is null ? null : c.Exemption with { AppliedAt = now }
                    }
                    : c)
                .ToList()
        };

        return Result<ApplyChargeRulesResultDto>.Success(new ApplyChargeRulesResultDto(
            Completed: !result.RequiresPayment,
            RequiresPayment: result.RequiresPayment,
            ExemptedCharges: result.Charges.Where(c => exemptIds.Contains(c.ChargeId)).ToList(),
            PayableChargeIds: result.Charges.Where(c => c.PayableTotal > 0m).Select(c => c.ChargeId).ToList(),
            result));
    }
}
