using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFeatureCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ApplicationFeatures",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // The real registration time of existing links is unknown. Use the upgrade time (UTC), so
            // existing features are not reported as unread before they have had a full warning period
            // to be read. SQLite rejects a non-constant default in ADD COLUMN, hence the UPDATE.
            migrationBuilder.Sql(
                "UPDATE ApplicationFeatures SET CreatedAt = strftime('%Y-%m-%d %H:%M:%f', 'now');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ApplicationFeatures");
        }
    }
}
