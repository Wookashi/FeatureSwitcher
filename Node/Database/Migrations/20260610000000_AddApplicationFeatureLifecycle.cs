using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFeatureLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite forbids ADD COLUMN with a non-constant default (e.g. CURRENT_TIMESTAMP) on an
            // existing table, so seed with a constant placeholder and backfill real values below.
            // (A fresh database rebuilds the table and would tolerate CURRENT_TIMESTAMP, but an
            // upgrade over existing rows uses a plain ALTER TABLE and fails — hence the constant.)
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "ApplicationFeatures",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingDeletionSince",
                table: "ApplicationFeatures",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ApplicationFeatures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill from the owning feature; CURRENT_TIMESTAMP is legal inside UPDATE and covers
            // any row whose feature has no timestamp, so no row keeps the placeholder default.
            migrationBuilder.Sql("""
                UPDATE ApplicationFeatures
                SET LastUsedAt = COALESCE(
                    (
                        SELECT Features.LastUsedAt
                        FROM Features
                        WHERE Features.Id = ApplicationFeatures.FeatureId
                    ),
                    CURRENT_TIMESTAMP)
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "ApplicationFeatures");

            migrationBuilder.DropColumn(
                name: "PendingDeletionSince",
                table: "ApplicationFeatures");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ApplicationFeatures");
        }
    }
}
