using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace THomeApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChietKhaus",
                columns: table => new
                {
                    MaCK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MocTichLuy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TyLeChietKhau = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DangApDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChietKhaus", x => x.MaCK);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangs",
                columns: table => new
                {
                    MaKH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoaiKhach = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaSoThue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenCongTy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TongTichLuy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangs", x => x.MaKH);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ViTri = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenDN = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.MaNV);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    MaSP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenSP = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.MaSP);
                });

            migrationBuilder.CreateTable(
                name: "HoaDons",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoaiChungTu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThaiThanhToan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiaChiGiao = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChiPhiVanChuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienThue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienDatCoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TyLeDatCoc = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HanThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TienChietKhau = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongTienHang = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongThanhToan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaHoanKhach = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLanGhiDe = table.Column<int>(type: "int", nullable: false),
                    IsLockedGhiDe = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaKH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaCK = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDons", x => x.MaHD);
                    table.ForeignKey(
                        name: "FK_HoaDons_ChietKhaus_MaCK",
                        column: x => x.MaCK,
                        principalTable: "ChietKhaus",
                        principalColumn: "MaCK",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HoaDons_KhachHangs_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KhachHangs",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoaDons_NhanViens_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KiemKeTienMats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayKiemKe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaLamViec = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TienMatPhanMem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienMatKiemDem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KiemKeTienMats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KiemKeTienMats_NhanViens_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuHoatDongs",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiGianThaoTac = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiDoiTuong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDungThaoTac = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuHoatDongs", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuHoatDongs_NhanViens_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gias",
                columns: table => new
                {
                    MaGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayApDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaSP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gias", x => x.MaGia);
                    table.ForeignKey(
                        name: "FK_Gias_SanPhams_MaSP",
                        column: x => x.MaSP,
                        principalTable: "SanPhams",
                        principalColumn: "MaSP",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CT_HoaDons",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaSP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DonGiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    SoLuongDaTra = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CT_HoaDons", x => new { x.MaHD, x.MaSP });
                    table.ForeignKey(
                        name: "FK_CT_HoaDons_HoaDons_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HoaDons",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CT_HoaDons_SanPhams_MaSP",
                        column: x => x.MaSP,
                        principalTable: "SanPhams",
                        principalColumn: "MaSP",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DotGiaoHangs",
                columns: table => new
                {
                    MaDotGiao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DotThu = table.Column<int>(type: "int", nullable: false),
                    NgayGiao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiGiao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThaiGiao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotGiaoHangs", x => x.MaDotGiao);
                    table.ForeignKey(
                        name: "FK_DotGiaoHangs_HoaDons_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HoaDons",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LanThuTiens",
                columns: table => new
                {
                    MaLanThu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayThu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoTienThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaNVThu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanThuTiens", x => x.MaLanThu);
                    table.ForeignKey(
                        name: "FK_LanThuTiens_HoaDons_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HoaDons",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanThuTiens_NhanViens_MaNVThu",
                        column: x => x.MaNVThu,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Phieus",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoaiPhieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoaiChiPhiPhatSinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HinhThucXuLyDoiTra = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TienChenhLech = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienThucHoan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoLanGhiDeHienTai = table.Column<int>(type: "int", nullable: false),
                    DuLieuGocJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DuLieuMoiJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Phieus", x => x.MaPhieu);
                    table.ForeignKey(
                        name: "FK_Phieus_HoaDons_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HoaDons",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Phieus_NhanViens_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauMoKhoas",
                columns: table => new
                {
                    MaYC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHD = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LyDoMoKhoa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThoiGianGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThoiGianDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaNVDuyet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GhiChuDuyet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauMoKhoas", x => x.MaYC);
                    table.ForeignKey(
                        name: "FK_YeuCauMoKhoas_HoaDons_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HoaDons",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauMoKhoas_NhanViens_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauMoKhoas_NhanViens_MaNVDuyet",
                        column: x => x.MaNVDuyet,
                        principalTable: "NhanViens",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CT_DotGiaoHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDotGiao = table.Column<int>(type: "int", nullable: false),
                    MaSP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoLuongGiao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CT_DotGiaoHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CT_DotGiaoHangs_DotGiaoHangs_MaDotGiao",
                        column: x => x.MaDotGiao,
                        principalTable: "DotGiaoHangs",
                        principalColumn: "MaDotGiao",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CT_DotGiaoHangs_SanPhams_MaSP",
                        column: x => x.MaSP,
                        principalTable: "SanPhams",
                        principalColumn: "MaSP",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CT_Phieus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaSP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LoaiDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGiaXuLy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TinhTrangHang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CT_Phieus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CT_Phieus_Phieus_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "Phieus",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CT_Phieus_SanPhams_MaSP",
                        column: x => x.MaSP,
                        principalTable: "SanPhams",
                        principalColumn: "MaSP",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CT_DotGiaoHangs_MaDotGiao",
                table: "CT_DotGiaoHangs",
                column: "MaDotGiao");

            migrationBuilder.CreateIndex(
                name: "IX_CT_DotGiaoHangs_MaSP",
                table: "CT_DotGiaoHangs",
                column: "MaSP");

            migrationBuilder.CreateIndex(
                name: "IX_CT_HoaDons_MaSP",
                table: "CT_HoaDons",
                column: "MaSP");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Phieus_MaPhieu",
                table: "CT_Phieus",
                column: "MaPhieu");

            migrationBuilder.CreateIndex(
                name: "IX_CT_Phieus_MaSP",
                table: "CT_Phieus",
                column: "MaSP");

            migrationBuilder.CreateIndex(
                name: "IX_DotGiaoHangs_MaHD",
                table: "DotGiaoHangs",
                column: "MaHD");

            migrationBuilder.CreateIndex(
                name: "IX_Gias_MaSP",
                table: "Gias",
                column: "MaSP");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaCK",
                table: "HoaDons",
                column: "MaCK");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaKH",
                table: "HoaDons",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_MaNV",
                table: "HoaDons",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangs_SDT",
                table: "KhachHangs",
                column: "SDT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KiemKeTienMats_MaNV",
                table: "KiemKeTienMats",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_LanThuTiens_MaHD",
                table: "LanThuTiens",
                column: "MaHD");

            migrationBuilder.CreateIndex(
                name: "IX_LanThuTiens_MaNVThu",
                table: "LanThuTiens",
                column: "MaNVThu");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuHoatDongs_MaNV",
                table: "LichSuHoatDongs",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_NhanViens_TenDN",
                table: "NhanViens",
                column: "TenDN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Phieus_MaHD",
                table: "Phieus",
                column: "MaHD");

            migrationBuilder.CreateIndex(
                name: "IX_Phieus_MaNV",
                table: "Phieus",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauMoKhoas_MaHD",
                table: "YeuCauMoKhoas",
                column: "MaHD");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauMoKhoas_MaNV",
                table: "YeuCauMoKhoas",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauMoKhoas_MaNVDuyet",
                table: "YeuCauMoKhoas",
                column: "MaNVDuyet");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CT_DotGiaoHangs");

            migrationBuilder.DropTable(
                name: "CT_HoaDons");

            migrationBuilder.DropTable(
                name: "CT_Phieus");

            migrationBuilder.DropTable(
                name: "Gias");

            migrationBuilder.DropTable(
                name: "KiemKeTienMats");

            migrationBuilder.DropTable(
                name: "LanThuTiens");

            migrationBuilder.DropTable(
                name: "LichSuHoatDongs");

            migrationBuilder.DropTable(
                name: "YeuCauMoKhoas");

            migrationBuilder.DropTable(
                name: "DotGiaoHangs");

            migrationBuilder.DropTable(
                name: "Phieus");

            migrationBuilder.DropTable(
                name: "SanPhams");

            migrationBuilder.DropTable(
                name: "HoaDons");

            migrationBuilder.DropTable(
                name: "ChietKhaus");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "NhanViens");
        }
    }
}
