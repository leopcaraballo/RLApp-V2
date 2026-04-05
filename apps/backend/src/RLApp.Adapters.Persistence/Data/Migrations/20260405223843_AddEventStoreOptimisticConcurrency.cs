using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RLApp.Adapters.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStoreOptimisticConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "EventStore",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH ordered_events AS (
                    SELECT "Id", ROW_NUMBER() OVER (
                        PARTITION BY "AggregateId"
                        ORDER BY "OccurredAt", "Id") AS "SequenceNumber"
                    FROM "EventStore"
                )
                UPDATE "EventStore" AS event_store
                SET "SequenceNumber" = ordered_events."SequenceNumber"
                FROM ordered_events
                WHERE event_store."Id" = ordered_events."Id";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "SequenceNumber",
                table: "EventStore",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventStore_AggregateId_SequenceNumber",
                table: "EventStore",
                columns: new[] { "AggregateId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventStore_AggregateId_SequenceNumber",
                table: "EventStore");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "EventStore");
        }
    }
}
