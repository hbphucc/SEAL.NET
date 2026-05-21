using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SEAL.NET.Models.Entities;

namespace SEAL.NET.Services.Interfaces
{
    public interface IRankingService
    {
        decimal CalculateWeightedScore(IEnumerable<Score> scores);
        Task<AdvanceRoundResult> AdvanceRoundAsync(Guid roundId);
    }

    public class AdvanceRoundResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
