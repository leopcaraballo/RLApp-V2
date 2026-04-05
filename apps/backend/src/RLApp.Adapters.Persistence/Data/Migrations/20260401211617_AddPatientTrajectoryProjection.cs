using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RLApp.Adapters.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientTrajectoryProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "v_patient_trajectory",
                columns: table => new
                {
                    TrajectoryId = table.Column<string>(type: "text", nullable: false),
                    PatientId = table.Column<string>(type: "text", nullable: false),
                    QueueId = table.Column<string>(type: "text", nullable: false),
                    CurrentState = table.Column<string>(type: "text", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    StagesJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_v_patient_trajectory", x => x.TrajectoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_v_patient_trajectory_PatientId",
                table: "v_patient_trajectory",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_v_patient_trajectory_PatientId_QueueId_CurrentState",
                table: "v_patient_trajectory",
                columns: new[] { "PatientId", "QueueId", "CurrentState" });

            migrationBuilder.CreateIndex(
                name: "IX_v_patient_trajectory_QueueId",
                table: "v_patient_trajectory",
                column: "QueueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "v_patient_trajectory");
        }
    }
}
