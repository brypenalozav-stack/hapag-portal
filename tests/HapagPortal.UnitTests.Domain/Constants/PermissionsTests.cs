using System.Reflection;
using FluentAssertions;
using HapagPortal.Domain.Constants;

namespace HapagPortal.UnitTests.Domain.Constants;

public sealed class PermissionsTests
{
    [Fact]
    public void AllPermissions_ShouldStartWithHapagPortal()
    {
        // Arrange
        var permissionValues = GetAllPermissionValues();

        // Assert
        permissionValues.Should().NotBeEmpty();
        permissionValues.Should().AllSatisfy(p => p.Should().StartWith("HapagPortal."));
    }

    [Fact]
    public void Clients_ShouldHaveReadCreateUpdate()
    {
        // Assert
        Permissions.Clients.Read.Should().Be("HapagPortal.Clients.Read");
        Permissions.Clients.Create.Should().Be("HapagPortal.Clients.Create");
        Permissions.Clients.Update.Should().Be("HapagPortal.Clients.Update");
    }

    [Fact]
    public void Payments_ShouldHaveReadCreateConfirmCancel()
    {
        // Assert
        Permissions.Payments.Read.Should().Be("HapagPortal.Payments.Read");
        Permissions.Payments.Create.Should().Be("HapagPortal.Payments.Create");
        Permissions.Payments.Confirm.Should().Be("HapagPortal.Payments.Confirm");
        Permissions.Payments.Cancel.Should().Be("HapagPortal.Payments.Cancel");
    }

    private static List<string> GetAllPermissionValues()
    {
        var values = new List<string>();
        var nestedTypes = typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var nestedType in nestedTypes)
        {
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = field.GetRawConstantValue() as string;
                if (value is not null)
                    values.Add(value);
            }
        }

        return values;
    }
}
