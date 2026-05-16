using Microsoft.EntityFrameworkCore;
using SEAL.NET.Models.Entities;
using SEAL.NET.Data;

namespace SEAL.NET.Tests.Data;

public class ApplicationDbContextTests
{
    [Fact]
    public void Model_DateTimeProperties_UseUtcConverters()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var startDate = context.Model
            .FindEntityType(typeof(Event))!
            .FindProperty(nameof(Event.StartDate))!;

        var submissionDeadline = context.Model
            .FindEntityType(typeof(Round))!
            .FindProperty(nameof(Round.SubmissionDeadline))!;

        Assert.NotNull(startDate.GetValueConverter());
        Assert.NotNull(submissionDeadline.GetValueConverter());
    }

    [Fact]
    public void Model_Submission_HasUniqueTeamRoundIndex()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var index = context.Model
            .FindEntityType(typeof(Submission))!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.Properties.Select(p => p.Name).SequenceEqual(new[]
                {
                    nameof(Submission.TeamId),
                    nameof(Submission.RoundId)
                }));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void Model_JudgeAssignment_RequiresRoundAndCategory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(JudgeAssignment))!;
        var roundForeignKey = entityType.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(JudgeAssignment.RoundId)));
        var categoryForeignKey = entityType.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(JudgeAssignment.CategoryId)));

        Assert.False(entityType.FindProperty(nameof(JudgeAssignment.RoundId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(JudgeAssignment.CategoryId))!.IsNullable);
        Assert.True(roundForeignKey.IsRequired);
        Assert.True(categoryForeignKey.IsRequired);
    }
}
