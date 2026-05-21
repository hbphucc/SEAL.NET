using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Controllers;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Score;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;

namespace SEAL.NET.Tests.Controllers;

public class ScoresControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ScoresController CreateController(ApplicationDbContext context, Guid judgeId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, judgeId.ToString()),
            new Claim(ClaimTypes.Role, "Judge")
        };

        return new ScoresController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            }
        };
    }

    [Fact]
    public async Task SubmitScore_WhenRankingPublished_ReturnsConflict()
    {
        using var context = CreateContext();
        var judgeId = Guid.NewGuid();
        var round = new Round
        {
            RoundId = Guid.NewGuid(),
            RoundName = "Round 1",
            IsRankingPublished = true
        };
        var category = new Category { CategoryId = Guid.NewGuid(), CategoryName = "Web" };
        var team = new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = "Team A",
            CategoryId = category.CategoryId,
            Category = category,
            Status = TeamStatus.Approved
        };
        var submission = new Submission
        {
            SubmissionId = Guid.NewGuid(),
            RoundId = round.RoundId,
            Round = round,
            TeamId = team.TeamId,
            Team = team
        };
        var criteria = new Criteria
        {
            CriteriaId = Guid.NewGuid(),
            CriteriaName = "Impact",
            RoundId = round.RoundId,
            Round = round,
            MaxScore = 10,
            Weight = 1
        };
        var assignment = new JudgeAssignment
        {
            JudgeId = judgeId,
            RoundId = round.RoundId,
            CategoryId = category.CategoryId
        };

        context.AddRange(round, category, team, submission, criteria, assignment);
        await context.SaveChangesAsync();

        var controller = CreateController(context, judgeId);
        var result = await controller.SubmitScore(new CreateScoreRequest
        {
            SubmissionId = submission.SubmissionId,
            CriteriaId = criteria.CriteriaId,
            ScoreValue = 8
        });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(context.Scores);
    }
}
