using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SEAL.NET.Data;
using SEAL.NET.Models.Entities;
using SEAL.NET.Models.Enums;
using SEAL.NET.Services.Interfaces;

namespace SEAL.NET.Services.Implementations
{
    public class RankingService : IRankingService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser>? _userManager;

        public RankingService(ApplicationDbContext context, UserManager<ApplicationUser>? userManager = null)
        {
            _context = context;
            _userManager = userManager;
        }

        public decimal CalculateWeightedScore(IEnumerable<Score> scores)
        {
            return scores
                .Where(score => score.Criteria != null && score.Criteria.MaxScore > 0)
                .GroupBy(score => score.CriteriaId)
                .Sum(criteriaScores =>
                    criteriaScores.Average(score => score.ScoreValue / score.Criteria!.MaxScore) *
                    criteriaScores.First().Criteria!.Weight);
        }

        public async Task<AdvanceRoundResult> AdvanceRoundAsync(Guid roundId)
        {
            var currentRound = await _context.Rounds
                .FirstOrDefaultAsync(r => r.RoundId == roundId);

            if (currentRound == null)
                return new AdvanceRoundResult { Success = false, Message = "Round not found." };

            var nextRound = await _context.Rounds
                .Where(r => r.EventId == currentRound.EventId &&
                            r.RoundOrder > currentRound.RoundOrder)
                .OrderBy(r => r.RoundOrder)
                .FirstOrDefaultAsync();

            if (nextRound == null)
                return new AdvanceRoundResult { Success = false, Message = "This is the final round. No next round found." };

            if (currentRound.MaxTeamsAdvancing <= 0)
                return new AdvanceRoundResult { Success = false, Message = "MaxTeamsAdvancing must be greater than 0." };

            var teams = await _context.Teams
                .Include(t => t.Category)
                .Include(t => t.Submissions.Where(s => s.RoundId == roundId))
                    .ThenInclude(s => s.Scores)
                        .ThenInclude(sc => sc.Criteria)
                .Where(t =>
                    t.CurrentRoundId == roundId &&
                    t.Status == TeamStatus.Approved)
                .ToListAsync();

            if (!teams.Any())
                return new AdvanceRoundResult { Success = false, Message = "No approved teams found for this round." };

            var criteriaIds = await _context.Criteria
                .Where(c => c.RoundId == roundId)
                .Select(c => c.CriteriaId)
                .ToListAsync();

            if (!criteriaIds.Any())
                return new AdvanceRoundResult { Success = false, Message = "Cannot advance round because this round has no scoring criteria." };

            var judgeAssignments = await _context.JudgeAssignments
                .Where(a => a.RoundId == roundId)
                .Select(a => new
                {
                    a.CategoryId,
                    a.JudgeId
                })
                .Distinct()
                .ToListAsync();

            var categoryIds = teams
                .Select(t => t.CategoryId)
                .Distinct()
                .ToList();

            var categoriesWithoutJudges = categoryIds
                .Where(categoryId => judgeAssignments.All(a => a.CategoryId != categoryId))
                .ToList();

            if (categoriesWithoutJudges.Any())
            {
                return new AdvanceRoundResult
                {
                    Success = false,
                    Message = "Cannot advance round because one or more categories have no assigned judges.",
                    Data = new { categoryIds = categoriesWithoutJudges }
                };
            }

            var teamsMissingSubmission = teams
                .Where(t => !t.Submissions.Any())
                .Select(t => new
                {
                    t.TeamId,
                    t.TeamName,
                    t.CategoryId
                })
                .ToList();

            if (teamsMissingSubmission.Any())
            {
                return new AdvanceRoundResult
                {
                    Success = false,
                    Message = "Cannot advance round because one or more approved teams have not submitted for this round.",
                    Data = new { teams = teamsMissingSubmission }
                };
            }

            var submissionChecks = teams
                .Select(t => new
                {
                    t.TeamId,
                    t.TeamName,
                    t.CategoryId,
                    Submission = t.Submissions
                        .OrderByDescending(s => s.SubmittedAt)
                        .First()
                })
                .ToList();

            var submissionIds = submissionChecks
                .Select(s => s.Submission.SubmissionId)
                .ToList();

            var assignedJudgeIds = judgeAssignments
                .Select(a => a.JudgeId)
                .Distinct()
                .ToList();

            var actualScoreKeys = await _context.Scores
                .Where(s =>
                    submissionIds.Contains(s.SubmissionId) &&
                    criteriaIds.Contains(s.CriteriaId) &&
                    assignedJudgeIds.Contains(s.JudgeId))
                .Select(s => new
                {
                    s.SubmissionId,
                    s.JudgeId,
                    s.CriteriaId
                })
                .Distinct()
                .ToListAsync();

            var actualScoreSet = actualScoreKeys
                .Select(s => (s.SubmissionId, s.JudgeId, s.CriteriaId))
                .ToHashSet();

            var expectedScoreCount = 0;
            var missingScores = new List<object>();

            foreach (var submissionCheck in submissionChecks)
            {
                var assignedJudgesForCategory = judgeAssignments
                    .Where(a => a.CategoryId == submissionCheck.CategoryId)
                    .Select(a => a.JudgeId)
                    .Distinct()
                    .ToList();

                expectedScoreCount += assignedJudgesForCategory.Count * criteriaIds.Count;

                foreach (var judgeId in assignedJudgesForCategory)
                {
                    foreach (var criteriaId in criteriaIds)
                    {
                        var scoreKey = (submissionCheck.Submission.SubmissionId, judgeId, criteriaId);

                        if (actualScoreSet.Contains(scoreKey))
                            continue;

                        missingScores.Add(new
                        {
                            submissionCheck.TeamId,
                            submissionCheck.TeamName,
                            submissionCheck.Submission.SubmissionId,
                            judgeId,
                            criteriaId
                        });
                    }
                }
            }

            if (missingScores.Any())
            {
                return new AdvanceRoundResult
                {
                    Success = false,
                    Message = "Cannot advance round because scoring is not complete.",
                    Data = new
                    {
                        expectedScoreCount,
                        actualScoreCount = actualScoreSet.Count,
                        missingScoreCount = missingScores.Count,
                        missingScores = missingScores.Take(50)
                    }
                };
            }

            var groupedByCategory = teams
                .GroupBy(t => t.CategoryId);

            var advancedTeams = new List<object>();
            var eliminatedTeams = new List<object>();

            foreach (var categoryGroup in groupedByCategory)
            {
                var rankedTeams = categoryGroup
                    .Select(t =>
                    {
                        var submission = t.Submissions
                            .OrderByDescending(s => s.SubmittedAt)
                            .FirstOrDefault();

                        return new
                        {
                            Submission = submission,
                            Team = t,
                            TotalScore = submission == null
                                ? 0
                                : CalculateWeightedScore(submission.Scores)
                        };
                    })
                    .Where(x => x.Submission != null)
                    .OrderByDescending(x => x.TotalScore)
                    .ThenBy(x => x.Submission!.SubmittedAt)
                    .ToList();

                var winners = rankedTeams
                    .Take(currentRound.MaxTeamsAdvancing)
                    .ToList();

                var losers = categoryGroup
                    .Where(t => winners.All(w => w.Team.TeamId != t.TeamId))
                    .Select(t =>
                    {
                        var submission = t.Submissions
                            .OrderByDescending(s => s.SubmittedAt)
                            .FirstOrDefault();

                        return new
                        {
                            Submission = submission,
                            Team = t,
                            TotalScore = submission == null
                                ? 0
                                : CalculateWeightedScore(submission.Scores)
                        };
                    })
                    .OrderByDescending(x => x.TotalScore)
                    .ThenBy(x => x.Submission == null ? DateTime.MaxValue : x.Submission.SubmittedAt)
                    .ToList();

                foreach (var item in winners)
                {
                    item.Team.CurrentRoundId = nextRound.RoundId;
                    item.Team.Status = TeamStatus.Approved;

                    advancedTeams.Add(new
                    {
                        item.Team.TeamId,
                        item.Team.TeamName,
                        categoryId = item.Team.CategoryId,
                        totalScore = item.TotalScore
                    });
                }

                foreach (var item in losers)
                {
                    item.Team.Status = TeamStatus.Eliminated;
                    item.Team.EliminationReason = "Eliminated after round ranking.";
                    item.Team.EliminatedAt = DateTime.UtcNow;

                    eliminatedTeams.Add(new
                    {
                        item.Team.TeamId,
                        item.Team.TeamName,
                        categoryId = item.Team.CategoryId,
                        totalScore = item.TotalScore
                    });
                }
            }

            currentRound.IsRankingPublished = true;

            await _context.SaveChangesAsync();
            await RemoveTeamLeaderRolesForEliminatedTeamsAsync(losers: teams.Where(t => t.Status == TeamStatus.Eliminated));

            return new AdvanceRoundResult
            {
                Success = true,
                Message = "Round advanced successfully.",
                Data = new
                {
                    fromRound = new
                    {
                        currentRound.RoundId,
                        currentRound.RoundName
                    },
                    toRound = new
                    {
                        nextRound.RoundId,
                        nextRound.RoundName
                    },
                    advancedTeams,
                    eliminatedTeams
                }
            };
        }

        private async Task RemoveTeamLeaderRolesForEliminatedTeamsAsync(IEnumerable<Team> losers)
        {
            if (_userManager == null)
                return;

            var leaderIds = losers
                .Select(t => t.LeaderId)
                .Distinct()
                .ToList();

            foreach (var leaderId in leaderIds)
            {
                var hasApprovedTeam = await _context.Teams.AnyAsync(t =>
                    t.LeaderId == leaderId &&
                    t.Status == TeamStatus.Approved);

                if (hasApprovedTeam)
                    continue;

                var leader = await _userManager.FindByIdAsync(leaderId.ToString());
                if (leader == null || !await _userManager.IsInRoleAsync(leader, "TeamLeader"))
                    continue;

                await _userManager.RemoveFromRoleAsync(leader, "TeamLeader");
                await _userManager.UpdateSecurityStampAsync(leader);
            }
        }
    }
}
