using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Application.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubmissionSuggestions",
                columns: table => new
                {
                    SubmissionSuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginalReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullEditedReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PromptVersion = table.Column<int>(type: "int", nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionSuggestions", x => x.SubmissionSuggestionId);
                    table.ForeignKey(
                        name: "FK_SubmissionSuggestions_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionSuggestions_SubmissionId",
                table: "SubmissionSuggestions",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionSuggestions_SubmissionId_ReasonHash",
                table: "SubmissionSuggestions",
                columns: new[] { "SubmissionId", "ReasonHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionSuggestions");
        }
    }
}
