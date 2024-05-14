using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addVoucherToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VoucherID",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Vouchers",
                columns: table => new
                {
                    VoucherID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vouchers", x => x.VoucherID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets",
                column: "VoucherID",
                unique: true,
                filter: "[VoucherID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Vouchers_VoucherID",
                table: "Tickets",
                column: "VoucherID",
                principalTable: "Vouchers",
                principalColumn: "VoucherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Vouchers_VoucherID",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_VoucherID",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "VoucherID",
                table: "Tickets");
        }
    }
}
