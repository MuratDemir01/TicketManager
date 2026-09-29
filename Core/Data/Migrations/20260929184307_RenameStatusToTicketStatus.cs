using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketManager.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatusToTicketStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Tickets",
                newName: "TicketStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TicketStatus",
                table: "Tickets",
                newName: "Status");
        }
    }
}
