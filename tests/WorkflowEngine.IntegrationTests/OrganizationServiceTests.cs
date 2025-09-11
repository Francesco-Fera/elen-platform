using Microsoft.EntityFrameworkCore;
using Moq;
using WorkflowEngine.Application.DTOs.Organization;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services.Auth;

namespace WorkflowEngine.IntegrationTests;

public class OrganizationServiceTests
{
    private WorkflowEngineDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowEngineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new WorkflowEngineDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private Mock<ICurrentUserService> CreateMockCurrentUserService(
        Guid? userId = null,
        Guid? orgId = null,
        string? email = null,
        OrganizationRole? role = null)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.OrganizationId).Returns(orgId);
        mock.Setup(x => x.Email).Returns(email);
        mock.Setup(x => x.OrganizationRole).Returns(role);
        return mock;
    }

    [Fact]
    public async Task InviteMemberAsync_ShouldCreateInvite_WhenValidRequest()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true,
            MaxUsers = 10
        };
        context.Organizations.Add(organization);

        var inviterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            FirstName = "Admin",
            IsActive = true
        };
        context.Users.Add(inviterUser);

        var membership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = inviterUser.Id,
            Role = OrganizationRole.Admin,
            IsActive = true
        };
        context.OrganizationMembers.Add(membership);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            inviterUser.Id, organization.Id, "admin@test.com", OrganizationRole.Admin);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        var request = new InviteMemberRequest
        {
            Email = "newuser@test.com",
            Role = OrganizationRole.Member
        };

        // Act
        var result = await service.InviteMemberAsync(request);

        // Assert
        Assert.True(result);

        var invite = await context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Email == request.Email);
        Assert.NotNull(invite);
        Assert.Equal(organization.Id, invite.OrganizationId);
        Assert.Equal(request.Role, invite.Role);
        Assert.Equal(InviteStatus.Pending, invite.Status);
        Assert.True(invite.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task InviteMemberAsync_ShouldReturnFalse_WhenUserAlreadyMember()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true,
            MaxUsers = 10
        };
        context.Organizations.Add(organization);

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            IsActive = true
        };
        context.Users.Add(existingUser);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            IsActive = true
        };
        context.Users.Add(adminUser);

        // Existing membership
        var existingMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = existingUser.Id,
            Role = OrganizationRole.Member,
            IsActive = true
        };
        context.OrganizationMembers.Add(existingMembership);

        var adminMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = adminUser.Id,
            Role = OrganizationRole.Admin,
            IsActive = true
        };
        context.OrganizationMembers.Add(adminMembership);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            adminUser.Id, organization.Id, "admin@test.com", OrganizationRole.Admin);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        var request = new InviteMemberRequest
        {
            Email = "existing@test.com",
            Role = OrganizationRole.Member
        };

        // Act
        var result = await service.InviteMemberAsync(request);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldCreateMembership_WhenValidInvite()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true,
            MaxUsers = 10
        };
        context.Organizations.Add(organization);

        var invitedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "invited@test.com",
            IsActive = true
        };
        context.Users.Add(invitedUser);

        var inviterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            IsActive = true
        };
        context.Users.Add(inviterUser);

        var invite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = "invited@test.com",
            Role = OrganizationRole.Member,
            InviteToken = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedBy = inviterUser.Id
        };
        context.OrganizationInvites.Add(invite);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            invitedUser.Id, null, "invited@test.com", null);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.AcceptInviteAsync(invite.InviteToken);

        // Assert
        Assert.True(result);

        // Check membership was created
        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == invitedUser.Id &&
                                     m.OrganizationId == organization.Id);
        Assert.NotNull(membership);
        Assert.Equal(OrganizationRole.Member, membership.Role);
        Assert.True(membership.IsActive);

        // Check invite was updated
        var updatedInvite = await context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Id == invite.Id);
        Assert.NotNull(updatedInvite);
        Assert.Equal(InviteStatus.Accepted, updatedInvite.Status);
        Assert.Equal(invitedUser.Id, updatedInvite.AcceptedBy);
    }

    [Fact]
    public async Task AcceptInviteAsync_ShouldReturnFalse_WhenEmailMismatch()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true,
            MaxUsers = 10
        };
        context.Organizations.Add(organization);

        var wrongUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "wrong@test.com",
            IsActive = true
        };
        context.Users.Add(wrongUser);

        var invite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = "invited@test.com", // Different email
            Role = OrganizationRole.Member,
            InviteToken = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedBy = Guid.NewGuid()
        };
        context.OrganizationInvites.Add(invite);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            wrongUser.Id, null, "wrong@test.com", null);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.AcceptInviteAsync(invite.InviteToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldDeactivateMembership_WhenValidRequest()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true,
            MaxUsers = 10
        };
        context.Organizations.Add(organization);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            IsActive = true
        };
        context.Users.Add(adminUser);

        var memberUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "member@test.com",
            IsActive = true
        };
        context.Users.Add(memberUser);

        var adminMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = adminUser.Id,
            Role = OrganizationRole.Admin,
            IsActive = true
        };
        context.OrganizationMembers.Add(adminMembership);

        var memberMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = memberUser.Id,
            Role = OrganizationRole.Member,
            IsActive = true
        };
        context.OrganizationMembers.Add(memberMembership);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            adminUser.Id, organization.Id, "admin@test.com", OrganizationRole.Admin);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.RemoveMemberAsync(memberUser.Id);

        // Assert
        Assert.True(result);

        var updatedMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == memberUser.Id &&
                                     m.OrganizationId == organization.Id);
        Assert.NotNull(updatedMembership);
        Assert.False(updatedMembership.IsActive);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldReturnFalse_WhenRemovingSelf()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization { Id = Guid.NewGuid() };
        context.Organizations.Add(organization);

        var adminUser = new User { Id = Guid.NewGuid(), Email = "admin@test.com" };
        context.Users.Add(adminUser);

        var adminMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = adminUser.Id,
            Role = OrganizationRole.Admin,
            IsActive = true
        };
        context.OrganizationMembers.Add(adminMembership);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            adminUser.Id, organization.Id, "admin@test.com", OrganizationRole.Admin);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.RemoveMemberAsync(adminUser.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_ShouldUpdateRole_WhenValidRequest()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true
        };
        context.Organizations.Add(organization);

        var ownerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            IsActive = true
        };
        context.Users.Add(ownerUser);

        var memberUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "member@test.com",
            IsActive = true
        };
        context.Users.Add(memberUser);

        var ownerMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = ownerUser.Id,
            Role = OrganizationRole.Owner,
            IsActive = true
        };
        context.OrganizationMembers.Add(ownerMembership);

        var memberMembership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = memberUser.Id,
            Role = OrganizationRole.Member,
            IsActive = true
        };
        context.OrganizationMembers.Add(memberMembership);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            ownerUser.Id, organization.Id, "owner@test.com", OrganizationRole.Owner);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.UpdateMemberRoleAsync(memberUser.Id, OrganizationRole.Admin);

        // Assert
        Assert.True(result);

        var updatedMembership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == memberUser.Id &&
                                     m.OrganizationId == organization.Id);
        Assert.NotNull(updatedMembership);
        Assert.Equal(OrganizationRole.Admin, updatedMembership.Role);
    }

    [Fact]
    public async Task GetOrganizationMembersAsync_ShouldReturnMembers_WhenCalled()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true
        };
        context.Organizations.Add(organization);

        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@test.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };
        context.Users.Add(user1);

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@test.com",
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = true
        };
        context.Users.Add(user2);

        var membership1 = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = user1.Id,
            Role = OrganizationRole.Owner,
            IsActive = true
        };
        context.OrganizationMembers.Add(membership1);

        var membership2 = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = user2.Id,
            Role = OrganizationRole.Member,
            IsActive = true
        };
        context.OrganizationMembers.Add(membership2);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            user1.Id, organization.Id, "user1@test.com", OrganizationRole.Owner);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.GetOrganizationMembersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var owner = result.First(m => m.Email == "user1@test.com");
        Assert.Equal("Owner", owner.Role);
        Assert.Equal("John", owner.FirstName);

        var member = result.First(m => m.Email == "user2@test.com");
        Assert.Equal("Member", member.Role);
        Assert.Equal("Jane", member.FirstName);
    }

    [Fact]
    public async Task GetPendingInvitesAsync_ShouldReturnPendingInvites_WhenCalled()
    {
        // Arrange
        using var context = CreateDbContext();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            IsActive = true
        };
        context.Organizations.Add(organization);

        var inviterUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            IsActive = true
        };
        context.Users.Add(inviterUser);

        var pendingInvite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = "pending@test.com",
            Role = OrganizationRole.Member,
            InviteToken = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedBy = inviterUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.OrganizationInvites.Add(pendingInvite);

        var acceptedInvite = new OrganizationInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = "accepted@test.com",
            Role = OrganizationRole.Member,
            InviteToken = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Accepted,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedBy = inviterUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.OrganizationInvites.Add(acceptedInvite);
        await context.SaveChangesAsync();

        var mockCurrentUser = CreateMockCurrentUserService(
            inviterUser.Id, organization.Id, "admin@test.com", OrganizationRole.Admin);

        var service = new OrganizationService(context, mockCurrentUser.Object);

        // Act
        var result = await service.GetPendingInvitesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only pending invite should be returned

        var invite = result.First();
        Assert.Equal("pending@test.com", invite.Email);
        Assert.Equal("Pending", invite.Status);
        Assert.Equal("Member", invite.Role);
        Assert.Equal("admin@test.com", invite.InvitedByEmail);
    }
}