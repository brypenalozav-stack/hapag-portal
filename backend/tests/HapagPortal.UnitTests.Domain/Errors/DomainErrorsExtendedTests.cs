using FluentAssertions;
using HapagPortal.Domain.Errors;

namespace HapagPortal.UnitTests.Domain.Errors;

public sealed class DomainErrorsExtendedTests
{
    // --- Client ---

    [Fact]
    public void Client_Inactive_ShouldHaveCorrectCodeAndMessage()
    {
        var error = DomainErrors.Client.Inactive;
        error.Code.Should().Be("Client.Inactive");
        error.Message.Should().Contain("inactive");
    }

    [Fact]
    public void Client_EmailNotConfirmed_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Client.EmailNotConfirmed;
        error.Code.Should().Be("Client.EmailNotConfirmed");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Client_AlreadyExists_ShouldContainTaxId()
    {
        var error = DomainErrors.Client.AlreadyExists("99999999-K");
        error.Code.Should().Be("Client.AlreadyExists");
        error.Message.Should().Contain("99999999-K");
    }

    // --- BillOfLading ---

    [Fact]
    public void BillOfLading_NotFoundByNumber_ShouldContainBlNumber()
    {
        var error = DomainErrors.BillOfLading.NotFoundByNumber("BL-999");
        error.Code.Should().Be("BillOfLading.NotFound");
        error.Message.Should().Contain("BL-999");
    }

    [Fact]
    public void BillOfLading_HasPayments_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.BillOfLading.HasPayments;
        error.Code.Should().Be("BillOfLading.HasPayments");
        error.Message.Should().NotBeEmpty();
    }

    // --- Payment ---

    [Fact]
    public void Payment_AlreadyCancelled_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Payment.AlreadyCancelled;
        error.Code.Should().Be("Payment.AlreadyCancelled");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Payment_InvalidStatus_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Payment.InvalidStatus;
        error.Code.Should().Be("Payment.InvalidStatus");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void Payment_InvalidAmount_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Payment.InvalidAmount;
        error.Code.Should().Be("Payment.InvalidAmount");
        error.Message.Should().NotBeEmpty();
    }

    // --- LocalCharge ---

    [Fact]
    public void LocalCharge_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.LocalCharge.NotFound(id);
        error.Code.Should().Be("LocalCharge.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void LocalCharge_AlreadyPaid_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.LocalCharge.AlreadyPaid;
        error.Code.Should().Be("LocalCharge.AlreadyPaid");
        error.Message.Should().NotBeEmpty();
    }

    // --- DemurrageCharge ---

    [Fact]
    public void DemurrageCharge_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.DemurrageCharge.NotFound(id);
        error.Code.Should().Be("DemurrageCharge.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void DemurrageCharge_AlreadyExempt_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.DemurrageCharge.AlreadyExempt;
        error.Code.Should().Be("DemurrageCharge.AlreadyExempt");
        error.Message.Should().NotBeEmpty();
    }

    // --- User ---

    [Fact]
    public void User_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.User.NotFound(id);
        error.Code.Should().Be("User.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void User_NotFoundByEmail_ShouldContainEmail()
    {
        var error = DomainErrors.User.NotFoundByEmail("test@example.com");
        error.Code.Should().Be("User.NotFound");
        error.Message.Should().Contain("test@example.com");
    }

    [Fact]
    public void User_InvalidCredentials_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.User.InvalidCredentials;
        error.Code.Should().Be("User.InvalidCredentials");
        error.Message.Should().NotBeEmpty();
    }

    [Fact]
    public void User_Inactive_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.User.Inactive;
        error.Code.Should().Be("User.Inactive");
        error.Message.Should().NotBeEmpty();
    }

    // --- ServiceOrder ---

    [Fact]
    public void ServiceOrder_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.ServiceOrder.NotFound(id);
        error.Code.Should().Be("ServiceOrder.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    // --- CreditClient ---

    [Fact]
    public void CreditClient_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.CreditClient.NotFound(id);
        error.Code.Should().Be("CreditClient.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void CreditClient_LimitExceeded_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.CreditClient.LimitExceeded;
        error.Code.Should().Be("CreditClient.LimitExceeded");
        error.Message.Should().NotBeEmpty();
    }

    // --- DemurrageExemption ---

    [Fact]
    public void DemurrageExemption_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.DemurrageExemption.NotFound(id);
        error.Code.Should().Be("DemurrageExemption.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    // --- FAQ ---

    [Fact]
    public void FAQ_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.FAQ.NotFound(id);
        error.Code.Should().Be("FAQ.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    // --- TaxConfiguration ---

    [Fact]
    public void TaxConfiguration_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.TaxConfiguration.NotFound(id);
        error.Code.Should().Be("TaxConfiguration.NotFound");
        error.Message.Should().Contain(id.ToString());
    }

    // --- WarehouseChange ---

    [Fact]
    public void WarehouseChange_NotFound_ShouldContainId()
    {
        var id = Guid.NewGuid();
        var error = DomainErrors.WarehouseChange.NotFound(id);
        error.Code.Should().Be("WarehouseChange.NotFound");
        error.Message.Should().Contain(id.ToString());
    }
}
