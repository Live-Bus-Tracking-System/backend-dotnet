using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRegistrationNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegistrationNotes",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId1",
                table: "VehiclePermits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Organizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePermits_VehicleId1",
                table: "VehiclePermits",
                column: "VehicleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_VehiclePermits_Vehicles_VehicleId1",
                table: "VehiclePermits",
                column: "VehicleId1",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehiclePermits_Vehicles_VehicleId1",
                table: "VehiclePermits");

            migrationBuilder.DropIndex(
                name: "IX_VehiclePermits_VehicleId1",
                table: "VehiclePermits");

            migrationBuilder.DropColumn(
                name: "RegistrationNotes",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleId1",
                table: "VehiclePermits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Organizations");
        }
    }
}
