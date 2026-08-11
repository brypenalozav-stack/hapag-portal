namespace HapagPortal.UnitTests.Application.Configuration;

using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Application.Configuration.Secrets;
using HapagPortal.Domain.Constants;
using HapagPortal.UnitTests.Application.TestHelpers;
using NSubstitute;

public sealed class UpsertSecretCommandHandlerTests
{
    private readonly MockApplicationDbContext _dbContext = new();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ISecretProtector _protector = Substitute.For<ISecretProtector>();
    private readonly UpsertSecretCommandHandler _handler;

    public UpsertSecretCommandHandlerTests()
    {
        _protector.Protect(Arg.Any<string>()).Returns(ci => "ENC(" + ci.Arg<string>() + ")");
        _handler = new UpsertSecretCommandHandler(_dbContext, _currentUser, _protector);
    }

    [Fact]
    public async Task GlobalWithPermission_ShouldStoreEncryptedAndNotReturnValue()
    {
        _currentUser.HasPermission("config.global.manage").Returns(true);

        var command = new UpsertSecretCommand(
            ConfigurationScopes.Global, null, SecretTypes.CustomsPassword, "clave-aduana", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.SecretCredentialList.Should().ContainSingle();
        _dbContext.SecretCredentialList[0].EncryptedValue.Should().Be("ENC(clave-aduana)"); // cifrado
        _dbContext.SecretCredentialList[0].EncryptedValue.Should().NotContain("clave-aduana".ToUpperInvariant());
        // El DTO de respuesta no tiene propiedad de valor: solo metadatos.
        result.Value.Type.Should().Be(SecretTypes.CustomsPassword);
    }

    [Fact]
    public async Task ClientTryingGlobalScope_ShouldBeForbidden()
    {
        // Cliente sin permiso global.
        _currentUser.HasPermission("config.global.manage").Returns(false);

        var command = new UpsertSecretCommand(
            ConfigurationScopes.Global, null, SecretTypes.SiiKey, "x", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Forbidden");
        _dbContext.SecretCredentialList.Should().BeEmpty();
    }

    [Fact]
    public async Task ClientOnAnotherClientScope_ShouldBeForbidden()
    {
        var me = Guid.NewGuid();
        _currentUser.HasPermission("config.client.manage").Returns(true);
        _currentUser.ClientId.Returns(me);

        // Intenta escribir credencial de OTRO cliente.
        var command = new UpsertSecretCommand(
            ConfigurationScopes.Client, Guid.NewGuid(), SecretTypes.SiiKey, "x", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Error.Forbidden");
    }
}
