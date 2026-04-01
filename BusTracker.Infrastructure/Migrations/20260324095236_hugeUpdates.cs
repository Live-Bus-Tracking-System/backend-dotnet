using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class hugeUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Organizations_OrganizationId",
                table: "Routes");

            migrationBuilder.DropForeignKey(
                name: "FK_Stops_Organizations_OrganizationId",
                table: "Stops");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Stops",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "DataOrigin",
                table: "Stops",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlobal",
                table: "Stops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Routes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "DataOrigin",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoverned",
                table: "Routes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ComplianceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuedAtDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresAtDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehiclePermits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermitStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    PermitNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePermits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehiclePermits_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePermits_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehiclePermits_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDocuments_EntityType_EntityId",
                table: "ComplianceDocuments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDocuments_EntityType_EntityId_DocumentType",
                table: "ComplianceDocuments",
                columns: new[] { "EntityType", "EntityId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePermits_OrganizationId",
                table: "VehiclePermits",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePermits_PermitStatus",
                table: "VehiclePermits",
                column: "PermitStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePermits_RouteId",
                table: "VehiclePermits",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePermits_VehicleId_RouteId",
                table: "VehiclePermits",
                columns: new[] { "VehicleId", "RouteId" },
                unique: true,
                filter: "[RouteId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Organizations_OrganizationId",
                table: "Routes",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_Organizations_OrganizationId",
                table: "Stops",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Organizations_OrganizationId",
                table: "Routes");

            migrationBuilder.DropForeignKey(
                name: "FK_Stops_Organizations_OrganizationId",
                table: "Stops");

            migrationBuilder.DropTable(
                name: "ComplianceDocuments");

            migrationBuilder.DropTable(
                name: "VehiclePermits");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "IsGlobal",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "DataOrigin",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "IsGoverned",
                table: "Routes");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Stops",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                table: "Routes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Organizations_OrganizationId",
                table: "Routes",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_Organizations_OrganizationId",
                table: "Stops",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
