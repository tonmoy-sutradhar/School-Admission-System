using FluentAssertions;
using Roles.Domain.Entities;

namespace UnitTests.Domain;

public class RoleTests
{
    [Fact]
    public void AddPermission_Should_Be_Idempotent()
    {
        var role = Role.Create("Editor", "Can edit");
        var permission = Permission.Create("users.view", "View users");

        role.AddPermission(permission);
        role.AddPermission(permission);

        role.RolePermissions.Should().ContainSingle();
    }

    [Fact]
    public void Update_System_Role_Should_Throw()
    {
        var role = Role.Create("Admin", "System", isSystem: true);
        var act = () => role.Update("Hack", "nope");
        act.Should().Throw<InvalidOperationException>();
    }
}