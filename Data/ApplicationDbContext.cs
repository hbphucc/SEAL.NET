using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SEAL.NET.Models.Entities;

namespace SEAL.NET.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Criteria> Criteria { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<JudgeAssignment> JudgeAssignments { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<ScoreAuditLog> ScoreAuditLogs { get; set; }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            builder.Entity<Team>()
                .HasOne(t => t.Leader)
                .WithMany(u => u.LedTeams)
                .HasForeignKey(t => t.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Team>()
                .HasOne(t => t.CurrentRound)
                .WithMany()
                .HasForeignKey(t => t.CurrentRoundId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMemberships)
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Score>()
                .HasOne(s => s.Criteria)
                .WithMany(c => c.Scores)
                .HasForeignKey(s => s.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Score>()
                .HasOne(s => s.Submission)
                .WithMany(sub => sub.Scores)
                .HasForeignKey(s => s.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ScoreAuditLog>()
                .HasOne(log => log.Score)
                .WithMany()
                .HasForeignKey(log => log.ScoreId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ScoreAuditLog>()
                .HasOne(log => log.Submission)
                .WithMany()
                .HasForeignKey(log => log.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ScoreAuditLog>()
                .HasOne(log => log.Judge)
                .WithMany()
                .HasForeignKey(log => log.JudgeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ScoreAuditLog>()
                .HasOne(log => log.Criteria)
                .WithMany()
                .HasForeignKey(log => log.CriteriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JudgeAssignment>()
                .HasOne(ja => ja.Round)
                .WithMany(r => r.JudgeAssignments)
                .HasForeignKey(ja => ja.RoundId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.Entity<JudgeAssignment>()
                .HasOne(ja => ja.Category)
                .WithMany(c => c.JudgeAssignments)
                .HasForeignKey(ja => ja.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.Entity<Submission>()
                .HasOne(s => s.Round)
                .WithMany(r => r.Submissions)
                .HasForeignKey(s => s.RoundId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Submission>()
                .HasIndex(s => new { s.TeamId, s.RoundId })
                .IsUnique();

            builder.Entity<Score>()
                .HasIndex(s => new { s.SubmissionId, s.JudgeId, s.CriteriaId })
                .IsUnique();

            builder.Entity<TeamMember>()
                .HasIndex(tm => new { tm.TeamId, tm.UserId })
                .IsUnique();

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                value => NormalizeUtc(value),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                value => value.HasValue ? NormalizeUtc(value.Value) : null,
                value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }
    }
}
