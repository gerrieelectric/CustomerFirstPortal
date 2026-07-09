using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portal.Application.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDispatchQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationDispatches",
                columns: table => new
                {
                    NotificationDispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecipientDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    NextAttemptUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    LastAttemptUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    SentUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDispatches", x => x.NotificationDispatchId);
                    table.ForeignKey(
                        name: "FK_NotificationDispatches_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDispatches_Status_NextAttemptUtc",
                table: "NotificationDispatches",
                columns: new[] { "Status", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDispatches_SubmissionId_CreatedUtc",
                table: "NotificationDispatches",
                columns: new[] { "SubmissionId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDispatches");
        }
    }
}
