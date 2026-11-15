using System;
using System.Collections.Generic;
using System.Linq;
using THomeApp.Helpers;
using THomeApp.Models;
using Microsoft.EntityFrameworkCore;

namespace THomeApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(THomeDbContext context, IConfiguration configuration)
        {
            context.Database.Migrate();

            // 1. Seed NhanVien
            if (!context.NhanViens.Any())
            {
                string SeedPassword(string key) => !string.IsNullOrWhiteSpace(configuration[$"SeedAccounts:{key}"])
                    ? configuration[$"SeedAccounts:{key}"]!
                    : throw new InvalidOperationException($"Thiếu SeedAccounts:{key} để tạo tài khoản trên database mới. Xem README; không dùng mật khẩu mặc định trong mã nguồn.");
                var nhanViens = new List<NhanVien>
                {
                    new NhanVien
                    {
                        MaNV = "NV001",
                        HoTen = "Nguyễn Hữu Hiếu",
                        SDT = "0905111222",
                        DiaChi = "100 Hải Phòng, Hải Châu, Đà Nẵng",
                        CCCD = "048099001122",
                        ViTri = "Chủ cửa hàng",
                        TenDN = "admin",
                        MatKhau = PasswordHelper.HashPassword(SeedPassword("OwnerPassword")),
                        TrangThai = "Đang hoạt động",
                        NgayTao = DateTime.Now
                    },
                    new NhanVien
                    {
                        MaNV = "NV002",
                        HoTen = "Trần Ngọc Quỳnh Như",
                        SDT = "0905333444",
                        DiaChi = "45 Lê Lợi, Thạch Thang, Đà Nẵng",
                        CCCD = "048099002233",
                        ViTri = "Kế toán bán hàng",
                        TenDN = "ketoan",
                        MatKhau = PasswordHelper.HashPassword(SeedPassword("AccountantPassword")),
                        TrangThai = "Đang hoạt động",
                        NgayTao = DateTime.Now
                    },
                    new NhanVien
                    {
                        MaNV = "NV003",
                        HoTen = "Võ Nhật Minh",
                        SDT = "0905555666",
                        DiaChi = "28 Nguyễn Chí Thanh, Hải Châu, Đà Nẵng",
                        CCCD = "048099003344",
                        ViTri = "Nhân viên bán hàng",
                        TenDN = "nhanvien",
                        MatKhau = PasswordHelper.HashPassword(SeedPassword("SalesPassword")),
                        TrangThai = "Đang hoạt động",
                        NgayTao = DateTime.Now
                    },
                    new NhanVien
                    {
                        MaNV = "NV004",
                        HoTen = "Trần Anh Trí (Tài khoản thử nghiệm khóa)",
                        SDT = "0905777888",
                        DiaChi = "12 Pasteur, Hải Châu, Đà Nẵng",
                        CCCD = "048099004455",
                        ViTri = "Nhân viên bán hàng",
                        TenDN = "khoa01",
                        MatKhau = PasswordHelper.HashPassword(SeedPassword("LockedPassword")),
                        TrangThai = "Đã khóa",
                        NgayTao = DateTime.Now
                    }
                };
                context.NhanViens.AddRange(nhanViens);
                context.SaveChanges();
            }

            // 2. Seed ChietKhau (BR: từ 500 triệu là 2%, từ 1 tỷ là 3%)
            if (!context.ChietKhaus.Any())
            {
                var chietKhaus = new List<ChietKhau>
                {
                    new ChietKhau
                    {
                        MocTichLuy = 500_000_000m,
                        TyLeChietKhau = 2.00m,
                        DangApDung = true,
                        NgayTao = DateTime.Now
                    },
                    new ChietKhau
                    {
                        MocTichLuy = 1_000_000_000m,
                        TyLeChietKhau = 3.00m,
                        DangApDung = true,
                        NgayTao = DateTime.Now
                    }
                };
                context.ChietKhaus.AddRange(chietKhaus);
                context.SaveChanges();
            }

            // 3. Seed SanPham and Gia
            if (!context.SanPhams.Any())
            {
                var spList = new List<(string MaSP, string TenSP, string DanhMuc, string DVT, decimal Gia, string GhiChu)>
                {
                    ("SP001", "Bồn cầu thông minh T-Home Pro", "Thiết bị vệ sinh", "Bộ", 12500000m, "Tự động xả, sưởi ấm nắp ngồi, bảo hành 5 năm"),
                    ("SP002", "Sen cây tắm nhiệt độ T-Home Grand", "Thiết bị vệ sinh", "Bộ", 4800000m, "Đồng mạ Chrome bóng cao cấp, chống bám cặn"),
                    ("SP003", "Gạch ốp lát Granite Royal 80x80", "Gạch ốp lát", "Thùng", 420000m, "Men bóng vi tinh thể, chống trầy xước, 4 viên/thùng"),
                    ("SP004", "Bình nóng lạnh Ariston Pro 30L", "Bình nóng lạnh", "Chiếc", 3450000m, "Thanh đốt titan bền bỉ, chống giật ELCB"),
                    ("SP005", "Đèn chùm pha lê phòng khách T-Lux", "Hệ thống đèn điện", "Bộ", 8600000m, "Pha lê K9 cao cấp, đổi 3 màu ánh sáng"),
                    ("SP006", "Sơn nội thất cao cấp Dulux 18L", "Sơn nước", "Lon", 1850000m, "Chống ẩm mốc, lau chùi hiệu quả"),
                    ("SP007", "Lavabo âm bàn đá tự nhiên", "Thiết bị vệ sinh", "Cái", 2900000m, "Đá nhân tạo hoa văn cẩm thạch"),
                    ("SP008", "Keo chà ron cá sấu chống thấm", "Phụ kiện thi công", "Thùng", 380000m, "Kháng nước, độ đàn hồi cao"),
                    ("SP009", "Bộ thiết bị phòng tắm Luxury All-in-One", "Thiết bị vệ sinh", "Bộ", 25000000m, "Trọn gói tiện nghi chuẩn 5 sao")
                };

                foreach (var item in spList)
                {
                    var sp = new SanPham
                    {
                        MaSP = item.MaSP,
                        TenSP = item.TenSP,
                        DanhMuc = item.DanhMuc,
                        DonViTinh = item.DVT,
                        GhiChu = item.GhiChu,
                        IsDeleted = false,
                        NgayTao = DateTime.Now
                    };
                    context.SanPhams.Add(sp);
                    context.SaveChanges();

                    var gia = new Gia
                    {
                        MaSP = item.MaSP,
                        DonGia = item.Gia,
                        NgayApDung = DateTime.Now.AddDays(-30)
                    };
                    context.Gias.Add(gia);
                }
                context.SaveChanges();
            }

            // 4. Seed KhachHang
            if (!context.KhachHangs.Any())
            {
                var khachHangs = new List<KhachHang>
                {
                    new KhachHang
                    {
                        MaKH = "KH0001",
                        LoaiKhach = "Cá nhân",
                        HoTen = "Trần Văn Hùng",
                        SDT = "0912345678",
                        DiaChi = "45 Lê Duẩn, P. Hải Châu 1, Đà Nẵng",
                        GioiTinh = "Nam",
                        Email = "hungtran@gmail.com",
                        NgaySinh = new DateTime(1985, 6, 15),
                        GhiChu = "Khách hàng quen khu vực Hải Châu",
                        TongTichLuy = 120_000_000m,
                        NgayTao = DateTime.Now.AddDays(-60)
                    },
                    new KhachHang
                    {
                        MaKH = "KH0002",
                        LoaiKhach = "Doanh nghiệp",
                        HoTen = "Nguyễn Văn Đại",
                        SDT = "0905987654",
                        DiaChi = "120 Nguyễn Văn Linh, P. Nam Dương, Đà Nẵng",
                        GioiTinh = "Nam",
                        Email = "info@ankhangdecor.vn",
                        NgaySinh = new DateTime(1980, 11, 20),
                        TenCongTy = "Công ty TNHH Xây Dựng & Nội Thất An Khang",
                        MaSoThue = "0401234567",
                        GhiChu = "Nhà thầu nội thất công trình khách sạn - Đạt mốc ưu đãi 2%",
                        TongTichLuy = 560_000_000m,
                        NgayTao = DateTime.Now.AddDays(-120)
                    },
                    new KhachHang
                    {
                        MaKH = "KH0003",
                        LoaiKhach = "Doanh nghiệp",
                        HoTen = "Phạm Thị Bích",
                        SDT = "0988776655",
                        DiaChi = "88 Bạch Đằng, P. Hải Châu 1, Đà Nẵng",
                        GioiTinh = "Nữ",
                        Email = "contact@mientrungcorp.vn",
                        NgaySinh = new DateTime(1978, 3, 10),
                        TenCongTy = "Tập đoàn Đầu tư Địa ốc Miền Trung",
                        MaSoThue = "0409876543210",
                        GhiChu = "Đối tác chiến lược dự án cao cấp - Đạt mốc ưu đãi 3%",
                        TongTichLuy = 1_200_000_000m,
                        NgayTao = DateTime.Now.AddDays(-200)
                    },
                    new KhachHang
                    {
                        MaKH = "KH0004",
                        LoaiKhach = "Cá nhân",
                        HoTen = "Nguyễn Thị Mai",
                        SDT = "0935123456",
                        DiaChi = "12 Trần Phú, P. Thạch Thang, Đà Nẵng",
                        GioiTinh = "Nữ",
                        Email = "main@gmail.com",
                        NgaySinh = DateTime.Today, // Hôm nay sinh nhật để test thông báo!
                        GhiChu = "Sinh nhật hôm nay!",
                        TongTichLuy = 35_000_000m,
                        NgayTao = DateTime.Now.AddDays(-20)
                    }
                };
                context.KhachHangs.AddRange(khachHangs);
                context.SaveChanges();
            }

            // 5. Seed HoaDon & Chi tiết & LanThuTien
            if (!context.HoaDons.Any())
            {
                // CASE ĐẶC BIỆT THEO YÊU CẦU KIỂM TRA CỦA USER:
                // "Kiểm tra đơn 100 triệu, cọc 30 triệu, thu thêm 20 triệu, kết quả phải là đã thu 50 triệu, còn nợ 50 triệu."
                var hdCongTrinh = new HoaDon
                {
                    MaHD = "CT20261110001",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-5),
                    TrangThaiThanhToan = "Đã đặt cọc",
                    PhuongThucThanhToan = "Chuyển khoản",
                    DiaChiGiao = "Công trình Biệt thự Euro Village 2, Hòa Xuân, Cẩm Lệ, Đà Nẵng",
                    ChiPhiVanChuyen = 0,
                    TienThue = 0,
                    TienDatCoc = 30_000_000m,
                    TyLeDatCoc = 30.00m,
                    HanThanhToan = DateTime.Now.AddDays(25),
                    TienChietKhau = 0,
                    TongTienHang = 100_000_000m,
                    TongThanhToan = 100_000_000m,
                    DaThu = 50_000_000m, // Cọc 30tr + thu thêm 20tr = 50tr
                    DaHoanKhach = 0,
                    SoLanGhiDe = 0,
                    IsLockedGhiDe = false,
                    GhiChu = "Đơn công trình 100 triệu: cọc 30 triệu (30%), đã thu thêm đợt 2: 20 triệu. Hiện tại đã thu 50 triệu, còn nợ 50 triệu",
                    MaKH = "KH0002",
                    MaNV = "NV003"
                };
                context.HoaDons.Add(hdCongTrinh);
                context.SaveChanges();

                // Chi tiết cho hdCongTrinh: 4 bộ Thiết bị Luxury (25 triệu * 4 = 100 triệu)
                context.CT_HoaDons.Add(new CT_HoaDon
                {
                    MaHD = hdCongTrinh.MaHD,
                    MaSP = "SP009",
                    DonGiaBan = 25_000_000m,
                    SoLuong = 4,
                    SoLuongDaTra = 0
                });

                // Lần thu 1: Đặt cọc 30 triệu
                context.LanThuTiens.Add(new LanThuTien
                {
                    MaHD = hdCongTrinh.MaHD,
                    NgayThu = DateTime.Now.AddDays(-5),
                    SoTienThu = 30_000_000m,
                    PhuongThucThanhToan = "Chuyển khoản",
                    NoiDung = "Thu tiền đặt cọc (30%) ký hợp đồng cung cấp vật tư",
                    MaNVThu = "NV003",
                    TrangThai = "Hiệu lực"
                });

                // Lần thu 2: Thu thêm 20 triệu
                context.LanThuTiens.Add(new LanThuTien
                {
                    MaHD = hdCongTrinh.MaHD,
                    NgayThu = DateTime.Now.AddDays(-2),
                    SoTienThu = 20_000_000m,
                    PhuongThucThanhToan = "Chuyển khoản",
                    NoiDung = "Thu thêm tiền đợt 2 sau khi giao đợt 1",
                    MaNVThu = "NV002",
                    TrangThai = "Hiệu lực"
                });

                // Đợt giao hàng 1: Giao 2 bộ
                var dot1 = new DotGiaoHang
                {
                    MaHD = hdCongTrinh.MaHD,
                    DotThu = 1,
                    NgayGiao = DateTime.Now.AddDays(-3),
                    NguoiGiao = "Tài xế Nguyễn Văn Bảy (Xe tải T-Home)",
                    TrangThaiGiao = "Đã giao thành công",
                    GhiChu = "Giao trước 2 bộ tầng 1 & tầng 2 theo tiến độ thầu"
                };
                context.DotGiaoHangs.Add(dot1);
                context.SaveChanges();

                context.CT_DotGiaoHangs.Add(new CT_DotGiaoHang
                {
                    MaDotGiao = dot1.MaDotGiao,
                    MaSP = "SP009",
                    SoLuongGiao = 2
                });

                // Đợt giao hàng 2: Còn lại 2 bộ chờ giao
                var dot2 = new DotGiaoHang
                {
                    MaHD = hdCongTrinh.MaHD,
                    DotThu = 2,
                    NgayGiao = DateTime.Now.AddDays(7),
                    NguoiGiao = "Tài xế Nguyễn Văn Bảy",
                    TrangThaiGiao = "Chờ giao",
                    GhiChu = "Dự kiến giao tiếp 2 bộ còn lại sau khi thầu hoàn thiện trát tường tầng 3"
                };
                context.DotGiaoHangs.Add(dot2);
                context.SaveChanges();

                context.CT_DotGiaoHangs.Add(new CT_DotGiaoHang
                {
                    MaDotGiao = dot2.MaDotGiao,
                    MaSP = "SP009",
                    SoLuongGiao = 2
                });

                // Đơn tạm tính 1: Đang chờ thanh toán
                var tt01 = new HoaDon
                {
                    MaHD = "TT20261110001",
                    LoaiChungTu = "DonTamTinh",
                    NgayTao = DateTime.Now.AddHours(-3),
                    TrangThaiThanhToan = "Chưa thanh toán",
                    PhuongThucThanhToan = "Tiền mặt",
                    DiaChiGiao = "45 Lê Duẩn, Đà Nẵng",
                    ChiPhiVanChuyen = 50_000m,
                    TienThue = 0,
                    TienDatCoc = 0,
                    TyLeDatCoc = 0,
                    TienChietKhau = 0,
                    TongTienHang = 12_500_000m,
                    TongThanhToan = 12_550_000m,
                    DaThu = 0,
                    DaHoanKhach = 0,
                    SoLanGhiDe = 0,
                    GhiChu = "Khách chọn mua bồn cầu thông minh",
                    MaKH = "KH0001",
                    MaNV = "NV003"
                };
                context.HoaDons.Add(tt01);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = tt01.MaHD, MaSP = "SP001", DonGiaBan = 12_500_000m, SoLuong = 1 });

                // Đơn tạm tính 2: Đã thanh toán (Sẵn sàng tạo hóa đơn chính thức UC_2.3)
                var tt02 = new HoaDon
                {
                    MaHD = "TT20261110002",
                    LoaiChungTu = "DonTamTinh",
                    NgayTao = DateTime.Now.AddHours(-2),
                    TrangThaiThanhToan = "Đã thanh toán",
                    PhuongThucThanhToan = "Chuyển khoản",
                    DiaChiGiao = "12 Trần Phú, Đà Nẵng",
                    ChiPhiVanChuyen = 100_000m,
                    TienThue = 0,
                    TienDatCoc = 0,
                    TyLeDatCoc = 0,
                    TienChietKhau = 0,
                    TongTienHang = 8_250_000m, // 1 sen tam (4.8tr) + 1 binh nong lanh (3.45tr) = 8.25tr
                    TongThanhToan = 8_350_000m,
                    DaThu = 8_350_000m,
                    DaHoanKhach = 0,
                    SoLanGhiDe = 0,
                    GhiChu = "Đã nhận tiền qua QR VietQR Techcombank",
                    MaKH = "KH0004",
                    MaNV = "NV003"
                };
                context.HoaDons.Add(tt02);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = tt02.MaHD, MaSP = "SP002", DonGiaBan = 4_800_000m, SoLuong = 1 });
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = tt02.MaHD, MaSP = "SP004", DonGiaBan = 3_450_000m, SoLuong = 1 });
                context.LanThuTiens.Add(new LanThuTien
                {
                    MaHD = tt02.MaHD,
                    NgayThu = DateTime.Now.AddHours(-2),
                    SoTienThu = 8_350_000m,
                    PhuongThucThanhToan = "Chuyển khoản",
                    NoiDung = "Thanh toán toàn bộ đơn tạm tính TT20261110002",
                    MaNVThu = "NV003",
                    TrangThai = "Hiệu lực"
                });

                // Hóa đơn chính thức 1 (Đã hoàn thành)
                var hd01 = new HoaDon
                {
                    MaHD = "HD20261109001",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-1),
                    TrangThaiThanhToan = "Hoàn thành",
                    PhuongThucThanhToan = "Tiền mặt",
                    DiaChiGiao = "45 Lê Duẩn, Đà Nẵng",
                    ChiPhiVanChuyen = 80_000m,
                    TienThue = 0,
                    TienDatCoc = 0,
                    TyLeDatCoc = 0,
                    TienChietKhau = 0,
                    TongTienHang = 17_300_000m,
                    TongThanhToan = 17_380_000m,
                    DaThu = 17_380_000m,
                    DaHoanKhach = 0,
                    SoLanGhiDe = 0,
                    GhiChu = "Giao hàng tận nơi, khách kiểm tra ưng ý",
                    MaKH = "KH0001",
                    MaNV = "NV003"
                };
                context.HoaDons.Add(hd01);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hd01.MaHD, MaSP = "SP001", DonGiaBan = 12_500_000m, SoLuong = 1 });
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hd01.MaHD, MaSP = "SP002", DonGiaBan = 4_800_000m, SoLuong = 1 });

                // Các hóa đơn để kiểm thử các mốc ghi đè UC_2.6 (0, 1, 3, 5, 7):
                // 1. Mốc 1 lần ghi đè (Không cảnh báo)
                var hdGD01 = new HoaDon
                {
                    MaHD = "HD20261108001",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-2),
                    TrangThaiThanhToan = "Hoàn thành",
                    PhuongThucThanhToan = "Tiền mặt",
                    DiaChiGiao = "12 Trần Phú, Đà Nẵng",
                    ChiPhiVanChuyen = 0,
                    TienThue = 0,
                    TongTienHang = 4_800_000m,
                    TongThanhToan = 4_800_000m,
                    DaThu = 4_800_000m,
                    SoLanGhiDe = 1,
                    GhiChu = "Hóa đơn thử nghiệm ghi đè 1 lần (Không cảnh báo)",
                    MaKH = "KH0004",
                    MaNV = "NV003"
                };
                context.HoaDons.Add(hdGD01);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hdGD01.MaHD, MaSP = "SP002", DonGiaBan = 4_800_000m, SoLuong = 1 });

                // 2. Mốc 3 lần ghi đè (Cảnh báo vàng 2-4 lần)
                var hdGD02 = new HoaDon
                {
                    MaHD = "HD20261108002",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-3),
                    TrangThaiThanhToan = "Hoàn thành",
                    PhuongThucThanhToan = "Chuyển khoản",
                    DiaChiGiao = "45 Lê Duẩn, Đà Nẵng",
                    ChiPhiVanChuyen = 0,
                    TienThue = 0,
                    TongTienHang = 8_400_000m,
                    TongThanhToan = 8_400_000m,
                    DaThu = 8_400_000m,
                    SoLanGhiDe = 3,
                    GhiChu = "Hóa đơn thử nghiệm ghi đè 3 lần (Cảnh báo vàng: 2-4 lần)",
                    MaKH = "KH0001",
                    MaNV = "NV002"
                };
                context.HoaDons.Add(hdGD02);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hdGD02.MaHD, MaSP = "SP003", DonGiaBan = 420_000m, SoLuong = 20 });

                // 3. Mốc 5 lần ghi đè (Cảnh báo đỏ 5-6 lần)
                var hdGD03 = new HoaDon
                {
                    MaHD = "HD20261107001",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-4),
                    TrangThaiThanhToan = "Hoàn thành",
                    PhuongThucThanhToan = "Tiền mặt",
                    DiaChiGiao = "120 Nguyễn Văn Linh, Đà Nẵng",
                    ChiPhiVanChuyen = 0,
                    TienThue = 0,
                    TongTienHang = 7_400_000m,
                    TongThanhToan = 7_400_000m,
                    DaThu = 7_400_000m,
                    SoLanGhiDe = 5,
                    GhiChu = "Hóa đơn thử nghiệm ghi đè 5 lần (Cảnh báo đỏ: 5-6 lần)",
                    MaKH = "KH0002",
                    MaNV = "NV002"
                };
                context.HoaDons.Add(hdGD03);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hdGD03.MaHD, MaSP = "SP006", DonGiaBan = 1_850_000m, SoLuong = 4 });

                // 4. Mốc 7 lần ghi đè (Bị khóa quyền ghi đè, có nút xin mở khóa gửi chủ cửa hàng)
                var hdGD04 = new HoaDon
                {
                    MaHD = "HD20261106001",
                    LoaiChungTu = "HoaDon",
                    NgayTao = DateTime.Now.AddDays(-6),
                    TrangThaiThanhToan = "Hoàn thành",
                    PhuongThucThanhToan = "Chuyển khoản",
                    DiaChiGiao = "88 Bạch Đằng, Đà Nẵng",
                    ChiPhiVanChuyen = 0,
                    TienThue = 0,
                    TongTienHang = 17_200_000m,
                    TongThanhToan = 17_200_000m,
                    DaThu = 17_200_000m,
                    SoLanGhiDe = 7,
                    IsLockedGhiDe = true,
                    GhiChu = "Hóa đơn đã ghi đè 7 lần - Bị khóa thao tác ghi đè theo BR-UC2.6-01",
                    MaKH = "KH0003",
                    MaNV = "NV002"
                };
                context.HoaDons.Add(hdGD04);
                context.SaveChanges();
                context.CT_HoaDons.Add(new CT_HoaDon { MaHD = hdGD04.MaHD, MaSP = "SP005", DonGiaBan = 8_600_000m, SoLuong = 2 });

                // Thêm 1 yêu cầu mở khóa đang chờ duyệt cho HD20261106001
                context.YeuCauMoKhoas.Add(new YeuCauMoKhoa
                {
                    MaHD = hdGD04.MaHD,
                    MaNV = "NV002",
                    LyDoMoKhoa = "Khách hàng dự án yêu cầu điều chỉnh thêm 1 bộ đèn pha lê cho sảnh phụ, kính mong Chủ cửa hàng xét duyệt mở khóa ghi đè.",
                    ThoiGianGui = DateTime.Now.AddHours(-4),
                    TrangThai = "Chờ duyệt"
                });
                context.SaveChanges();

                // 6. Ghi nhật ký hoạt động khởi tạo
                context.LichSuHoatDongs.Add(new LichSuHoatDong
                {
                    ThoiGianThaoTac = DateTime.Now.AddDays(-6),
                    LoaiDoiTuong = "HeThong",
                    NoiDungThaoTac = "Khởi tạo hệ thống cơ sở dữ liệu T-HOME và các danh mục ban đầu",
                    LyDo = "Cài đặt ứng dụng lần đầu",
                    MaNV = "NV001"
                });
                context.SaveChanges();
            }
        }
    }
}
