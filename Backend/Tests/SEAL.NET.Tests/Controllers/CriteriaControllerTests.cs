using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Controllers;
using SEAL.NET.Data;
using SEAL.NET.DTOs.Criteria;
using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Implementations;

namespace SEAL.NET.Tests.Controllers;

public class CriteriaControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpdateCriteria_WhenNameDuplicatesAnotherCriteriaInRound_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var roundId = Guid.NewGuid();
        var innovationCriteria = new Criteria
        {
            CriteriaId = Guid.NewGuid(),
            RoundId = roundId,
            CriteriaName = "Innovation",
            MaxScore = 10,
            Weight = 50
        };
        var technicalCriteria = new Criteria
        {
            CriteriaId = Guid.NewGuid(),
            RoundId = roundId,
            CriteriaName = "Technical",
            MaxScore = 10,
            Weight = 50
        };

        context.Add(new Round { RoundId = roundId, RoundName = "Round 1" });
        context.Criteria.AddRange(innovationCriteria, technicalCriteria);
        await context.SaveChangesAsync();

        var controller = new CriteriaController(new CriteriaService(context));
        var result = await controller.UpdateCriteria(roundId, technicalCriteria.CriteriaId, new UpdateCriteriaRequest
        {
            CriteriaName = "Innovation",
            MaxScore = 10,
            Weight = 50
        });

        Assert.IsType<BadRequestObjectResult>(result);

        var unchanged = await context.Criteria.SingleAsync(c => c.CriteriaId == technicalCriteria.CriteriaId);
        Assert.Equal("Technical", unchanged.CriteriaName);
    }
}
