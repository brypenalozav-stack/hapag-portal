using FluentAssertions;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.ServiceRequests;

namespace HapagPortal.UnitTests.Domain.ServiceRequests;

/// <summary>
/// Formulario tipado de las definiciones de servicio (M2-03, M2-04): validación del esquema del mantenedor y de
/// los datos ingresados por el cliente (tipos, obligatorios al enviar, contenedores del BL y archivos adjuntos).
/// </summary>
public sealed class ServiceInputSchemaTests
{
    private static readonly IReadOnlyList<ServiceInputField> Fields =
    [
        new("containers", "Contenedores", "Containers", ServiceInputFieldTypes.Containers, true),
        new("returnDate", "Fecha", "Date", ServiceInputFieldTypes.Date, true),
        new("hours", "Horas", "Hours", ServiceInputFieldTypes.Number, true, Min: 1, Max: 240, Integer: true),
        new("depot", "Depósito", "Depot", ServiceInputFieldTypes.Select, true, [new("SCL_PUDAHUEL", "Pudahuel", "Pudahuel")]),
        new("notes", "Notas", "Notes", ServiceInputFieldTypes.Text, MaxLength: 10),
        new("support", "Respaldo", "Support", ServiceInputFieldTypes.File, true),
    ];

    private static readonly string[] BlContainers = ["HLXU1234567", "HLXU7654321"];

    private static ServiceInputValidation Validate(string json, bool complete = true, params string[] attached) =>
        ServiceInputSchema.ValidateValues(Fields, json, BlContainers, attached, complete);

    [Fact]
    public void ValidValues_ShouldBeNormalized_WithSelectedContainersAndNumbers()
    {
        var result = Validate(
            "{\"containers\":[\"hlxu1234567\",\"HLXU1234567\"],\"returnDate\":\"2026-10-09\",\"hours\":30,\"depot\":\"SCL_PUDAHUEL\",\"notes\":\"  ok  \"}",
            attached: "support");

        result.IsValid.Should().BeTrue();
        result.Containers.Should().Equal("HLXU1234567");
        result.Numbers["hours"].Should().Be(30m);
        result.NormalizedJson.Should().Contain("\"notes\":\"ok\"").And.Contain("[\"HLXU1234567\"]");
    }

    [Fact]
    public void TypeErrors_ShouldBeReportedPerField()
    {
        var result = Validate(
            "{\"containers\":[\"HLXU0000000\"],\"returnDate\":\"09-10-2026\",\"hours\":2.5,\"depot\":\"OTHER\",\"notes\":\"demasiado largo\",\"extra\":1}",
            attached: "support");

        result.Errors.Select(e => e.Field).Should().BeEquivalentTo(["containers", "returnDate", "hours", "depot", "notes", "extra"]);
        result.Errors.Single(e => e.Field == "containers").Message.Should().Contain("does not belong");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(241)]
    public void Numbers_ShouldRespectTheRange(int hours)
    {
        var result = Validate($"{{\"hours\":{hours}}}", complete: false);

        result.Errors.Should().ContainSingle(e => e.Field == "hours");
    }

    [Fact]
    public void RequiredFields_ShouldOnlyBeEnforcedWhenSubmitting()
    {
        var draft = Validate("{\"hours\":5}", complete: false);
        var submitted = Validate("{\"hours\":5}");

        draft.IsValid.Should().BeTrue();
        submitted.Errors.Select(e => e.Field).Should().BeEquivalentTo(["containers", "returnDate", "depot", "support"]);
    }

    [Fact]
    public void FileFields_ShouldBeAnsweredWithAttachmentsNotValues()
    {
        var result = Validate("{\"support\":\"file.pdf\"}", complete: false);

        result.Errors.Should().ContainSingle(e => e.Field == "support" && e.Message.Contains("attachment"));
    }

    [Fact]
    public void NonObjectValues_ShouldBeRejected()
    {
        var result = Validate("[1,2]", complete: false);

        result.Errors.Should().ContainSingle(e => e.Field == "inputValues");
    }

    [Fact]
    public void Schema_ShouldRejectDuplicatedKeys_UnknownTypes_SelectsWithoutOptions_AndTwoContainerFields()
    {
        IReadOnlyList<ServiceInputField> fields =
        [
            new("code", "Código", "Code", ServiceInputFieldTypes.Text),
            new("code", "Código", "Code", ServiceInputFieldTypes.Text),
            new("Bad-Key", "X", "X", ServiceInputFieldTypes.Text),
            new("kind", "Tipo", "Type", ServiceInputFieldTypes.Select),
            new("color", "Color", "Color", "colour"),
            new("units", "Unidades", "Units", ServiceInputFieldTypes.Containers),
            new("units2", "Unidades", "Units", ServiceInputFieldTypes.Containers),
            new("range", "Rango", "Range", ServiceInputFieldTypes.Number, Min: 5, Max: 1),
            new("label", "", "Label", ServiceInputFieldTypes.Text),
        ];

        var errors = ServiceInputSchema.ValidateSchema(fields);

        errors.Should().Contain(e => e.Contains("duplicated"));
        errors.Should().Contain(e => e.Contains("Bad-Key"));
        errors.Should().Contain(e => e.Contains("requires options"));
        errors.Should().Contain(e => e.Contains("unknown type"));
        errors.Should().Contain(e => e.Contains("at most one containers field"));
        errors.Should().Contain(e => e.Contains("min greater than max"));
        errors.Should().Contain(e => e.Contains("labels"));
        ServiceInputSchema.ValidateSchema(Fields).Should().BeEmpty();
    }

    [Fact]
    public void Schema_ShouldRoundTripThroughJson()
    {
        var json = ServiceInputSchema.Serialize(Fields);

        ServiceInputSchema.Parse(json).Should().BeEquivalentTo(Fields);
        ServiceInputSchema.Parse("not json").Should().BeNull();
    }

    [Fact]
    public void StateMachine_ShouldAllowTheStandardFlowOnly()
    {
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.Draft, ServiceRequestStatus.Submitted).Should().BeTrue();
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.PendingApproval, ServiceRequestStatus.Rejected).Should().BeTrue();
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.PendingPayment, ServiceRequestStatus.Paid).Should().BeTrue();
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.Paid, ServiceRequestStatus.Cancelled).Should().BeFalse();
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.Draft, ServiceRequestStatus.Paid).Should().BeFalse();
        ServiceRequestStateMachine.CanTransition(ServiceRequestStatus.Completed, ServiceRequestStatus.InProgress).Should().BeFalse();
        ServiceRequestStateMachine.IsTerminal(ServiceRequestStatus.Rejected).Should().BeTrue();
    }
}
