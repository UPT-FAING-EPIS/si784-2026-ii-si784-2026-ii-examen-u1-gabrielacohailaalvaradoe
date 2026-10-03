using Microsoft.AspNetCore.Identity;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Tests;

public sealed class DomainRuleTests
{
    [Fact]
    public void Password_Is_Stored_As_A_Verifiable_Hash()
    {
        var user = new User { Email = "secure@upt.test" };
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "SecurePass123!");
        Assert.NotEqual("SecurePass123!", user.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, "SecurePass123!"));
    }

    [Theory]
    [InlineData(Roles.Student)]
    [InlineData(Roles.Teacher)]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Administrator)]
    public void Required_Roles_Are_Recognized(string role) => Assert.Contains(role, Roles.Allowed);
}
