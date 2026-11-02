using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnHub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN ('ADMIN', 'STUDENT', 'INSTRUCTOR')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");
        }
    }
}
