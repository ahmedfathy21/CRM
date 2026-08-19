using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Features.CRM.Common.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "activities");
        }
    }
}
