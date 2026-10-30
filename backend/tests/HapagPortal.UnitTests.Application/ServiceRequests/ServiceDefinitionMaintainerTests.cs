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

    /// <summary>Entrada completa en ES, EN y PT (nombre y etiquetas de los campos), como la exige el validador.</summary>
    private static ServiceDefinitionInput CompleteInput(
        string quantityMode = ServiceQuantityModes.PerRequest,
        string? measureField = null) =>
        Input(quantityMode: quantityMode, measureField: measureField,
            fields: [new("notes", "Notas", "Notes", ServiceInputFieldTypes.TextArea, LabelPt: "Observações")]) with { NamePt = "Serviço novo" };

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

        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput())).IsValid.Should().BeTrue();

        var perContainerWithoutField = validator.Validate(new CreateServiceDefinitionCommand(CompleteInput(quantityMode: ServiceQuantityModes.PerContainer)));
        perContainerWithoutField.Errors.Should().Contain(e => e.PropertyName == "QuantityMode");

        var measureNotNumber = validator.Validate(new CreateServiceDefinitionCommand(CompleteInput(measureField: "notes")));
        measureNotNumber.Errors.Should().Contain(e => e.PropertyName == "MeasureFieldKey");

        var lateWithoutMilestone = validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { TimingRule = ServiceTimingRules.LateOnly }));
        lateWithoutMilestone.Errors.Should().Contain(e => e.PropertyName == "Milestone");

        var badSchema = validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with
        {
            InputSchema = [new("kind", "Tipo", "Type", ServiceInputFieldTypes.Select, LabelPt: "Tipo")],
        }));
        badSchema.Errors.Should().Contain(e => e.PropertyName == "InputSchema");

        var badCountry = validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { Countries = ["AR"] }));
        badCountry.Errors.Should().Contain(e => e.PropertyName == "Countries");
    }

    [Fact]
    public void Validator_ShouldRequirePortugueseNameAndLabels_AndLimitItsLength()
    {
        var validator = new CreateServiceDefinitionCommandValidator();

        // La descripción en portugués es opcional, como en español e inglés.
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput())).IsValid.Should().BeTrue();
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { DescriptionPt = "Descrição" }))
            .IsValid.Should().BeTrue();
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { NamePt = null }))
            .Errors.Should().Contain(e => e.PropertyName == "NamePt");
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { NamePt = " " }))
            .Errors.Should().Contain(e => e.PropertyName == "NamePt");
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with
            {
                InputSchema = [new("notes", "Notas", "Notes", ServiceInputFieldTypes.TextArea)],
            }))
            .Errors.Should().Contain(e => e.PropertyName == "InputSchema");
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { NamePt = new string('a', 151) }))
            .Errors.Should().Contain(e => e.PropertyName == "NamePt");
        validator.Validate(new CreateServiceDefinitionCommand(CompleteInput() with { DescriptionPt = new string('a', 1001) }))
            .Errors.Should().Contain(e => e.PropertyName == "DescriptionPt");
        new UpdateServiceDefinitionCommandValidator().Validate(new UpdateServiceDefinitionCommand(Guid.NewGuid(), CompleteInput() with { NamePt = null }))
            .Errors.Should().Contain(e => e.PropertyName == "NamePt");
    }

    [Fact]
    public async Task Update_WithLegacySchemaJson_AndSameFields_ShouldKeepTheSameInputSchemaJson()
    {
        var created = await new CreateServiceDefinitionCommandHandler(_f.Db, _f.Internal)
            .Handle(new CreateServiceDefinitionCommand(Input()), CancellationToken.None);
        // Formato guardado antes del portugués: ayudas nulas escritas y sin labelPt ni helpPt.
        const string legacy =
            """[{"key":"notes","labelEs":"Notas","labelEn":"Notes","type":"textarea","required":false,"options":null,"min":null,"max":null,"integer":false,"maxLength":null,"helpEs":null,"helpEn":null}]""";
        var definition = _f.Db.ServiceDefinitionList.Single(d => d.Id == created.Value.Id);
        definition.InputSchemaJson = legacy;

        await new UpdateServiceDefinitionCommandHandler(_f.Db, _f.Internal)
            .Handle(new UpdateServiceDefinitionCommand(created.Value.Id, Input()), CancellationToken.None);
        var history = await new GetServiceDefinitionHistoryQueryHandler(_f.Db)
            .Handle(new GetServiceDefinitionHistoryQuery(created.Value.Id), CancellationToken.None);

        definition.InputSchemaJson.Should().Be(legacy);
        var change = history.Value.Single(h => h.Action == MaintainerActions.Updated);
        change.Current!.InputSchemaJson.Should().Be(change.Previous!.InputSchemaJson);
    }

    [Fact]
    public async Task Create_ShouldKeepPortugueseNameAndDescription()
    {
        var created = await new CreateServiceDefinitionCommandHandler(_f.Db, _f.Internal)
            .Handle(new CreateServiceDefinitionCommand(Input() with { NamePt = " Serviço novo ", DescriptionPt = " " }), CancellationToken.None);

        created.Value.NamePt.Should().Be("Serviço novo");
        created.Value.DescriptionPt.Should().BeNull();
    }
}
