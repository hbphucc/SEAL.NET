using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL.NET.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSubmissionPerTeamRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_TeamId",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TeamId_RoundId",
                table: "Submissions",
                columns: new[] { "TeamId", "RoundId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_TeamId_RoundId",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TeamId",
                table: "Submissions",
                column: "TeamId");
        }
    }
}
