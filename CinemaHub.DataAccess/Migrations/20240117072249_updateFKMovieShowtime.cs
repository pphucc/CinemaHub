using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaHub.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class updateFKMovieShowtime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_MovieID",
                table: "Showtimes");

            migrationBuilder.AddColumn<decimal>(
                name: "Point",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_MovieID",
                table: "Showtimes",
                column: "MovieID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Showtimes_MovieID",
                table: "Showtimes");

            migrationBuilder.DropColumn(
                name: "Point",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_MovieID",
                table: "Showtimes",
                column: "MovieID",
                unique: true);
        }
    }
}
