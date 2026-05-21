using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SEAL.NET.Data;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Implementations;

namespace SEAL.NET.Tests.Services;

public class RankingServiceIntegrationTests
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

    private static ApplicationUser CreateLeader(string email)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            FullName = email,
            IsApproved = true
        };
    }

    private static async Task SeedTeamLeaderRoleAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        params ApplicationUser[] leaders)
    {
        context.Roles.Add(new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = "TeamLeader",
            NormalizedName = "TEAMLEADER"
        });
        context.Users.AddRange(leaders);
        await context.SaveChangesAsync();

        foreach (var leader in leaders)
        {
            await userManager.AddToRoleAsync(leader, "TeamLeader");
        }
    }

    [Fact]
    public async Task AdvanceRoundAsync_WhenScoreMissing_DoesNotPublishRanking()
    {
        using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var round = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 1", RoundOrder = 1, MaxTeamsAdvancing = 1 };
        var nextRound = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 2", RoundOrder = 2, MaxTeamsAdvancing = 1 };
        var category = new Category { CategoryId = categoryId, EventId = eventId, CategoryName = "Web" };
        var team = new Team { TeamId = Guid.NewGuid(), TeamName = "Team A", CategoryId = categoryId, CurrentRoundId = round.RoundId, Status = TeamStatus.Approved };
        var criteria = new Criteria { CriteriaId = Guid.NewGuid(), CriteriaName = "Impact", RoundId = round.RoundId, MaxScore = 10, Weight = 1 };
        var submission = new Submission { SubmissionId = Guid.NewGuid(), TeamId = team.TeamId, RoundId = round.RoundId };
        var assignment = new JudgeAssignment { JudgeId = judgeId, RoundId = round.RoundId, CategoryId = categoryId };

        context.AddRange(round, nextRound, category, team, criteria, submission, assignment);
        await context.SaveChangesAsync();

        var service = new RankingService(context);
        var result = await service.AdvanceRoundAsync(round.RoundId);

        Assert.False(result.Success);
        Assert.Equal("Cannot advance round because scoring is not complete.", result.Message);
        Assert.False(round.IsRankingPublished);
        Assert.Equal(round.RoundId, team.CurrentRoundId);
    }

    [Fact]
    public async Task AdvanceRoundAsync_WhenScoresComplete_PublishesAndAdvancesTopTeam()
    {
        using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var round = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 1", RoundOrder = 1, MaxTeamsAdvancing = 1 };
        var nextRound = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 2", RoundOrder = 2, MaxTeamsAdvancing = 1 };
        var category = new Category { CategoryId = categoryId, EventId = eventId, CategoryName = "Web" };
        var winner = new Team { TeamId = Guid.NewGuid(), TeamName = "Winner", CategoryId = categoryId, CurrentRoundId = round.RoundId, Status = TeamStatus.Approved };
        var loser = new Team { TeamId = Guid.NewGuid(), TeamName = "Loser", CategoryId = categoryId, CurrentRoundId = round.RoundId, Status = TeamStatus.Approved };
        var criteria = new Criteria { CriteriaId = Guid.NewGuid(), CriteriaName = "Impact", RoundId = round.RoundId, MaxScore = 10, Weight = 1 };
        var winnerSubmission = new Submission { SubmissionId = Guid.NewGuid(), TeamId = winner.TeamId, RoundId = round.RoundId, SubmittedAt = DateTime.UtcNow.AddMinutes(-2) };
        var loserSubmission = new Submission { SubmissionId = Guid.NewGuid(), TeamId = loser.TeamId, RoundId = round.RoundId, SubmittedAt = DateTime.UtcNow.AddMinutes(-1) };
        var assignment = new JudgeAssignment { JudgeId = judgeId, RoundId = round.RoundId, CategoryId = categoryId };
        var winnerScore = new Score { SubmissionId = winnerSubmission.SubmissionId, JudgeId = judgeId, CriteriaId = criteria.CriteriaId, ScoreValue = 9 };
        var loserScore = new Score { SubmissionId = loserSubmission.SubmissionId, JudgeId = judgeId, CriteriaId = criteria.CriteriaId, ScoreValue = 5 };

        context.AddRange(round, nextRound, category, winner, loser, criteria, winnerSubmission, loserSubmission, assignment, winnerScore, loserScore);
        await context.SaveChangesAsync();

        var service = new RankingService(context);
        var result = await service.AdvanceRoundAsync(round.RoundId);

        Assert.True(result.Success);
        Assert.True(round.IsRankingPublished);
        Assert.Equal(nextRound.RoundId, winner.CurrentRoundId);
        Assert.Equal(TeamStatus.Eliminated, loser.Status);
        Assert.Equal("Eliminated after round ranking.", loser.EliminationReason);
    }

    [Fact]
    public async Task AdvanceRoundAsync_WhenLoserAutoEliminated_RemovesLoserLeaderRoleAndUpdatesSecurityStamp()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var eventId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var winnerLeader = CreateLeader("winner@example.com");
        var loserLeader = CreateLeader("loser@example.com");
        var round = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 1", RoundOrder = 1, MaxTeamsAdvancing = 1 };
        var nextRound = new Round { RoundId = Guid.NewGuid(), EventId = eventId, RoundName = "Round 2", RoundOrder = 2, MaxTeamsAdvancing = 1 };
        var category = new Category { CategoryId = categoryId, EventId = eventId, CategoryName = "Web" };
        var winner = new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = "Winner",
            LeaderId = winnerLeader.Id,
            CategoryId = categoryId,
            CurrentRoundId = round.RoundId,
            Status = TeamStatus.Approved
        };
        var loser = new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = "Loser",
            LeaderId = loserLeader.Id,
            CategoryId = categoryId,
            CurrentRoundId = round.RoundId,
            Status = TeamStatus.Approved
        };
        var criteria = new Criteria { CriteriaId = Guid.NewGuid(), CriteriaName = "Impact", RoundId = round.RoundId, MaxScore = 10, Weight = 1 };
        var winnerSubmission = new Submission { SubmissionId = Guid.NewGuid(), TeamId = winner.TeamId, RoundId = round.RoundId, SubmittedAt = DateTime.UtcNow.AddMinutes(-2) };
        var loserSubmission = new Submission { SubmissionId = Guid.NewGuid(), TeamId = loser.TeamId, RoundId = round.RoundId, SubmittedAt = DateTime.UtcNow.AddMinutes(-1) };
        var assignment = new JudgeAssignment { JudgeId = judgeId, RoundId = round.RoundId, CategoryId = categoryId };
        var winnerScore = new Score { SubmissionId = winnerSubmission.SubmissionId, JudgeId = judgeId, CriteriaId = criteria.CriteriaId, ScoreValue = 9 };
        var loserScore = new Score { SubmissionId = loserSubmission.SubmissionId, JudgeId = judgeId, CriteriaId = criteria.CriteriaId, ScoreValue = 5 };

        await SeedTeamLeaderRoleAsync(context, userManager, winnerLeader, loserLeader);
        context.AddRange(round, nextRound, category, winner, loser, criteria, winnerSubmission, loserSubmission, assignment, winnerScore, loserScore);
        await context.SaveChangesAsync();
        var loserStampBefore = await userManager.GetSecurityStampAsync(loserLeader);

        var service = new RankingService(context, userManager);
        var result = await service.AdvanceRoundAsync(round.RoundId);

        Assert.True(result.Success);
        Assert.True(await userManager.IsInRoleAsync(winnerLeader, "TeamLeader"));
        Assert.False(await userManager.IsInRoleAsync(loserLeader, "TeamLeader"));
        Assert.NotEqual(loserStampBefore, await userManager.GetSecurityStampAsync(loserLeader));
    }
}
