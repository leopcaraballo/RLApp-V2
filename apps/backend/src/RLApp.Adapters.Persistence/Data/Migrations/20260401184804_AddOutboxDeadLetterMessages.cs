using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RLApp.Adapters.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeadLetterMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxDeadLetterMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDeadLetterMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeadLetterMessages_AggregateId_FailedAt",
                table: "OutboxDeadLetterMessages",
                columns: new[] { "AggregateId", "FailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeadLetterMessages_CorrelationId",
                table: "OutboxDeadLetterMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeadLetterMessages_FailedAt",
                table: "OutboxDeadLetterMessages",
                column: "FailedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxDeadLetterMessages");
        }
    }
}
