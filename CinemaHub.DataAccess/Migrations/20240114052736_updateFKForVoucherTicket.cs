using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class updateFKForVoucherTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets",
                column: "VoucherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets",
                column: "VoucherID",
                unique: true,
                filter: "[VoucherID] IS NOT NULL");
        }
    }
}
