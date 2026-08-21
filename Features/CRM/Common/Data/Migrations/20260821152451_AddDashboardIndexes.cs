using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Features.CRM.Common.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_deals_ClosedAt",
                table: "deals",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_deals_OwnerUserId_Stage",
                table: "deals",
                columns: new[] { "OwnerUserId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_contacts_AssignedToUserId_Status",
                table: "contacts",
                columns: new[] { "AssignedToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_CreatedByUserId_IsCompleted_ScheduledAt",
                table: "activities",
                columns: new[] { "CreatedByUserId", "IsCompleted", "ScheduledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deals_ClosedAt",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_deals_OwnerUserId_Stage",
                table: "deals");

            migrationBuilder.DropIndex(
                name: "IX_contacts_AssignedToUserId_Status",
                table: "contacts");

            migrationBuilder.DropIndex(
                name: "IX_activities_CreatedByUserId_IsCompleted_ScheduledAt",
                table: "activities");
        }
    }
}
