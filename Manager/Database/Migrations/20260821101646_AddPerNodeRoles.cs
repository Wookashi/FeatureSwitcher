using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wookashi.FeatureSwitcher.Manager.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPerNodeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAdmin",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RoleEnum",
                table: "UserNodeAccess",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Backfill: users whose old global role was Admin (2) become system admins.
            migrationBuilder.Sql("UPDATE Users SET IsSystemAdmin = 1 WHERE RoleEnum = 2;");

            // Backfill: existing node-access rows inherit the owning user's old global role
            // (Viewer = 0 is already the column default, so only Editor = 1 needs an update).
            migrationBuilder.Sql(
                "UPDATE UserNodeAccess SET RoleEnum = 1 " +
                "WHERE UserId IN (SELECT Id FROM Users WHERE RoleEnum = 1);");

            migrationBuilder.DropColumn(
                name: "RoleEnum",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleEnum",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE Users SET RoleEnum = 2 WHERE IsSystemAdmin = 1;");

            migrationBuilder.DropColumn(
                name: "RoleEnum",
                table: "UserNodeAccess");

            migrationBuilder.DropColumn(
                name: "IsSystemAdmin",
                table: "Users");
        }
    }
}
