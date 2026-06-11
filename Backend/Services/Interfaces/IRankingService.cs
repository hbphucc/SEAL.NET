using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SEAL.NET.Common;
using SEAL.NET.Models.Entities;

namespace SEAL.NET.Services.Interfaces
{
    public interface IRankingService
    {
        decimal CalculateWeightedScore(IEnumerable<Score> scores);
        Task<AdvanceRoundResult> AdvanceRoundAsync(Guid roundId);

        /// <summary>
        /// Ranking for every team in a round. When <paramref name="requirePublished"/> is true
        /// (public endpoint) the round must exist and have its ranking published; otherwise only
        /// existence is required (admin endpoint).
        /// </summary>
        Task<ServiceResult> GetRoundRankingAsync(Guid roundId, bool requirePublished);

        /// <summary>Ranking for one category within a round, with the same publish gating.</summary>
        Task<ServiceResult> GetCategoryRoundRankingAsync(Guid categoryId, Guid roundId, bool requirePublished);
    }

    public class AdvanceRoundResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
