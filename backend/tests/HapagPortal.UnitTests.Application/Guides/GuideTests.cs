namespace HapagPortal.UnitTests.Application.Guides;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Guides;
using HapagPortal.Domain.Constants;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

/// <summary>
/// Modo guía (M1-27): guías administrables por flujo, ofrecidas según la audiencia, con estado por usuario que se puede
/// desactivar y que vuelve a ofrecerse cuando cambian los pasos.
/// </summary>
public sealed class GuideTests
{
    private readonly MockApplicationDbContext _db = new();
    private readonly ICurrentUserService _admin = Substitute.For<ICurrentUserService>();

    public GuideTests()
    {
        _admin.UserId.Returns(Guid.NewGuid());
        _admin.Email.Returns("admin@hapag-lloyd.cl");
    }

    private static GuideStepDto Step(int order, string key) =>
        new(order, "/cart", key, $"Paso {order}", $"Step {order}", $"Texto {order}", $"Text {order}");

    private async Task<GuideDefinitionDto> CreateAsync(string code, string audience, params GuideStepDto[] steps)
    {
        var result = await new CreateGuideCommandHandler(_db, _admin).Handle(new CreateGuideCommand(
            code, "Guía", "Guide", null, null, "/cart", audience, true, 10, steps), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private ICurrentUserService ClientUser()
    {
        var organization = AccessTestData.AddOrganization(_db);
        var user = AccessTestData.AddMember(_db, organization);
        return AccessTestData.CurrentUser(user);
    }

    [Fact]
    public async Task Guides_ShouldBeOfferedByAudience_WithOrderedSteps()
    {
        await CreateAsync("cart-checkout", GuideAudiences.Client, Step(2, "cart.checkout"), Step(1, "cart.items"));
        await CreateAsync("internal-queue", GuideAudiences.Internal, Step(1, "queue.list"));
        await CreateAsync("everyone", GuideAudiences.All, Step(1, "menu.help"));

        var client = await new GetGuidesQueryHandler(_db, ClientUser()).Handle(new GetGuidesQuery(), CancellationToken.None);

        client.Value.Select(g => g.Code).Should().BeEquivalentTo(["cart-checkout", "everyone"]);
        var cart = client.Value.Single(g => g.Code == "cart-checkout");
        cart.Steps.Select(s => s.ElementKey).Should().Equal("cart.items", "cart.checkout");
        cart.Steps.Select(s => s.Order).Should().Equal(1, 2);
        cart.Status.Should().BeNull();
    }

    [Fact]
    public async Task State_ShouldBeKeptPerUser_ResetAndOfferedAgainWhenStepsChange()
    {
        var guide = await CreateAsync("cart-checkout", GuideAudiences.Client, Step(1, "cart.items"));
        var user = ClientUser();
        var setState = new SetGuideStateCommandHandler(_db, user);

        var dismissed = await setState.Handle(new SetGuideStateCommand("cart-checkout", GuideStates.Dismissed, 1), CancellationToken.None);
        dismissed.Value.Status.Should().Be(GuideStates.Dismissed);

        var completed = await setState.Handle(new SetGuideStateCommand("cart-checkout", GuideStates.Completed, 1), CancellationToken.None);
        completed.Value.Status.Should().Be(GuideStates.Completed);
        _db.UserGuideStateList.Should().ContainSingle();

        // Otro usuario no hereda el estado.
        var other = await new GetGuideQueryHandler(_db, ClientUser()).Handle(new GetGuideQuery("cart-checkout"), CancellationToken.None);
        other.Value.Status.Should().BeNull();

        // Cambiar los pasos sube la versión y vuelve a ofrecer la guía.
        var updated = await new UpdateGuideCommandHandler(_db, _admin).Handle(new UpdateGuideCommand(
            guide.Id, "Guía", "Guide", null, null, "/cart", GuideAudiences.Client, true, 10,
            [Step(1, "cart.items"), Step(2, "cart.checkout")]), CancellationToken.None);
        updated.Value.Version.Should().Be(2);
        (await new GetGuideQueryHandler(_db, user).Handle(new GetGuideQuery("cart-checkout"), CancellationToken.None)).Value.Status.Should().BeNull();

        var reset = await setState.Handle(new SetGuideStateCommand("cart-checkout", GuideStates.Reset), CancellationToken.None);
        reset.Value.Status.Should().BeNull();
        _db.UserGuideStateList.Should().BeEmpty();
    }

    [Fact]
    public async Task InactiveOrOtherAudienceGuides_ShouldNotBeFound()
    {
        await CreateAsync("internal-queue", GuideAudiences.Internal, Step(1, "queue.list"));

        var result = await new SetGuideStateCommandHandler(_db, ClientUser())
            .Handle(new SetGuideStateCommand("internal-queue", GuideStates.Completed), CancellationToken.None);

        result.Error.Code.Should().Be("Guide.NotFound");
    }

    [Fact]
    public async Task Maintainer_ShouldRejectDuplicatesAndLogChanges()
    {
        var guide = await CreateAsync("cart-checkout", GuideAudiences.Client, Step(1, "cart.items"));

        var duplicate = await new CreateGuideCommandHandler(_db, _admin).Handle(new CreateGuideCommand(
            "cart-checkout", "x", "x", null, null, "/cart", GuideAudiences.Client, true, 1, [Step(1, "cart.items")]), CancellationToken.None);
        await new DeleteGuideCommandHandler(_db, _admin).Handle(new DeleteGuideCommand(guide.Id), CancellationToken.None);
        var history = await new GetGuideHistoryQueryHandler(_db).Handle(new GetGuideHistoryQuery(guide.Id), CancellationToken.None);

        duplicate.Error.Code.Should().Be("Guide.AlreadyExists");
        history.Value.Select(h => h.Action).Should().BeEquivalentTo([MaintainerActions.Created, MaintainerActions.Deactivated]);
        history.Value.Single(h => h.Action == MaintainerActions.Created).Current!.Steps.Should().ContainSingle();
    }

    [Fact]
    public void Validator_ShouldRequireValidStepsAndKeys()
    {
        var validator = new CreateGuideCommandValidator();
        var valid = new CreateGuideCommand("cart-checkout", "Guía", "Guide", null, null, "/cart", GuideAudiences.Client, true, 1, [Step(1, "cart.items")]);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(valid with { Steps = [] }).IsValid.Should().BeFalse();
        validator.Validate(valid with { Steps = [Step(1, "Cart Items")] }).IsValid.Should().BeFalse();
        validator.Validate(valid with { Route = "cart" }).IsValid.Should().BeFalse();
        validator.Validate(valid with { Audience = "Everyone" }).IsValid.Should().BeFalse();
    }
}
