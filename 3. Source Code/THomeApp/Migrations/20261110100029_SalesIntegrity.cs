using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THomeApp.Migrations
{
    /// <inheritdoc />
    public partial class SalesIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TienCanTru",
                table: "Phieus",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TienPhaiHoan",
                table: "Phieus",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaHoaDon",
                table: "HoaDons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayPhatHanh",
                table: "HoaDons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaHDGoc",
                table: "CT_Phieus",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaSPGoc",
                table: "CT_Phieus",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestOperations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RedirectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaHoaDon",
                table: "HoaDons",
                column: "MaHoaDon",
                unique: true,
                filter: "[MaHoaDon] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Phieus_MaHDGoc_MaSPGoc",
                table: "CT_Phieus",
                columns: new[] { "MaHDGoc", "MaSPGoc" });

            migrationBuilder.AddForeignKey(
                name: "FK_CT_Phieus_CT_HoaDons_MaHDGoc_MaSPGoc",
                table: "CT_Phieus",
                columns: new[] { "MaHDGoc", "MaSPGoc" },
                principalTable: "CT_HoaDons",
                principalColumns: new[] { "MaHD", "MaSP" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CT_Phieus_CT_HoaDons_MaHDGoc_MaSPGoc",
                table: "CT_Phieus");

            migrationBuilder.DropTable(
                name: "RequestOperations");

            migrationBuilder.DropIndex(
                name: "IX_HoaDons_MaHoaDon",
                table: "HoaDons");

            migrationBuilder.DropIndex(
                name: "IX_CT_Phieus_MaHDGoc_MaSPGoc",
                table: "CT_Phieus");

            migrationBuilder.DropColumn(
                name: "TienCanTru",
                table: "Phieus");

            migrationBuilder.DropColumn(
                name: "TienPhaiHoan",
                table: "Phieus");

            migrationBuilder.DropColumn(
                name: "MaHoaDon",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "NgayPhatHanh",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "MaHDGoc",
                table: "CT_Phieus");

            migrationBuilder.DropColumn(
                name: "MaSPGoc",
                table: "CT_Phieus");
        }
    }
}
