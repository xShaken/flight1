using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace flight.Migrations
{
    /// <inheritdoc />
    public partial class drop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketPrice",
                table: "Flights");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TicketPrice",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
