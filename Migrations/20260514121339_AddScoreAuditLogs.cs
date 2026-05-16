using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL.NET.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoreAuditLogs",
                columns: table => new
                {
                    ScoreAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JudgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OldScoreValue = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    NewScoreValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OldComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreAuditLogs", x => x.ScoreAuditLogId);
                    table.ForeignKey(
                        name: "FK_ScoreAuditLogs_Criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "Criteria",
                        principalColumn: "CriteriaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreAuditLogs_Scores_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Scores",
                        principalColumn: "ScoreId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreAuditLogs_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScoreAuditLogs_Users_JudgeId",
                        column: x => x.JudgeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAuditLogs_CriteriaId",
                table: "ScoreAuditLogs",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAuditLogs_JudgeId",
                table: "ScoreAuditLogs",
                column: "JudgeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAuditLogs_ScoreId",
                table: "ScoreAuditLogs",
                column: "ScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreAuditLogs_SubmissionId",
                table: "ScoreAuditLogs",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreAuditLogs");
        }
    }
}
