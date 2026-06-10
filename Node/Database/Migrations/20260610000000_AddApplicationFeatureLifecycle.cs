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
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "ApplicationFeatures",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

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

            migrationBuilder.Sql("""
                UPDATE ApplicationFeatures
                SET LastUsedAt = (
                    SELECT Features.LastUsedAt
                    FROM Features
                    WHERE Features.Id = ApplicationFeatures.FeatureId
                )
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
