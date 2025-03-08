using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace flight.Migrations
{
    /// <inheritdoc />
    public partial class flow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnFlightId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Guest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsChild = table.Column<bool>(type: "bit", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guest_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ReturnFlightId",
                table: "Bookings",
                column: "ReturnFlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Guest_BookingId",
                table: "Guest",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Flights_ReturnFlightId",
                table: "Bookings",
                column: "ReturnFlightId",
                principalTable: "Flights",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Flights_ReturnFlightId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Guest");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ReturnFlightId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReturnFlightId",
                table: "Bookings");
        }
    }
}
