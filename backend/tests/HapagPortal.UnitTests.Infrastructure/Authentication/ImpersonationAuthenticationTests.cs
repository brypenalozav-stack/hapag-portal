namespace HapagPortal.UnitTests.Infrastructure.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using HapagPortal.Application.Common.Interfaces;
using HapagPortal.Domain.Constants;
using HapagPortal.Domain.Entities;
using HapagPortal.Infrastructure.Authentication;
using HapagPortal.Infrastructure.Persistence;
using HapagPortal.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// «Vista como cliente» (M8-08) en la infraestructura: el token marca la sesión y al actor, vence con la sesión, el
/// usuario actual expone ambos y los cambios guardados durante la sesión quedan con la identidad del actor interno.
/// </summary>
public sealed class ImpersonationAuthenticationTests
{
    private static JwtTokenService TokenService()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Jwt:Secret"].Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly1234567890!");
        configuration["Jwt:Issuer"].Returns("TestIssuer");
        configuration["Jwt:Audience"].Returns("TestAudience");
        return new JwtTokenService(configuration, Substitute.For<ILogger<JwtTokenService>>());
    }

    [Fact]
    public void ImpersonationToken_ShouldCarryTheSubjectTheActorAndTheSessionExpiry()
    {
        var subject = new User
        {
            Username = "cliente@org.cl", Email = "cliente@org.cl", PasswordHash = "x", UserType = UserTypes.Client, Country = "CL",
            ClientId = Guid.NewGuid()
        };
        var session = new ImpersonationSession
        {
            ActorUserId = Guid.NewGuid(), ActorEmail = "admin@hapag-lloyd.cl", SubjectUserId = subject.Id, SubjectEmail = subject.Email,
            OrganizationId = subject.ClientId!.Value, OrganizationName = "Org", Reason = "x", Status = ImpersonationStatus.Active,
            StartedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            TokenService().GenerateImpersonationToken(subject, [RoleCodes.OrgAdmin], [AccessPermissions.OperateShipments], session));

        token.Subject.Should().Be(subject.Id.ToString());
        token.Claims.Single(c => c.Type == ImpersonationClaims.SessionId).Value.Should().Be(session.Id.ToString());
        token.Claims.Single(c => c.Type == ImpersonationClaims.ActorUserId).Value.Should().Be(session.ActorUserId.ToString());
        token.Claims.Single(c => c.Type == ImpersonationClaims.ActorEmail).Value.Should().Be("admin@hapag-lloyd.cl");
        token.Claims.Should().Contain(c => c.Type == "permission" && c.Value == AccessPermissions.OperateShipments);
        token.ValidTo.Should().BeCloseTo(session.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    private static ICurrentUserService CurrentUser(params Claim[] claims)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) });
        return new CurrentUserService(accessor);
    }

    [Fact]
    public void CurrentUser_ShouldExposeTheImpersonationSession()
    {
        var sessionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var impersonating = CurrentUser(
            new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()),
            new Claim(ImpersonationClaims.SessionId, sessionId.ToString()),
            new Claim(ImpersonationClaims.ActorUserId, actorId.ToString()));
        var regular = CurrentUser(new Claim(JwtRegisteredClaimNames.Sub, subjectId.ToString()));

        impersonating.UserId.Should().Be(subjectId);
        impersonating.ImpersonationSessionId.Should().Be(sessionId);
        impersonating.ImpersonatorUserId.Should().Be(actorId);
        impersonating.IsImpersonating.Should().BeTrue();
        regular.IsImpersonating.Should().BeFalse();
        regular.ImpersonatorUserId.Should().BeNull();
    }

    [Fact]
    public async Task Interceptor_ShouldStampChangesWithTheActorWhileImpersonating()
    {
        var actorId = Guid.NewGuid();
        var currentUser = CurrentUser(
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(ImpersonationClaims.SessionId, Guid.NewGuid().ToString()),
            new Claim(ImpersonationClaims.ActorUserId, actorId.ToString()));
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(currentUser))
            .Options;
        await using var context = new ApplicationDbContext(options);

        var notification = new Notification { Type = NotificationTypes.PaymentConfirmed, Title = "t", Body = "b" };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        notification.CreatedBy.Should().Be(actorId.ToString());
    }

    [Fact]
    public async Task ContactListChanges_ShouldBeAppendOnly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntityInterceptor(Substitute.For<ICurrentUserService>()))
            .Options;
        await using var context = new ApplicationDbContext(options);
        var change = new ContactListChange
        {
            OrganizationId = Guid.NewGuid(), ReportType = ContactReportTypes.Invoices, NewEmails = "a@b.cl",
            Status = ContactListChangeStatus.Propagated, ChangedBy = "x", ChangedAt = DateTime.UtcNow
        };
        context.ContactListChanges.Add(change);
        await context.SaveChangesAsync();

        change.NewEmails = "tampered@b.cl";
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
