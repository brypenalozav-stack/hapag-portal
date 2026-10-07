namespace HapagPortal.UnitTests.Application.ServiceRequests;

using FluentAssertions;
using HapagPortal.Application.ServiceRequests.Definitions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.ServiceRequests;
using HapagPortal.UnitTests.Application.TestHelpers;

/// <summary>
/// Mantenedor de definiciones de servicios on demand (M2-03, M2-04): validación de la configuración, referencias a
/// los catálogos (concepto, acción de M1-11, regla de plazo) y registro de cambios (NF-15).
/// </summary>
public sealed class ServiceDefinitionMaintainerTests
{
    private readonly ServiceRequestsFixture _f = new();

    private static ServiceDefinitionInput Input(
        string code = "NEW_SERVICE",
        string quantityMode = ServiceQuantityModes.PerRequest,
        string actionCode = ShipmentActionCodes.PayOnDemandLocalCharges,
        string? measureField = null,
        IReadOnlyList<ServiceInputField>? fields = null) =>
        new(code, "Servicio nuevo", "New service", null, null, ["EXPORT"], ["CL", "BO"], ServiceReferenceTypes.Bl, null,
            ServiceAvailabilityWindows.AfterDeparture, false, true,
            fields ?? [new("notes", "Notas", "Notes", ServiceInputFieldTypes.TextArea)],
            true, false, ServicePricingModes.Tariff, ChargeConceptCodes.BlCorrection, null, null, quantityMode, measureField,
            ServiceMilestones.None, 0, null, ServiceTimingRules.None, true, null, false, ServiceTeams.None,
            ServiceTeams.CustomerService, false, actionCode, 10);

    [Fact]
    public async Task Create_ShouldNormalizeAndLogTheChange_AndUpdateShouldKeepThePreviousValue()
    {
        var created = await new CreateServiceDefinitionCommandHandler(_f.Db, _f.Internal)
            .Handle(new CreateServiceDefinitionCommand(Input(code: "new_service")), CancellationToken.None);

        created.IsSuccess.Should().BeTrue(created.IsFailure ? created.Error.Code : null);
        created.Value.Code.Should().Be("NEW_SERVICE");
        created.Value.Countries.Should().Equal("CL", "BO");
        created.Value.InputSchema.Should().ContainSingle(f => f.Key == "notes");

        var updated = await new UpdateServiceDefinitionCommandHandler(_f.Db, _f.Internal)
            .Handle(new UpdateServiceDefinitionCommand(created.Value.Id, Input() with { NameEs = "Servicio renombrado" }), CancellationToken.None);
        var history = await new GetServiceDefinitionHistoryQueryHandler(_f.Db)
            .Handle(new GetServiceDefinitionHistoryQuery(created.Value.Id), CancellationToken.None);

        updated.Value.NameEs.Should().Be("Servicio renombrado");
        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Created, MaintainerActions.Updated]);
        var change = history.Value.Single(h => h.Action == MaintainerActions.Updated);
        change.Previous!.NameEs.Should().Be("Servicio nuevo");
        change.Current!.NameEs.Should().Be("Servicio renombrado");
    }

    [Fact]
    public async Task Create_ShouldRejectUnknownReferences_AndDuplicatedCodes()
    {
        var handler = new CreateServiceDefinitionCommandHandler(_f.Db, _f.Internal);

        (await handler.Handle(new CreateServiceDefinitionCommand(Input(actionCode: "unknown.action")), CancellationToken.None))
            .Error.Code.Should().Be("ServiceDefinition.UnknownAction");
        (await handler.Handle(new CreateServiceDefinitionCommand(Input() with { ChargeConceptCode = "NOPE" }), CancellationToken.None))
            .Error.Code.Should().Be("ChargeConcept.NotFound");

        (await handler.Handle(new CreateServiceDefinitionCommand(Input()), CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await handler.Handle(new CreateServiceDefinitionCommand(Input()), CancellationToken.None))
            .Error.Code.Should().Be("ServiceDefinition.AlreadyExists");
    }

    [Fact]
    public void Validator_ShouldRejectInconsistentConfigurations()
    {
        var validator = new CreateServiceDefinitionCommandValidator();

        validator.Validate(new CreateServiceDefinitionCommand(Input())).IsValid.Should().BeTrue();

        var perContainerWithoutField = validator.Validate(new CreateServiceDefinitionCommand(Input(quantityMode: ServiceQuantityModes.PerContainer)));
        perContainerWithoutField.Errors.Should().Contain(e => e.PropertyName == "QuantityMode");

        var measureNotNumber = validator.Validate(new CreateServiceDefinitionCommand(Input(measureField: "notes")));
        measureNotNumber.Errors.Should().Contain(e => e.PropertyName == "MeasureFieldKey");

        var lateWithoutMilestone = validator.Validate(new CreateServiceDefinitionCommand(Input() with { TimingRule = ServiceTimingRules.LateOnly }));
        lateWithoutMilestone.Errors.Should().Contain(e => e.PropertyName == "Milestone");

        var badSchema = validator.Validate(new CreateServiceDefinitionCommand(Input(fields:
            [new("kind", "Tipo", "Type", ServiceInputFieldTypes.Select)])));
        badSchema.Errors.Should().Contain(e => e.PropertyName == "InputSchema");

        var badCountry = validator.Validate(new CreateServiceDefinitionCommand(Input() with { Countries = ["AR"] }));
        badCountry.Errors.Should().Contain(e => e.PropertyName == "Countries");
    }
}
