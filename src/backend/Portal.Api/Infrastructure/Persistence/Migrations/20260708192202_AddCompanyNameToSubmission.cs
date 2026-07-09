using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyNameToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: the column may already exist where the SQL database
            // project deployed it before this migration was created.
            migrationBuilder.Sql("""
                IF COL_LENGTH('Submissions', 'CompanyName') IS NULL
                BEGIN
                    ALTER TABLE [Submissions] ADD [CompanyName] nvarchar(200) NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Submissions");
        }
    }
}
