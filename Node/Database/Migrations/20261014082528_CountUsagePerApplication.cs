using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <summary>
    /// Moves the daily usage counters from the global flag (FeatureUsage) to the application-feature
    /// link (ApplicationFeatureUsage), so each application has its own read count.
    /// Counts of flags used by a single application are copied to its link. Counts of shared flags are
    /// a sum over applications that cannot be split, so they are dropped and start again from zero.
    /// </summary>
    public partial class CountUsagePerApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationFeatureUsage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationFeatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsageDay = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UseCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFeatureUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationFeatureUsage_ApplicationFeatures_ApplicationFeatureId",
                        column: x => x.ApplicationFeatureId,
                        principalTable: "ApplicationFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFeatureUsage_ApplicationFeatureId_UsageDay",
                table: "ApplicationFeatureUsage",
                columns: new[] { "ApplicationFeatureId", "UsageDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFeatureUsage_UsageDay",
                table: "ApplicationFeatureUsage",
                column: "UsageDay");

            migrationBuilder.Sql(
                """
                INSERT INTO ApplicationFeatureUsage (ApplicationFeatureId, UsageDay, UseCount)
                SELECT link.Id, usage.UsageDay, usage.UseCount
                FROM FeatureUsage usage
                JOIN ApplicationFeatures link ON link.FeatureId = usage.FeatureId
                WHERE (SELECT COUNT(*) FROM ApplicationFeatures other WHERE other.FeatureId = usage.FeatureId) = 1;
                """);

            migrationBuilder.DropTable(
                name: "FeatureUsage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_FeatureUsage_FeatureId_UsageDay",
                table: "FeatureUsage",
                columns: new[] { "FeatureId", "UsageDay" },
                unique: true);

            // Per-application counts sum up to the previous per-flag counts.
            migrationBuilder.Sql(
                """
                INSERT INTO FeatureUsage (FeatureId, UsageDay, UseCount)
                SELECT link.FeatureId, usage.UsageDay, SUM(usage.UseCount)
                FROM ApplicationFeatureUsage usage
                JOIN ApplicationFeatures link ON link.Id = usage.ApplicationFeatureId
                GROUP BY link.FeatureId, usage.UsageDay;
                """);

            migrationBuilder.DropTable(
                name: "ApplicationFeatureUsage");
        }
    }
}
