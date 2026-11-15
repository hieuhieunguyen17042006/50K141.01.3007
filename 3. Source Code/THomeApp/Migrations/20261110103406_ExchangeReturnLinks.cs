using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THomeApp.Migrations
{
    /// <inheritdoc />
    public partial class ExchangeReturnLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceVoucherLineId",
                table: "CT_Phieus",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CT_Phieus_SourceVoucherLineId",
                table: "CT_Phieus",
                column: "SourceVoucherLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_CT_Phieus_CT_Phieus_SourceVoucherLineId",
                table: "CT_Phieus",
                column: "SourceVoucherLineId",
                principalTable: "CT_Phieus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CT_Phieus_CT_Phieus_SourceVoucherLineId",
                table: "CT_Phieus");

            migrationBuilder.DropIndex(
                name: "IX_CT_Phieus_SourceVoucherLineId",
                table: "CT_Phieus");

            migrationBuilder.DropColumn(
                name: "SourceVoucherLineId",
                table: "CT_Phieus");
        }
    }
}
