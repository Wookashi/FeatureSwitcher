using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Node.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFeatureLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationFeatures_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO ApplicationFeatures (ApplicationId, FeatureId)
                SELECT ApplicationId, Id
                FROM Features
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFeatures_ApplicationId_FeatureId",
                table: "ApplicationFeatures",
                columns: new[] { "ApplicationId", "FeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationFeatures_FeatureId",
                table: "ApplicationFeatures",
                column: "FeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationFeatures");
        }
    }
}
