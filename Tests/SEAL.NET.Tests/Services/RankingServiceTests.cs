using SEAL.NET.Models.Entities;
using SEAL.NET.Services.Implementations;

namespace SEAL.NET.Tests.Services;

public class RankingServiceTests
{
    private static Score MakeScore(Guid criteriaId, decimal scoreValue, decimal maxScore, decimal weight)
    {
        var criteria = new Criteria
        {
            CriteriaId = criteriaId,
            CriteriaName = "Test",
            MaxScore = maxScore,
            Weight = weight
        };
        return new Score
        {
            CriteriaId = criteriaId,
            ScoreValue = scoreValue,
            Criteria = criteria
        };
    }

    [Fact]
    public void CalculateWeightedScore_EmptyScores_ReturnsZero()
    {
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore([]);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateWeightedScore_SingleCriteria_FullScore_ReturnsWeight()
    {
        // score = 10 / 10 * weight 2 = 2
        var criteriaId = Guid.NewGuid();
        var scores = new[] { MakeScore(criteriaId, 10m, 10m, 2m) };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(2m, result);
    }

    [Fact]
    public void CalculateWeightedScore_SingleCriteria_HalfScore_ReturnsHalfWeight()
    {
        // score = 5 / 10 * weight 4 = 2
        var criteriaId = Guid.NewGuid();
        var scores = new[] { MakeScore(criteriaId, 5m, 10m, 4m) };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(2m, result);
    }

    [Fact]
    public void CalculateWeightedScore_MultipleCriteria_SumsCorrectly()
    {
        // crit1: 10/10 * 3 = 3
        // crit2:  6/10 * 2 = 1.2
        // total = 4.2
        var crit1 = Guid.NewGuid();
        var crit2 = Guid.NewGuid();
        var scores = new[]
        {
            MakeScore(crit1, 10m, 10m, 3m),
            MakeScore(crit2, 6m, 10m, 2m)
        };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(4.2m, result);
    }

    [Fact]
    public void CalculateWeightedScore_MultipleJudgesSameCriteria_AveragesScoreFirst()
    {
        // Two judges score crit1: 8 and 10 → avg = 9. Weighted = 9/10 * 5 = 4.5
        var criteriaId = Guid.NewGuid();
        var criteria = new Criteria
        {
            CriteriaId = criteriaId,
            CriteriaName = "Test",
            MaxScore = 10m,
            Weight = 5m
        };
        var scores = new[]
        {
            new Score { CriteriaId = criteriaId, ScoreValue = 8m, Criteria = criteria },
            new Score { CriteriaId = criteriaId, ScoreValue = 10m, Criteria = criteria }
        };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(4.5m, result);
    }

    [Fact]
    public void CalculateWeightedScore_ZeroMaxScore_CriteriaExcluded()
    {
        // criteria with MaxScore = 0 should be skipped
        var criteriaId = Guid.NewGuid();
        var criteria = new Criteria
        {
            CriteriaId = criteriaId,
            CriteriaName = "Invalid",
            MaxScore = 0m,
            Weight = 10m
        };
        var scores = new[]
        {
            new Score { CriteriaId = criteriaId, ScoreValue = 8m, Criteria = criteria }
        };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateWeightedScore_NullCriteria_ScoreExcluded()
    {
        // Score with null Criteria should be skipped
        var scores = new[]
        {
            new Score { CriteriaId = Guid.NewGuid(), ScoreValue = 9m, Criteria = null }
        };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData(0, 10, 5, 0)]    // 0% score → 0 weighted
    [InlineData(10, 10, 5, 5)]   // 100% score → full weight
    [InlineData(5, 10, 5, 2.5)]  // 50% score → half weight
    public void CalculateWeightedScore_ParameterizedCases(
        double score, double maxScore, double weight, double expected)
    {
        var criteriaId = Guid.NewGuid();
        var scores = new[] { MakeScore(criteriaId, (decimal)score, (decimal)maxScore, (decimal)weight) };
        var svc = new RankingService(null!);
        var result = svc.CalculateWeightedScore(scores);
        Assert.Equal((decimal)expected, result);
    }
}
