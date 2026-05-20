using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Features_ApplicationId",
                table: "Features");

            migrationBuilder.DropIndex(
                name: "IX_Features_Name",
                table: "Features");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "Features",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingDeletionSince",
                table: "Features",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "Applications",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingDeletionSince",
                table: "Applications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Applications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FeatureUsage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageDay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UseCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureUsage_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Features_ApplicationId_Name",
                table: "Features",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureUsage_FeatureId_UsageDay",
                table: "FeatureUsage",
                columns: new[] { "FeatureId", "UsageDay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureUsage");

            migrationBuilder.DropIndex(
                name: "IX_Features_ApplicationId_Name",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "PendingDeletionSince",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "PendingDeletionSince",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Features_ApplicationId",
                table: "Features",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Name",
                table: "Features",
                column: "Name",
                unique: true);
        }
    }
}
