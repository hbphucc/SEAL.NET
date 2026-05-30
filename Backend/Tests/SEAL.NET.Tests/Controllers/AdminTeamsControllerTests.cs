using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SEAL.NET.Controllers;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Team;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.Tests.Controllers;

public class AdminTeamsControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(context);

        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static ApplicationUser CreateLeader(string email = "leader@example.com")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            FullName = "Team Leader",
            IsApproved = true
        };
    }

    private static Team CreateTeam(Guid leaderId, TeamStatus status = TeamStatus.Approved)
    {
        return new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = $"Team {Guid.NewGuid():N}",
            LeaderId = leaderId,
            Status = status
        };
    }

    private static async Task SeedTeamLeaderRoleAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ApplicationUser leader,
        bool assignRole = true)
    {
        context.Roles.Add(new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = "TeamLeader",
            NormalizedName = "TEAMLEADER"
        });
        context.Users.Add(leader);
        await context.SaveChangesAsync();

        if (assignRole)
            await userManager.AddToRoleAsync(leader, "TeamLeader");
    }

    private static void AddApprovalMembers(ApplicationDbContext context, Team team)
    {
        context.TeamMembers.AddRange(
            new TeamMember { TeamId = team.TeamId, UserId = team.LeaderId },
            new TeamMember { TeamId = team.TeamId, UserId = Guid.NewGuid() },
            new TeamMember { TeamId = team.TeamId, UserId = Guid.NewGuid() });
    }

    private static string? GetMessage(IActionResult result)
    {
        var value = result switch
        {
            ObjectResult objectResult => objectResult.Value,
            _ => null
        };

        return value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
    }

    [Fact]
    public async Task ApproveTeam_AddsTeamLeaderRoleAndUpdatesSecurityStamp()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id, TeamStatus.Pending);

        await SeedTeamLeaderRoleAsync(context, userManager, leader, assignRole: false);
        context.Teams.Add(team);
        AddApprovalMembers(context, team);
        await context.SaveChangesAsync();
        var beforeStamp = await userManager.GetSecurityStampAsync(leader);

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.ApproveTeam(team.TeamId);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.NotEqual(beforeStamp, await userManager.GetSecurityStampAsync(leader));
    }

    [Fact]
    public async Task RejectTeam_WhenLeaderHasNoApprovedTeams_RemovesTeamLeaderRoleAndUpdatesSecurityStamp()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id, TeamStatus.Pending);

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var beforeStamp = await userManager.GetSecurityStampAsync(leader);

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.RejectTeam(team.TeamId);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.NotEqual(beforeStamp, await userManager.GetSecurityStampAsync(leader));
        Assert.Equal(TeamStatus.Rejected, team.Status);
        Assert.Null(team.EliminationReason);
        Assert.Null(team.EliminatedAt);
    }

    [Fact]
    public async Task RejectTeam_WhenPending_SetsRejectedStatusWithoutEliminationFields()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id, TeamStatus.Pending);

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.RejectTeam(team.TeamId, new TeamDecisionRequest { Reason = "Missing eligibility proof" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(TeamStatus.Rejected, team.Status);
        Assert.Equal("Missing eligibility proof", team.StatusReason);
        Assert.Null(team.EliminationReason);
        Assert.Null(team.EliminatedAt);
        Assert.Contains(context.AuditLogs, log =>
            log.Action == "TeamRejected" &&
            log.EntityId == team.TeamId &&
            log.Details == "Missing eligibility proof");
    }

    [Fact]
    public async Task RejectTeam_WhenNotPending_ReturnsBadRequestAndKeepsStatus()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id, TeamStatus.Approved);

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.RejectTeam(team.TeamId);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only pending teams can be rejected.", GetMessage(result));
        Assert.Equal(TeamStatus.Approved, team.Status);
        Assert.Null(team.StatusReason);
        Assert.Null(team.EliminationReason);
        Assert.Null(team.EliminatedAt);
        Assert.Empty(context.AuditLogs);
    }

    [Fact]
    public async Task EliminateTeam_WhenLeaderHasNoApprovedTeams_RemovesTeamLeaderRoleAndUpdatesSecurityStamp()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var category = new Category { CategoryId = Guid.NewGuid(), CategoryName = "Web" };
        var team = CreateTeam(leader.Id);
        team.CategoryId = category.CategoryId;

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.AddRange(category, team);
        await context.SaveChangesAsync();
        var beforeStamp = await userManager.GetSecurityStampAsync(leader);

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.EliminateTeam(team.TeamId, new EliminateTeamRequest { Reason = "Rule violation" });

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.NotEqual(beforeStamp, await userManager.GetSecurityStampAsync(leader));
    }

    [Theory]
    [InlineData(TeamStatus.Pending)]
    [InlineData(TeamStatus.Rejected)]
    public async Task EliminateTeam_WhenTeamIsNotParticipating_ReturnsBadRequestAndKeepsStatus(TeamStatus status)
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var category = new Category { CategoryId = Guid.NewGuid(), CategoryName = "Web" };
        var team = CreateTeam(leader.Id, status);
        team.CategoryId = category.CategoryId;

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.AddRange(category, team);
        await context.SaveChangesAsync();

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.EliminateTeam(team.TeamId, new EliminateTeamRequest { Reason = "Rule violation" });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only approved or active teams can be eliminated.", GetMessage(result));
        Assert.Equal(status, team.Status);
        Assert.Null(team.EliminationReason);
        Assert.Null(team.EliminatedAt);
    }

    [Fact]
    public async Task DeleteTeam_WhenLeaderHasNoApprovedTeams_RemovesTeamLeaderRoleAndUpdatesSecurityStamp()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id);

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        var beforeStamp = await userManager.GetSecurityStampAsync(leader);

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.DeleteTeam(team.TeamId);

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.NotEqual(beforeStamp, await userManager.GetSecurityStampAsync(leader));
    }

    [Theory]
    [InlineData("reject")]
    [InlineData("eliminate")]
    [InlineData("delete")]
    public async Task TeamRemovalActions_WhenLeaderHasAnotherApprovedTeam_KeepsTeamLeaderRoleAndSecurityStamp(string action)
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var category = new Category { CategoryId = Guid.NewGuid(), CategoryName = "Web" };
        var targetTeam = CreateTeam(leader.Id, action == "reject" ? TeamStatus.Pending : TeamStatus.Approved);
        var stillApprovedTeam = CreateTeam(leader.Id);
        targetTeam.CategoryId = category.CategoryId;
        stillApprovedTeam.CategoryId = category.CategoryId;

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.AddRange(category, targetTeam, stillApprovedTeam);
        await context.SaveChangesAsync();
        var beforeStamp = await userManager.GetSecurityStampAsync(leader);

        var controller = new AdminTeamsController(context, userManager);
        IActionResult result = action switch
        {
            "reject" => await controller.RejectTeam(targetTeam.TeamId),
            "eliminate" => await controller.EliminateTeam(targetTeam.TeamId, new EliminateTeamRequest { Reason = "Rule violation" }),
            "delete" => await controller.DeleteTeam(targetTeam.TeamId),
            _ => throw new InvalidOperationException("Unknown test action.")
        };

        Assert.IsType<OkObjectResult>(result);
        Assert.True(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.Equal(beforeStamp, await userManager.GetSecurityStampAsync(leader));
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamHasSubmissions_ReturnsBadRequestAndKeepsTeamLeaderRole()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateLeader();
        var team = CreateTeam(leader.Id);
        var submission = new Submission
        {
            SubmissionId = Guid.NewGuid(),
            TeamId = team.TeamId,
            RoundId = Guid.NewGuid()
        };

        await SeedTeamLeaderRoleAsync(context, userManager, leader);
        context.AddRange(team, submission);
        await context.SaveChangesAsync();

        var controller = new AdminTeamsController(context, userManager);
        var result = await controller.DeleteTeam(team.TeamId);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await userManager.IsInRoleAsync(leader, "TeamLeader"));
        Assert.True(await context.Teams.AnyAsync(t => t.TeamId == team.TeamId));
    }
}
