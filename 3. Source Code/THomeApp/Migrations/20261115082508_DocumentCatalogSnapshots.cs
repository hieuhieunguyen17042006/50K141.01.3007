using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THomeApp.Migrations
{
    /// <inheritdoc />
    public partial class DocumentCatalogSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuLieuThayDoiJson",
                table: "LichSuHoatDongs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonViTinhDaChot",
                table: "CT_Phieus",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenSPDaChot",
                table: "CT_Phieus",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonViTinhDaChot",
                table: "CT_HoaDons",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenSPDaChot",
                table: "CT_HoaDons",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Preserve the labels available at upgrade time. This does not reconstruct
            // catalog names from an unknown historical state before this migration.
            migrationBuilder.Sql("EXEC(N'UPDATE c SET TenSPDaChot=s.TenSP, DonViTinhDaChot=s.DonViTinh FROM CT_HoaDons c JOIN SanPhams s ON s.MaSP=c.MaSP WHERE c.TenSPDaChot IS NULL;');");
            migrationBuilder.Sql("EXEC(N'UPDATE c SET TenSPDaChot=s.TenSP, DonViTinhDaChot=s.DonViTinh FROM CT_Phieus c JOIN SanPhams s ON s.MaSP=c.MaSP WHERE c.TenSPDaChot IS NULL;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuLieuThayDoiJson",
                table: "LichSuHoatDongs");

            migrationBuilder.DropColumn(
                name: "DonViTinhDaChot",
                table: "CT_Phieus");

            migrationBuilder.DropColumn(
                name: "TenSPDaChot",
                table: "CT_Phieus");

            migrationBuilder.DropColumn(
                name: "DonViTinhDaChot",
                table: "CT_HoaDons");

            migrationBuilder.DropColumn(
                name: "TenSPDaChot",
                table: "CT_HoaDons");
        }
    }
}
