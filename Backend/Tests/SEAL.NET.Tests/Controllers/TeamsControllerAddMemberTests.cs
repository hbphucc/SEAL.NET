using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

public class TeamsControllerAddMemberTests
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

    private static ApplicationUser CreateUser(
        string email,
        string? studentCode = null,
        bool isApproved = true)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            FullName = email.Split('@')[0],
            StudentCode = studentCode,
            IsApproved = isApproved
        };
    }

    private static TeamsController CreateController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ApplicationUser currentUser)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, currentUser.Id.ToString()),
                new Claim(ClaimTypes.Role, "TeamLeader")
            ],
            "TestAuth");

        return new TeamsController(context, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static Category CreateCategory()
    {
        var ev = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = "Hackathon",
            IsPublished = true
        };

        return new Category
        {
            CategoryId = Guid.NewGuid(),
            CategoryName = "Software",
            EventId = ev.EventId,
            Event = ev
        };
    }

    private static Team CreatePendingTeam(ApplicationUser leader, Category category)
    {
        return new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = $"Team {Guid.NewGuid():N}",
            LeaderId = leader.Id,
            CategoryId = category.CategoryId,
            Category = category,
            Status = TeamStatus.Pending
        };
    }

    private static async Task<(TeamsController Controller, ApplicationDbContext Context, ApplicationUser Leader, ApplicationUser Candidate, Team Team)> CreateTeamScenarioAsync(
        string candidateStudentCode = "ST001",
        bool candidateIsApproved = true,
        TeamStatus teamStatus = TeamStatus.Pending)
    {
        var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateUser("leader@example.com", "TL001");
        var candidate = CreateUser("candidate@example.com", candidateStudentCode, candidateIsApproved);
        var category = CreateCategory();
        var team = CreatePendingTeam(leader, category);
        team.Status = teamStatus;

        context.Add(category.Event);
        context.Add(category);
        context.Users.AddRange(leader, candidate);
        context.Teams.Add(team);
        context.TeamMembers.Add(new TeamMember
        {
            TeamId = team.TeamId,
            UserId = leader.Id,
            Role = TeamMemberRole.Leader,
            IsLeader = true
        });
        await context.SaveChangesAsync();

        return (CreateController(context, userManager, leader), context, leader, candidate, team);
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
    public void AddMemberToMyTeam_RequiresTeamLeaderRole()
    {
        var method = typeof(TeamsController).GetMethod(nameof(TeamsController.AddMemberToMyTeam));

        var authorize = Assert.Single(
            method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("TeamLeader", authorize.Roles);
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenCurrentUserLeadsNoTeam_ReturnsNotFoundAndDoesNotAddMembership()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateUser("leader@example.com", "TL001");
        var candidate = CreateUser("candidate@example.com", "ST001");

        context.Users.AddRange(leader, candidate);
        await context.SaveChangesAsync();

        var controller = CreateController(context, userManager, leader);

        var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
        {
            StudentCode = candidate.StudentCode!
        });

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(context.TeamMembers);
    }

    [Fact]
    public async Task AddMemberToMyTeam_UsesCurrentUsersLedTeamAndDoesNotModifyAnotherTeam()
    {
        using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var leader = CreateUser("leader@example.com", "TL001");
        var otherLeader = CreateUser("other-leader@example.com", "TL002");
        var candidate = CreateUser("candidate@example.com", "ST001");
        var category = CreateCategory();
        var myTeam = CreatePendingTeam(leader, category);
        var otherTeam = CreatePendingTeam(otherLeader, category);

        context.Add(category.Event);
        context.Add(category);
        context.Users.AddRange(leader, otherLeader, candidate);
        context.Teams.AddRange(myTeam, otherTeam);
        context.TeamMembers.AddRange(
            new TeamMember
            {
                TeamId = myTeam.TeamId,
                UserId = leader.Id,
                Role = TeamMemberRole.Leader,
                IsLeader = true
            },
            new TeamMember
            {
                TeamId = otherTeam.TeamId,
                UserId = otherLeader.Id,
                Role = TeamMemberRole.Leader,
                IsLeader = true
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userManager, leader);

        var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
        {
            StudentCode = candidate.StudentCode!
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AddTeamMemberResponse>(ok.Value);

        Assert.Equal(myTeam.TeamId, response.Team.TeamId);
        Assert.Equal(candidate.Id, response.AddedMember.UserId);
        Assert.True(await context.TeamMembers.AnyAsync(m =>
            m.TeamId == myTeam.TeamId &&
            m.UserId == candidate.Id));
        Assert.False(await context.TeamMembers.AnyAsync(m =>
            m.TeamId == otherTeam.TeamId &&
            m.UserId == candidate.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddMemberToMyTeam_WhenStudentCodeIsEmpty_ReturnsBadRequestAndDoesNotAddMembership(string studentCode)
    {
        var (controller, context, _, _, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = studentCode
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Student code is required.", GetMessage(result));
            Assert.Single(context.TeamMembers.Where(m => m.TeamId == team.TeamId));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenStudentCodeIsUnknown_ReturnsNotFoundAndDoesNotAddMembership()
    {
        var (controller, context, _, _, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = "UNKNOWN"
            });

            Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User with Student Code 'UNKNOWN' was not found.", GetMessage(result));
            Assert.Single(context.TeamMembers.Where(m => m.TeamId == team.TeamId));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenUserAlreadyInSameTeam_ReturnsBadRequestAndDoesNotDuplicateMembership()
    {
        var (controller, context, _, candidate, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.TeamId,
                UserId = candidate.Id,
                Role = TeamMemberRole.Member,
                IsLeader = false
            });
            await context.SaveChangesAsync();

            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = candidate.StudentCode!
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User is already in this team.", GetMessage(result));
            Assert.Equal(1, context.TeamMembers.Count(m => m.TeamId == team.TeamId && m.UserId == candidate.Id));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenLeaderAddsSelf_ReturnsBadRequestAndDoesNotDuplicateMembership()
    {
        var (controller, context, leader, _, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = leader.StudentCode!
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Leader is already part of the team.", GetMessage(result));
            Assert.Equal(1, context.TeamMembers.Count(m => m.TeamId == team.TeamId && m.UserId == leader.Id));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenUserAlreadyJoinedAnotherTeamInSameEvent_ReturnsBadRequest()
    {
        var (controller, context, _, candidate, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            var otherLeader = CreateUser("other-leader@example.com", "TL002");
            var otherTeam = new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = "Other Team",
                LeaderId = otherLeader.Id,
                CategoryId = team.CategoryId,
                Status = TeamStatus.Pending
            };
            context.Users.Add(otherLeader);
            context.Teams.Add(otherTeam);
            context.TeamMembers.AddRange(
                new TeamMember
                {
                    TeamId = otherTeam.TeamId,
                    UserId = otherLeader.Id,
                    Role = TeamMemberRole.Leader,
                    IsLeader = true
                },
                new TeamMember
                {
                    TeamId = otherTeam.TeamId,
                    UserId = candidate.Id,
                    Role = TeamMemberRole.Member,
                    IsLeader = false
                });
            await context.SaveChangesAsync();

            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = candidate.StudentCode!
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User already joined another team in this event.", GetMessage(result));
            Assert.False(context.TeamMembers.Any(m => m.TeamId == team.TeamId && m.UserId == candidate.Id));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenTeamIsFull_ReturnsBadRequest()
    {
        var (controller, context, _, candidate, team) = await CreateTeamScenarioAsync();
        await using (context)
        {
            var users = Enumerable.Range(1, 4)
                .Select(i => CreateUser($"member{i}@example.com", $"M{i:000}"))
                .ToList();
            context.Users.AddRange(users);
            context.TeamMembers.AddRange(users.Select(user => new TeamMember
            {
                TeamId = team.TeamId,
                UserId = user.Id,
                Role = TeamMemberRole.Member,
                IsLeader = false
            }));
            await context.SaveChangesAsync();

            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = candidate.StudentCode!
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A team can have maximum 5 members.", GetMessage(result));
            Assert.False(context.TeamMembers.Any(m => m.TeamId == team.TeamId && m.UserId == candidate.Id));
        }
    }

    [Fact]
    public async Task AddMemberToMyTeam_WhenUserIsUnapproved_ReturnsBadRequest()
    {
        var (controller, context, _, candidate, team) = await CreateTeamScenarioAsync(candidateIsApproved: false);
        await using (context)
        {
            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = candidate.StudentCode!
            });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("This user has not been approved yet.", GetMessage(result));
            Assert.False(context.TeamMembers.Any(m => m.TeamId == team.TeamId && m.UserId == candidate.Id));
        }
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("same-event")]
    [InlineData("team-full")]
    [InlineData("unapproved")]
    [InlineData("no-led-team")]
    public async Task AddMemberToMyTeam_DeniedIntegrityAttempts_CreateAuditLog(string scenario)
    {
        var (controller, context, leader, candidate, team) = scenario == "unapproved"
            ? await CreateTeamScenarioAsync(candidateIsApproved: false)
            : await CreateTeamScenarioAsync();
        await using (context)
        {
            if (scenario == "duplicate")
            {
                context.TeamMembers.Add(new TeamMember { TeamId = team.TeamId, UserId = candidate.Id });
            }
            else if (scenario == "same-event")
            {
                var otherLeader = CreateUser("other-leader@example.com", "TL002");
                var otherTeam = new Team
                {
                    TeamId = Guid.NewGuid(),
                    TeamName = "Other Team",
                    LeaderId = otherLeader.Id,
                    CategoryId = team.CategoryId,
                    Status = TeamStatus.Pending
                };
                context.Users.Add(otherLeader);
                context.Teams.Add(otherTeam);
                context.TeamMembers.Add(new TeamMember { TeamId = otherTeam.TeamId, UserId = candidate.Id });
            }
            else if (scenario == "team-full")
            {
                var users = Enumerable.Range(1, 4).Select(i => CreateUser($"full{i}@example.com", $"F{i:000}")).ToList();
                context.Users.AddRange(users);
                context.TeamMembers.AddRange(users.Select(user => new TeamMember { TeamId = team.TeamId, UserId = user.Id }));
            }
            else if (scenario == "no-led-team")
            {
                context.TeamMembers.RemoveRange(context.TeamMembers);
                context.Teams.RemoveRange(context.Teams);
            }

            await context.SaveChangesAsync();

            var result = await controller.AddMemberToMyTeam(new AddTeamMemberRequest
            {
                StudentCode = scenario == "no-led-team" ? leader.StudentCode! : candidate.StudentCode!
            });

            Assert.IsAssignableFrom<ObjectResult>(result);
            var audit = Assert.Single(context.AuditLogs.Where(log => log.Action == "TeamMemberAddDenied"));
            Assert.Contains("Outcome=", audit.Details);
            Assert.Contains("StudentCode=", audit.Details);
        }
    }
}
