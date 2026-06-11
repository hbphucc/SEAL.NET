using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SEAL.NET.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeEnumStringStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Widen the columns to nvarchar first. SQL Server converts the existing
            // int values to their numeric string form ('0', '1', ...).
            migrationBuilder.AlterColumn<string>(
                name: "StudentType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Rounds",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // Map the numeric strings to the corresponding enum names so existing rows
            // match the new string storage (and the JSON the API already returns).
            migrationBuilder.Sql(@"
UPDATE [Users] SET [StudentType] = CASE [StudentType]
    WHEN '0' THEN 'FPT'
    WHEN '1' THEN 'External'
    ELSE [StudentType] END
WHERE [StudentType] IS NOT NULL;");

            migrationBuilder.Sql(@"
UPDATE [Rounds] SET [Status] = CASE [Status]
    WHEN '0' THEN 'Draft'
    WHEN '1' THEN 'Open'
    WHEN '2' THEN 'Closed'
    WHEN '3' THEN 'Locked'
    WHEN '4' THEN 'ResultsPublished'
    ELSE [Status] END;");

            migrationBuilder.Sql(@"
UPDATE [Notifications] SET [Status] = CASE [Status]
    WHEN '0' THEN 'Unread'
    WHEN '1' THEN 'Read'
    ELSE [Status] END;");

            migrationBuilder.Sql(@"
UPDATE [Events] SET [Status] = CASE [Status]
    WHEN '0' THEN 'Draft'
    WHEN '1' THEN 'Upcoming'
    WHEN '2' THEN 'RegistrationClosed'
    WHEN '3' THEN 'Judging'
    WHEN '4' THEN 'RankingPublished'
    WHEN '5' THEN 'Ongoing'
    WHEN '6' THEN 'Completed'
    WHEN '7' THEN 'Cancelled'
    WHEN '8' THEN 'Archived'
    ELSE [Status] END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert the enum names back to their numeric strings while the columns are
            // still nvarchar, so the values can be cast to int by the AlterColumn calls.
            migrationBuilder.Sql(@"
UPDATE [Users] SET [StudentType] = CASE [StudentType]
    WHEN 'FPT' THEN '0'
    WHEN 'External' THEN '1'
    ELSE [StudentType] END
WHERE [StudentType] IS NOT NULL;");

            migrationBuilder.Sql(@"
UPDATE [Rounds] SET [Status] = CASE [Status]
    WHEN 'Draft' THEN '0'
    WHEN 'Open' THEN '1'
    WHEN 'Closed' THEN '2'
    WHEN 'Locked' THEN '3'
    WHEN 'ResultsPublished' THEN '4'
    ELSE [Status] END;");

            migrationBuilder.Sql(@"
UPDATE [Notifications] SET [Status] = CASE [Status]
    WHEN 'Unread' THEN '0'
    WHEN 'Read' THEN '1'
    ELSE [Status] END;");

            migrationBuilder.Sql(@"
UPDATE [Events] SET [Status] = CASE [Status]
    WHEN 'Draft' THEN '0'
    WHEN 'Upcoming' THEN '1'
    WHEN 'RegistrationClosed' THEN '2'
    WHEN 'Judging' THEN '3'
    WHEN 'RankingPublished' THEN '4'
    WHEN 'Ongoing' THEN '5'
    WHEN 'Completed' THEN '6'
    WHEN 'Cancelled' THEN '7'
    WHEN 'Archived' THEN '8'
    ELSE [Status] END;");

            migrationBuilder.AlterColumn<int>(
                name: "StudentType",
                table: "Users",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Rounds",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Events",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
