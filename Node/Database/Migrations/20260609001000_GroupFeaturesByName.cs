using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <inheritdoc />
    public partial class GroupFeaturesByName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Features_Applications_ApplicationId",
                table: "Features");

            migrationBuilder.DropIndex(
                name: "IX_Features_ApplicationId_Name",
                table: "Features");

            migrationBuilder.Sql("""
                CREATE TEMP TABLE FeatureCanonical AS
                SELECT Name, MIN(Id) AS CanonicalId
                FROM Features
                GROUP BY Name
                """);

            migrationBuilder.Sql("""
                CREATE TEMP TABLE FeatureUsageMerged AS
                SELECT canonical.CanonicalId AS FeatureId,
                       usage.UsageDay AS UsageDay,
                       SUM(usage.UseCount) AS UseCount
                FROM FeatureUsage usage
                INNER JOIN Features feature ON feature.Id = usage.FeatureId
                INNER JOIN FeatureCanonical canonical ON canonical.Name = feature.Name
                GROUP BY canonical.CanonicalId, usage.UsageDay
                """);

            migrationBuilder.Sql("""
                DELETE FROM FeatureUsage
                """);

            migrationBuilder.Sql("""
                INSERT INTO FeatureUsage (FeatureId, UsageDay, UseCount)
                SELECT FeatureId, UsageDay, UseCount
                FROM FeatureUsageMerged
                """);

            migrationBuilder.Sql("""
                UPDATE ApplicationFeatures
                SET FeatureId = (
                    SELECT canonical.CanonicalId
                    FROM Features feature
                    INNER JOIN FeatureCanonical canonical ON canonical.Name = feature.Name
                    WHERE feature.Id = ApplicationFeatures.FeatureId
                )
                """);

            migrationBuilder.Sql("""
                DELETE FROM Features
                WHERE Id NOT IN (
                    SELECT CanonicalId
                    FROM FeatureCanonical
                )
                """);

            migrationBuilder.Sql("""
                DROP TABLE FeatureUsageMerged
                """);

            migrationBuilder.Sql("""
                DROP TABLE FeatureCanonical
                """);

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Features");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Name",
                table: "Features",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Features_Name",
                table: "Features");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationId",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE Features
                SET ApplicationId = COALESCE(
                    (
                        SELECT MIN(ApplicationId)
                        FROM ApplicationFeatures
                        WHERE ApplicationFeatures.FeatureId = Features.Id
                    ),
                    (
                        SELECT MIN(Id)
                        FROM Applications
                    ),
                    0
                )
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Features_ApplicationId_Name",
                table: "Features",
                columns: new[] { "ApplicationId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Features_Applications_ApplicationId",
                table: "Features",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
