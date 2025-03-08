using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace flight.Migrations
{
    /// <inheritdoc />
    public partial class searchflight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NumberOfSeats",
                table: "Bookings",
                newName: "NumberOfChildren");

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartureDate",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "NumberOfAdults",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartureDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NumberOfAdults",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReturnDate",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "NumberOfChildren",
                table: "Bookings",
                newName: "NumberOfSeats");
        }
    }
}
