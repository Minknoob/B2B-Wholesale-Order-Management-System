using B2B.AgentPortal.Data;
using B2B.AgentPortal.Models;
using B2B.AgentPortal.Services;
using B2B.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace B2B.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task Login_ThreeWrongPasswords_LocksAccountAndRejectsCorrectPassword()
    {
        var options = new DbContextOptionsBuilder<B2BDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var setup = new B2BDbContext(options))
        {
            setup.Agents.Add(new Agent
            {
                AgentID = 1,
                AgentName = "Test Agent",
                Address = "Ho Chi Minh City",
                Phone = "0901234567"
            });

            setup.Users.Add(new User
            {
                UserID = 1,
                UserName = "agent_test",
                Email = "agent_test@example.com",
                PasswordHash = PasswordHasher.HashPassword("Correct@123"),
                Role = "Agent",
                AgentID = 1,
                IsLocked = false,
                FailedLoginCount = 0
            });

            await setup.SaveChangesAsync();
        }

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            await using (var db = new B2BDbContext(options))
            {
                var service = new AgentAuthenticationService(
                    db,
                    NullLogger<AgentAuthenticationService>.Instance);

                var result = await service.AuthenticateAgentAsync(
                    "agent_test", "WrongPassword");

                Assert.False(result.Succeeded);
            }

            await using var verify = new B2BDbContext(options);
            var user = await verify.Users.SingleAsync();

            Assert.Equal(attempt, user.FailedLoginCount);
            Assert.Equal(attempt == 3, user.IsLocked);
        }

        await using (var db = new B2BDbContext(options))
        {
            var service = new AgentAuthenticationService(
                db,
                NullLogger<AgentAuthenticationService>.Instance);

            var result = await service.AuthenticateAgentAsync(
                "agent_test", "Correct@123");

            Assert.False(result.Succeeded);
        }

        await using var finalCheck = new B2BDbContext(options);
        var lockedUser = await finalCheck.Users.SingleAsync();

        Assert.True(lockedUser.IsLocked);
        Assert.Equal(3, lockedUser.FailedLoginCount);
    }
}