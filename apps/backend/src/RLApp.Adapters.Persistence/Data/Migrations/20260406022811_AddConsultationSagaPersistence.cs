using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RLApp.Adapters.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultationSagaPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsultationSagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TrajectoryId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastCorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    QueueId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RoomId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeoutTokenId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationSagaStates", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSagaStates_LastCorrelationId",
                table: "ConsultationSagaStates",
                column: "LastCorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSagaStates_PatientId",
                table: "ConsultationSagaStates",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationSagaStates_TrajectoryId",
                table: "ConsultationSagaStates",
                column: "TrajectoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultationSagaStates");
        }
    }
}
