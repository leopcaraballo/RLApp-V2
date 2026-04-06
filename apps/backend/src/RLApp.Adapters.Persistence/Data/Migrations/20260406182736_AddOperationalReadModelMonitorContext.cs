using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RLApp.Adapters.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalReadModelMonitorContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "v_waiting_room_monitor",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PatientId",
                table: "v_waiting_room_monitor",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QueueId",
                table: "v_waiting_room_monitor",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_v_waiting_room_monitor_PatientId",
                table: "v_waiting_room_monitor",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_v_waiting_room_monitor_QueueId",
                table: "v_waiting_room_monitor",
                column: "QueueId");

            migrationBuilder.CreateIndex(
                name: "IX_v_waiting_room_monitor_QueueId_Status_UpdatedAt",
                table: "v_waiting_room_monitor",
                columns: new[] { "QueueId", "Status", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_v_waiting_room_monitor_PatientId",
                table: "v_waiting_room_monitor");

            migrationBuilder.DropIndex(
                name: "IX_v_waiting_room_monitor_QueueId",
                table: "v_waiting_room_monitor");

            migrationBuilder.DropIndex(
                name: "IX_v_waiting_room_monitor_QueueId_Status_UpdatedAt",
                table: "v_waiting_room_monitor");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "v_waiting_room_monitor");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "v_waiting_room_monitor");

            migrationBuilder.DropColumn(
                name: "QueueId",
                table: "v_waiting_room_monitor");
        }
    }
}
