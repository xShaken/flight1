using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace flight.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DepartureTime",
                table: "Flights",
                newName: "EstimatedArrivalDateTime");

            migrationBuilder.RenameColumn(
                name: "ArrivalTime",
                table: "Flights",
                newName: "DepartureDateTime");

            migrationBuilder.AddColumn<string>(
                name: "AircraftCode",
                table: "Flights",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BusinessClassPrice",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BusinessSeatsAvailable",
                table: "Flights",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EconomyClassPrice",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EconomySeatsAvailable",
                table: "Flights",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FirstClassPrice",
                table: "Flights",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FirstClassSeatsAvailable",
                table: "Flights",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AircraftCode",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "BusinessClassPrice",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "BusinessSeatsAvailable",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "EconomyClassPrice",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "EconomySeatsAvailable",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "FirstClassPrice",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "FirstClassSeatsAvailable",
                table: "Flights");

            migrationBuilder.RenameColumn(
                name: "EstimatedArrivalDateTime",
                table: "Flights",
                newName: "DepartureTime");

            migrationBuilder.RenameColumn(
                name: "DepartureDateTime",
                table: "Flights",
                newName: "ArrivalTime");
        }
    }
}
