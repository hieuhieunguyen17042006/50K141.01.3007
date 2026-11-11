using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Helpers;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers
{
    [Authorize]
    public partial class BanHangController : Controller
    {
        private const string InvoiceRoles = "Chủ cửa hàng,Kế toán bán hàng,Nhân viên bán hàng";
        private readonly THomeDbContext _context;
        private readonly IActivityLogger _logger;

        public BanHangController(THomeDbContext context, IActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // 1. DANH SÁCH BÁN HÀNG (Tab Đơn tạm tính & Hóa đơn)
        // ==========================================
        public async Task<IActionResult> Index(string tab = "dontamtinh", string? search = null, string? status = null)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            ViewBag.ActiveTab = tab.ToLower();
            ViewBag.Search = search;
            status = status is SalesListStatus.CompletedLabel or SalesListStatus.IncompleteLabel ? status : null;
            ViewBag.Status = status;
            ViewBag.UserRole = userRole;

            IQueryable<HoaDon> query = _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.SanPham)
                .Include(h => h.NhanVien)
                .OrderByDescending(h => h.NgayTao);

            if (tab.ToLower() == "hoadon")
            {
                query = query.Where(h => h.LoaiChungTu == "HoaDon");
            }
            else
            {
                query = query.Where(h => h.LoaiChungTu == "DonTamTinh");
            }

            // Bộ lọc trạng thái
            if (status == SalesListStatus.CompletedLabel)
                query = query.Where(SalesListStatus.Completed);
            else if (status == SalesListStatus.IncompleteLabel)
                query = query.Where(SalesListStatus.Incomplete);

            // Ô tìm kiếm đa năng (BR-UC2.4-01)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(h =>
                    h.MaHD.ToLower().Contains(s) || (h.MaHoaDon != null && h.MaHoaDon.ToLower().Contains(s)) ||
                    (h.KhachHang != null && h.KhachHang.HoTen.ToLower().Contains(s)) ||
                    (h.KhachHang != null && h.KhachHang.SDT.ToLower().Contains(s)) ||
                    (h.KhachHang != null && h.KhachHang.TenCongTy != null && h.KhachHang.TenCongTy.ToLower().Contains(s))
                );
            }

            var list = await query.ToListAsync();
            ViewBag.IssuedCount = await _context.HoaDons.CountAsync(h => h.LoaiChungTu == "HoaDon");
            return View(list);
        }

        // ==========================================
        // 2. TẠO HÓA ĐƠN TẠM TÍNH (UC_2.1)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TaoDonTamTinh()
        {
            ViewBag.KhachHangs = await _context.KhachHangs.OrderBy(k => k.HoTen).ToListAsync();

            // Lấy danh sách sản phẩm cùng đơn giá mới nhất
            var sanPhams = await _context.SanPhams
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    s.MaSP,
                    s.TenSP,
                    s.DanhMuc,
                    s.DonViTinh,
                    DonGia = _context.Gias
                        .Where(g => g.MaSP == s.MaSP && g.NgayApDung <= DateTime.Now)
                        .OrderByDescending(g => g.NgayApDung).ThenByDescending(g => g.MaGia)
                        .Select(g => g.DonGia)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.SanPhams = sanPhams;
            ViewBag.ChietKhaus = await _context.ChietKhaus.Where(c => c.DangApDung).OrderBy(c => c.MocTichLuy).ToListAsync();

            return View();
        }

        public class TaoDonTamTinhRequest
        {
            public string MaKH { get; set; } = string.Empty;
            public string? DiaChiGiao { get; set; }
            public string? GhiChu { get; set; }
            public decimal ChiPhiVanChuyen { get; set; }
            public decimal TienThue { get; set; }
            public bool ApDungVAT { get; set; }
            public bool IsDatCoc { get; set; }
            public decimal TyLeDatCoc { get; set; }
            public decimal TienDatCoc { get; set; }
            public DateTime? HanThanhToan { get; set; }
            public string PhuongThucThanhToan { get; set; } = "Tiền mặt";
            public List<ChiTietSPItem> SanPhams { get; set; } = new();
        }

        public class ChiTietSPItem
        {
            public int? SourceVoucherLineId { get; set; }
            public string MaSP { get; set; } = string.Empty;
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuDonTamTinh([FromBody] TaoDonTamTinhRequest model)
        {
            if (model == null || string.IsNullOrEmpty(model.MaKH) || model.SanPhams == null || !model.SanPhams.Any())
            {
                return Json(new { success = false, message = "Thông tin không hợp lệ! Vui lòng chọn khách hàng và ít nhất 1 sản phẩm." });
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";
            var khachHang = await _context.KhachHangs.FindAsync(model.MaKH);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Khách hàng không tồn tại!" });
            }

            if (model.ChiPhiVanChuyen < 0 || model.TienThue < 0 || model.SanPhams.Count > 100
                || model.SanPhams.Any(x => x.SoLuong <= 0 || x.SoLuong > 100000)
                || model.SanPhams.Select(x => x.MaSP).Distinct().Count() != model.SanPhams.Count
                || !ValidPayment(model.PhuongThucThanhToan))
                return Json(new { success = false, message = "Số lượng, chi phí hoặc phương thức thanh toán không hợp lệ. Mỗi sản phẩm chỉ chọn một dòng." });

            // The server supplies the effective price. Client prices are never trusted.
            foreach (var item in model.SanPhams)
            {
                var product = await _context.SanPhams.FindAsync(item.MaSP);
                var price = await _context.Gias.Where(g => g.MaSP == item.MaSP && g.NgayApDung <= DateTime.Now)
                    .OrderByDescending(g => g.NgayApDung).ThenByDescending(g => g.MaGia).FirstOrDefaultAsync();
                if (product == null || product.IsDeleted || price == null || price.DonGia <= 0)
                    return Json(new { success = false, message = "Sản phẩm không còn bán hoặc chưa có giá hiệu lực." });
                item.DonGia = price.DonGia;
            }
            decimal tongTienHang = model.SanPhams.Sum(x => x.DonGia * x.SoLuong);
            var discount = await _context.ChietKhaus.Where(c => c.DangApDung && c.MocTichLuy <= khachHang.TongTichLuy)
                .OrderByDescending(c => c.MocTichLuy).FirstOrDefaultAsync();
            decimal tyLeCK = discount?.TyLeChietKhau ?? 0;
            int? maCK = discount?.MaCK;

            decimal tienChietKhau = Math.Round(tongTienHang * (tyLeCK / 100m), 0);
            model.TienThue = model.ApDungVAT ? Math.Round((tongTienHang - tienChietKhau) * 0.1m, 0) : 0;
            decimal tongThanhToan = tongTienHang - tienChietKhau + model.TienThue + model.ChiPhiVanChuyen;

            // Xử lý đặt cọc (30% - 50%)
            decimal tienDatCoc = 0;
            if (model.IsDatCoc)
            {
                if (model.TyLeDatCoc < 30 || model.TyLeDatCoc > 100)
                {
                    return Json(new { success = false, message = "Tỷ lệ đặt cọc đơn công trình phải từ 30% đến 100%!" });
                }
                tienDatCoc = Math.Round(tongThanhToan * (model.TyLeDatCoc / 100m), 0);
            }

            if (tongThanhToan <= 0 || tongThanhToan > 1_000_000_000_000m)
                return Json(new { success = false, message = "Tổng thanh toán ngoài giới hạn hợp lệ." });

            // Tự sinh mã đơn tạm tính: TT + yyyyMMdd + 4 số ngẫu nhiên
            string maHD = NewCode("TT");

            var donTamTinh = new HoaDon
            {
                MaHD = maHD,
                LoaiChungTu = "DonTamTinh",
                NgayTao = DateTime.Now,
                TrangThaiThanhToan = "Chưa thanh toán",
                PhuongThucThanhToan = model.PhuongThucThanhToan,
                DiaChiGiao = model.DiaChiGiao ?? khachHang.DiaChi,
                ChiPhiVanChuyen = model.ChiPhiVanChuyen,
                TienThue = model.TienThue,
                TienDatCoc = tienDatCoc,
                TyLeDatCoc = model.IsDatCoc ? model.TyLeDatCoc : 0,
                HanThanhToan = model.HanThanhToan,
                TienChietKhau = tienChietKhau,
                TongTienHang = tongTienHang,
                TongThanhToan = tongThanhToan,
                DaThu = 0, // Đơn tạm tính chưa thu tiền thì DaThu = 0
                DaHoanKhach = 0,
                SoLanGhiDe = 0,
                GhiChu = model.GhiChu,
                MaKH = model.MaKH,
                MaNV = maNV,
                MaCK = maCK
            };

            _context.HoaDons.Add(donTamTinh);
            await _context.SaveChangesAsync();

            // Lưu chi tiết
            foreach (var sp in model.SanPhams)
            {
                _context.CT_HoaDons.Add(new CT_HoaDon
                {
                    MaHD = maHD,
                    MaSP = sp.MaSP,
                    DonGiaBan = sp.DonGia,
                    SoLuong = sp.SoLuong,
                    SoLuongDaTra = 0
                });
            }
            await _context.SaveChangesAsync();

            await _logger.LogAsync("DonHang", $"Tạo đơn tạm tính {maHD} cho khách hàng {khachHang.HoTen}", "Tạo đơn tạm tính", maNV);

            return Json(new { success = true, maHD = maHD, tongThanhToan = tongThanhToan, tienDatCoc = tienDatCoc });
        }

        // ==========================================
        // 3. XÁC NHẬN THU TIỀN & SINH MÃ QR (UC_2.1 / UC_2.3)
        // ==========================================
        [HttpGet, Authorize(Roles = InvoiceRoles)]
        public async Task<IActionResult> MaQR(string id, decimal? amount = null)
        {
            var order = await _context.HoaDons.AsNoTracking().Include(h => h.KhachHang).FirstOrDefaultAsync(h => h.MaHD == id);
            if (order == null) return NotFound();
            var value = amount ?? (order.TyLeDatCoc > 0 && order.DaThu == 0 ? Math.Min(order.TienDatCoc, order.ConNo) : order.ConNo);
            if (!ModelState.IsValid || order.TrangThaiThanhToan == "Đã hủy" || value < 0 || value > order.ConNo)
                return BadRequest("Số tiền cần thu hoặc trạng thái đơn không hợp lệ.");
            // Read-only: viewing/saving/printing the QR never confirms a receipt.
            return View(new OrderQrView(order, value));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanThuTien(string maHD, decimal soTienThu, string phuongThuc = "Tiền mặt", string? noiDung = null)
        {
            var hd = await _context.HoaDons.Include(h => h.KhachHang).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();

            if (hd.TrangThaiThanhToan == "Đã hủy" || soTienThu <= 0 || soTienThu > hd.ConNo || !ValidPayment(phuongThuc))
            {
                TempData["ErrorMessage"] = "Số tiền thu phải lớn hơn 0, không vượt quá số còn nợ; kiểm tra hình thức thu và trạng thái đơn!";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";

            // Tạo bản ghi Lần thu tiền
            var lanThu = new LanThuTien
            {
                MaHD = hd.MaHD,
                NgayThu = DateTime.Now,
                SoTienThu = soTienThu,
                PhuongThucThanhToan = phuongThuc,
                NoiDung = noiDung ?? (hd.TyLeDatCoc > 0 && hd.DaThu == 0 ? "Thu tiền đặt cọc" : "Thu tiền đơn hàng"),
                MaNVThu = maNV,
                TrangThai = "Hiệu lực"
            };
            _context.LanThuTiens.Add(lanThu);

            // Cập nhật DaThu
            hd.DaThu += soTienThu;

            UpdatePaymentStatus(hd);

            // Cập nhật điểm tích lũy cho khách hàng khi thu tiền
            if (hd.KhachHang != null)
            {
                hd.KhachHang.TongTichLuy += soTienThu;
            }

            await _context.SaveChangesAsync();

            await _logger.LogAsync("DonHang", $"Xác nhận thu {soTienThu:N0} đ cho chứng từ {hd.MaHD} ({phuongThuc})", "Thu tiền", maNV);

            TempData["SuccessMessage"] = $"Đã xác nhận thu thành công {soTienThu:N0} đ!";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }

        // ==========================================
        // 4. PHÁT HÀNH HÓA ĐƠN CHÍNH THỨC (UC_2.3)
        // ==========================================
        [HttpGet, Authorize(Roles = InvoiceRoles)]
        public async Task<IActionResult> TaoHoaDon(string id)
        {
            var don = await LoadInvoiceDocument(id);
            if (don == null) return NotFound();
            if (!CanIssue(don))
            {
                TempData["ErrorMessage"] = "Chỉ tạo hóa đơn từ đơn tạm tính đã thanh toán đủ và chưa có hóa đơn chính thức.";
                return RedirectToAction("ChiTietHoaDon", new { id = don.MaHD });
            }
            // Preview is read-only: the official number/date are assigned only on POST.
            return View(don);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = InvoiceRoles)]
        public async Task<IActionResult> PhatHanhHoaDon(string maHD, bool inHoaDonGiay = false, bool guiZalo = false, bool guiEmail = false)
        {
            var don = await _context.HoaDons.Include(h => h.ChiTietHoaDons).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (don == null) return NotFound();

            if (don.LoaiChungTu != "DonTamTinh" || !string.IsNullOrEmpty(don.MaHoaDon))
            {
                TempData["ErrorMessage"] = "Chứng từ này đã là hóa đơn chính thức!";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            // UC_2.3: recheck persisted payment after acquiring the document-write transaction lock.
            if (!CanIssue(don))
            {
                TempData["ErrorMessage"] = "Đơn tạm tính chưa thanh toán đủ hoặc đã bị hủy. Không thể tạo hóa đơn.";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";

            // Chuyển loại chứng từ sang HoaDon và sinh mã hóa đơn HD...
            don.LoaiChungTu = "HoaDon";
            don.MaHoaDon = NewCode("HD");
            don.NgayPhatHanh = DateTime.Now;
            if (don.ThuRong >= don.TongThanhToan)
            {
                don.TrangThaiThanhToan = "Hoàn thành";
            }

            await _context.SaveChangesAsync();

            await _logger.LogAsync("HoaDon", $"Phát hành hóa đơn chính thức {don.MaHD}", "Phát hành hóa đơn", maNV);

            TempData["SuccessMessage"] = "Tạo hóa đơn thành công.";
            var lanXuat = Request.Headers["X-Request-Id"].FirstOrDefault() ?? Request.Form["requestId"].FirstOrDefault();
            // DocumentWriteFilter commits before this redirect is executed. Export starts on the next GET.
            return RedirectToAction("XuatHoaDon", new { id = maHD, inHoaDonGiay, guiZalo, guiEmail, lanXuat });
        }

        [HttpGet, Authorize(Roles = InvoiceRoles)]
        public async Task<IActionResult> XuatHoaDon(string id, bool inHoaDonGiay = false, bool guiZalo = false, bool guiEmail = false, string? lanXuat = null)
        {
            var don = await LoadInvoiceDocument(id);
            if (don == null) return NotFound();
            if (don.LoaiChungTu != "HoaDon" || string.IsNullOrEmpty(don.MaHoaDon) || don.NgayPhatHanh == null)
            {
                TempData["ErrorMessage"] = "Hóa đơn chưa được tạo. Không thể xuất chứng từ.";
                return RedirectToAction("ChiTietHoaDon", new { id = don.MaHD });
            }
            ViewBag.InHoaDonGiay = inHoaDonGiay;
            ViewBag.GuiZalo = guiZalo;
            ViewBag.GuiEmail = guiEmail;
            ViewBag.LanXuat = Guid.TryParse(lanXuat, out var exportId) ? exportId.ToString() : null;
            return View(don);
        }

        private Task<HoaDon?> LoadInvoiceDocument(string id) => _context.HoaDons.AsNoTracking()
            .Include(h => h.KhachHang).Include(h => h.NhanVien)
            .Include(h => h.Phieus).ThenInclude(p => p.ChiTietPhieus).ThenInclude(c => c.SanPham)
            .Include(h => h.ChiTietHoaDons).ThenInclude(c => c.SanPham)
            .FirstOrDefaultAsync(h => h.MaHD == id);

        // ==========================================
        // 5. CHI TIẾT HÓA ĐƠN & TRA CỨU (UC_2.4)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ChiTietHoaDon(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var hd = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.NhanVien)
                .AsSplitQuery()
                .Include(h => h.ChietKhau)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(c => c.SanPham)
                .Include(h => h.Phieus)
                    .ThenInclude(p => p.NhanVien)
                .Include(h => h.Phieus).ThenInclude(p => p.ChiTietPhieus).ThenInclude(c => c.SanPham)
                .Include(h => h.LanThuTiens)
                    .ThenInclude(l => l.NhanVienThu)
                .Include(h => h.DotGiaoHangs)
                    .ThenInclude(d => d.ChiTietGiaoHangs)
                        .ThenInclude(cd => cd.SanPham)
                .FirstOrDefaultAsync(h => h.MaHD == id || h.MaHoaDon == id);

            if (hd == null)
            {
                return View("NotFoundInvoice", id);
            }

            ViewBag.UserRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            ViewBag.SanPhams = await _context.SanPhams.Where(s => !s.IsDeleted).Include(s => s.LichSuGia).ToListAsync();
            ViewBag.CanIssue = CanIssue(hd) && (User.IsInRole("Chủ cửa hàng") || User.IsInRole("Kế toán bán hàng") || User.IsInRole("Nhân viên bán hàng"));
            ViewBag.CanOverride = !hd.IsLockedGhiDe && (hd.SoLanGhiDe < 7 || await _context.YeuCauMoKhoas.CountAsync(y => y.MaHD == hd.MaHD && y.TrangThai == "Đã duyệt") > hd.SoLanGhiDe - 7);
            ViewBag.ReturnableLines = await ReturnableLines(hd);

            return View(hd);
        }

        // ==========================================
        // 6. PHIẾU CHI PHÍ PHÁT SINH (UC_2.2)
        // ==========================================
        [HttpPost, Authorize(Roles = InvoiceRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhieuCPPS(string maHD, [ModelBinder(BinderType=typeof(InvariantDecimalBinder))] decimal soTien, string lyDo, string loaiChiPhi, string phuongThuc)
        {
            var hd = await _context.HoaDons.Include(h=>h.KhachHang).Include(h=>h.ChiTietHoaDons).ThenInclude(c=>c.SanPham).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();
            if (!Adjustable(hd)) return VoucherFailure(maHD,"maHD","Chỉ lập phiếu cho hóa đơn đã phát hành và chưa hủy.");
            if(soTien<=0||soTien>1_000_000_000_000m||hd.TongThanhToan+soTien>1_000_000_000_000m) ModelState.AddModelError("soTien","Số tiền phải lớn hơn 0 và tổng phải thu không vượt 1.000.000.000.000 đ.");
            ValidateVoucherReason(lyDo,"lyDo");
            if(loaiChiPhi is not ("ThuThemKhach" or "CuaHangChi")) ModelState.AddModelError("loaiChiPhi","Loại chi phí không hợp lệ.");
            if(!ValidPayment(phuongThuc)) ModelState.AddModelError("phuongThuc","Phương thức không hợp lệ.");
            if(!ModelState.IsValid) return VoucherFailure(maHD);
            var before=InvoiceSnapshot(hd);

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";
            string maPhieu = NewCode("PSC");

            var phieu = new Phieu
            {
                MaPhieu = maPhieu,
                LoaiPhieu = "ChiPhiPhatSinh",
                NgayLap = DateTime.Now,
                LyDo = lyDo.Trim(),
                SoTien = soTien,
                LoaiChiPhiPhatSinh = loaiChiPhi, // "ThuThemKhach" hoặc "CuaHangChi"
                PhuongThucThanhToan = phuongThuc,
                MaNV = maNV,
                MaHD = maHD,
                TrangThai = "Hiệu lực"
            };

            _context.Phieus.Add(phieu);

            // BR-UC2.2-03: Nếu thu thêm của khách, cộng một lần vào tổng thanh toán phải thu
            if (loaiChiPhi == "ThuThemKhach")
            {
                hd.TongThanhToan += soTien;
                UpdatePaymentStatus(hd);
            }
            phieu.DuLieuGocJson=before;phieu.DuLieuMoiJson=InvoiceSnapshot(hd);

            await _context.SaveChangesAsync();

            await _logger.LogAsync("HoaDon", $"Tạo phiếu CPPS {maPhieu} ({soTien:N0} đ, {loaiChiPhi}) cho đơn {maHD}", lyDo, maNV);

            return VoucherSuccess(hd,phieu,"Tạo phiếu thành công.");
        }

        // ==========================================
        // 7. PHIẾU ĐỔI TRẢ (UC_2.5)
        // ==========================================
        [HttpPost, Authorize(Roles = InvoiceRoles)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhieuDoiTra(
            string maHD,
            string loaiPhieu, // "TraHang" hoặc "DoiHang"
            string? maSPTra,
            int soLuongTra,
            string tinhTrangHang,
            string lyDo,
            string? maSPDoi = null,
            int soLuongDoi = 0,
            string hinhThucXuLy = "CanTruCongNo", List<ChiTietSPItem>? returns = null) // "CanTruCongNo" hoặc "HoanTienTrucTiep"
        {
            var hd = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(c=>c.SanPham)
                .Include(h => h.KhachHang)
                .FirstOrDefaultAsync(h => h.MaHD == maHD);

            if (hd == null) return NotFound();

            if(!Adjustable(hd)) return VoucherFailure(maHD,"maHD","Chỉ đổi trả hóa đơn đã phát hành và chưa hủy.");
            ValidateVoucherReason(lyDo,"lyDo");
            if(loaiPhieu is not ("TraHang" or "DoiHang")) ModelState.AddModelError("loaiPhieu","Chọn Trả hàng hoặc Đổi hàng.");
            if(hinhThucXuLy is not ("CanTruCongNo" or "HoanTienTrucTiep")) ModelState.AddModelError("hinhThucXuLy","Phương án không hợp lệ.");

            // BR-UC2.5-03: Chỉ nhận hàng còn nguyên vẹn
            if (tinhTrangHang != "Còn nguyên vẹn")
            {
                ModelState.AddModelError("tinhTrangHang","Chỉ chấp nhận hàng Còn nguyên vẹn.");
            }
            if(!ModelState.IsValid) return VoucherFailure(maHD);

            var selected = returns?.ToList()
                ?? new List<ChiTietSPItem> { new() { MaSP = maSPTra ?? "", SoLuong = soLuongTra } };
            var available = await ReturnableLines(hd);
            var sourceKeys = selected.Select(r => r.SourceVoucherLineId.HasValue ? "P" + r.SourceVoucherLineId : "H" + r.MaSP).ToList();
            var refundLines = new List<(CT_HoaDon? Original, ReturnableLine Line, int Quantity)>();
            for(var index=0;index<selected.Count;index++)
            {
                var item=selected[index];
                var line = available.FirstOrDefault(c => c.MaSP == item.MaSP && c.SourceVoucherLineId == item.SourceVoucherLineId);
                if (line == null || item.SoLuong <= 0 || item.SoLuong > line.Remaining)
                {
                    return VoucherFailure(maHD,$"returns[{index}].SoLuong","Số lượng trả không hợp lệ hoặc vượt số lượng còn được trả của đúng dòng gốc.");
                }
                refundLines.Add((item.SourceVoucherLineId == null ? hd.ChiTietHoaDons.Single(c => c.MaSP == item.MaSP) : null, line, item.SoLuong));
            }
            if (selected.Count == 0 || sourceKeys.Distinct().Count() != selected.Count)
            {
                return VoucherFailure(maHD,"returns","Chọn ít nhất một dòng hàng và không chọn trùng dòng gốc.");
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";
            string maPhieu = NewCode(loaiPhieu == "DoiHang" ? "PDH" : "PTH");

            decimal tienHangTra = refundLines.Sum(r => r.Line.Price * r.Quantity);
            decimal tienHangDoi = 0;
            decimal donGiaDoi = 0;

            if (loaiPhieu == "DoiHang")
            {
                if (string.IsNullOrEmpty(maSPDoi) || !await _context.SanPhams.AnyAsync(s => s.MaSP == maSPDoi && !s.IsDeleted))
                {
                    return VoucherFailure(maHD,"maSPDoi","Vui lòng chọn sản phẩm đổi mới hợp lệ.");
                }
                if(soLuongDoi<=0||soLuongDoi>100000) return VoucherFailure(maHD,"soLuongDoi","Số lượng phải từ 1 đến 100.000.");

                var spMoiGia = await _context.Gias
                    .Where(g => g.MaSP == maSPDoi && g.NgayApDung <= DateTime.Now)
                    .OrderByDescending(g => g.NgayApDung).ThenByDescending(g => g.MaGia)
                    .Select(g => g.DonGia)
                    .FirstOrDefaultAsync();

                if (spMoiGia <= 0)
                {
                    return VoucherFailure(maHD,"maSPDoi","Sản phẩm đổi chưa có giá hiệu lực.");
                }
                donGiaDoi = spMoiGia;
                tienHangDoi = donGiaDoi * soLuongDoi;
            }

            decimal tienChenhLech = tienHangDoi - tienHangTra; // > 0: Khách trả thêm; < 0: Cửa hàng hoàn tiền/cấn nợ
            if(hd.TongThanhToan+tienChenhLech<0||hd.TongThanhToan+tienChenhLech>1_000_000_000_000m)
                return VoucherFailure(maHD,"returns","Giá trị điều chỉnh vượt tổng phải thu hợp lệ.");
            var before=InvoiceSnapshot(hd);

            var phieu = new Phieu
            {
                MaPhieu = maPhieu,
                LoaiPhieu = loaiPhieu,
                NgayLap = DateTime.Now,
                LyDo = lyDo.Trim(),
                SoTien = Math.Abs(tienChenhLech),
                HinhThucXuLyDoiTra = hinhThucXuLy,
                TienChenhLech = tienChenhLech,
                MaNV = maNV,
                MaHD = maHD,
                TrangThai = "Hiệu lực"
            };

            foreach (var line in refundLines)
            {
                phieu.ChiTietPhieus.Add(new CT_Phieu {
                    MaPhieu = maPhieu, MaSP = line.Line.MaSP, LoaiDong = "TraHang", SoLuong = line.Quantity,
                    DonGiaXuLy = line.Line.Price, MaHDGoc = line.Original?.MaHD, MaSPGoc = line.Original?.MaSP, SourceVoucherLineId = line.Line.SourceVoucherLineId,
                    TinhTrangHang = tinhTrangHang, GhiChu = "Trả lại hàng từ hóa đơn gốc"
                });
                if (line.Original != null) line.Original.SoLuongDaTra += line.Quantity;
            }

            // Nếu là đổi hàng, thêm dòng sản phẩm mới
            if (loaiPhieu == "DoiHang")
            {
                phieu.ChiTietPhieus.Add(new CT_Phieu
                {
                    MaPhieu = maPhieu,
                    MaSP = maSPDoi!,
                    LoaiDong = "DoiMoi",
                    SoLuong = soLuongDoi,
                    DonGiaXuLy = donGiaDoi,
                    TinhTrangHang = "Còn nguyên vẹn",
                    GhiChu = "Đổi sản phẩm mới"
                });

                // Exchange lines belong to the voucher; the original invoice remains a price snapshot.
            }

            decimal noTruoc = hd.ConNo;
            decimal phaiHoanTruoc = hd.PhaiHoan;
            hd.TongThanhToan += tienChenhLech;
            phieu.TienCanTru = Math.Min(Math.Max(-tienChenhLech, 0), noTruoc);
            phieu.TienPhaiHoan = Math.Max(hd.PhaiHoan - phaiHoanTruoc, 0);
            phieu.TienThucHoan = 0;
            phieu.GhiChu = $"Cấn công nợ: {phieu.TienCanTru:N0} đ; phải hoàn: {phieu.TienPhaiHoan:N0} đ. Chưa chi tiền; xác nhận hoàn riêng.";
            UpdatePaymentStatus(hd);
            phieu.DuLieuGocJson=before;phieu.DuLieuMoiJson=InvoiceSnapshot(hd);

            _context.Phieus.Add(phieu);
            await _context.SaveChangesAsync();

            await _logger.LogAsync("HoaDon", $"Tạo phiếu {loaiPhieu} {maPhieu} cho hóa đơn {maHD} ({hinhThucXuLy})", lyDo, maNV);

            return VoucherSuccess(hd,phieu,"Tạo phiếu đổi trả thành công.");
        }

        // ==========================================
        // 8. TẠO PHIẾU GHI ĐÈ HÓA ĐƠN (UC_2.6)
        // ==========================================
        [HttpGet, Authorize(Roles="Chủ cửa hàng,Kế toán bán hàng")]
        public async Task<IActionResult> TaoPhieuGhiDe(string id)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            if (userRole == "Nhân viên bán hàng")
            {
                TempData["ErrorMessage"] = "Nhân viên bán hàng không có quyền tạo phiếu ghi đè! Chức năng chỉ dành cho Kế toán bán hàng và Chủ cửa hàng (NFR-02).";
                return RedirectToAction("ChiTietHoaDon", new { id = id });
            }

            var hd = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(h => h.MaHD == id || h.MaHoaDon == id);

            if (hd == null) return NotFound();
            if(!Adjustable(hd)) return BadRequest("Chỉ ghi đè hóa đơn đã phát hành và chưa hủy.");

            // BR-UC2.6-01: Chặn thao tác khi đã có 7 lần ghi đè trở lên
            if (await OverrideBlocked(hd))
            {
                ViewBag.IsLocked = true;
                return View("KhoaGhiDe", hd);
            }

            ViewBag.SanPhams = await _context.SanPhams
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    s.MaSP,
                    s.TenSP,
                    s.DonViTinh,
                    DonGia = _context.Gias
                        .Where(g => g.MaSP == s.MaSP && g.NgayApDung <= DateTime.Now)
                        .OrderByDescending(g => g.NgayApDung).ThenByDescending(g => g.MaGia)
                        .Select(g => g.DonGia)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // A product hidden from the catalog must still retain its original invoice line.
            ViewBag.OverrideProducts=await _context.SanPhams.AsNoTracking().Include(s=>s.LichSuGia)
                .Where(s=>!s.IsDeleted||hd.ChiTietHoaDons.Select(c=>c.MaSP).Contains(s.MaSP)).ToListAsync();

            return View(hd);
        }

        [HttpPost, Authorize(Roles="Chủ cửa hàng,Kế toán bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuPhieuGhiDe(string maHD, string lyDoGhiDe, string itemsJson, string? ghiChu=null)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            if (userRole == "Nhân viên bán hàng")
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này!" });
            }

            var hd = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(c=>c.SanPham)
                .Include(h => h.KhachHang)
                .FirstOrDefaultAsync(h => h.MaHD == maHD);

            if (hd == null) return Json(new { success = false, message = "Không tìm thấy hóa đơn!" });

            if (!Adjustable(hd)) return VoucherFailure(maHD,"maHD","Chỉ ghi đè hóa đơn đã phát hành và chưa hủy.");
            // Kiểm tra giới hạn 7 lần
            if (await OverrideBlocked(hd))
            {
                return Json(new { success = false, message = "Hóa đơn đã bị khóa quyền ghi đè do vượt quá giới hạn 7 lần! Vui lòng nộp báo cáo mở khóa." });
            }

            ValidateVoucherReason(lyDoGhiDe,"lyDoGhiDe");
            if(ghiChu?.Length>500) ModelState.AddModelError("ghiChu","Ghi chú không quá 500 ký tự.");
            if(!ModelState.IsValid) return VoucherFailure(maHD);

            List<ChiTietSPItem>? items;
            try { items = JsonSerializer.Deserialize<List<ChiTietSPItem>>(itemsJson); }
            catch (JsonException) { return VoucherFailure(maHD,"itemsJson","Danh sách sản phẩm không hợp lệ."); }
            if (items == null || !items.Any() || items.Count>100 || items.Any(i=>i==null) || items.Select(i => i.MaSP).Distinct().Count() != items.Count)
            {
                return VoucherFailure(maHD,"itemsJson","Chọn từ 1 đến 100 dòng sản phẩm, không trùng sản phẩm.");
            }
            for(var index=0;index<items.Count;index++) {
                var item=items[index];
                if(item.SoLuong<=0||item.SoLuong>100000) return VoucherFailure(maHD,$"items[{index}].SoLuong","Số lượng phải là số nguyên từ 1 đến 100.000.");
                if(item.DonGia<=0||item.DonGia>1_000_000_000_000m||item.DonGia!=decimal.Truncate(item.DonGia)) return VoucherFailure(maHD,$"items[{index}].DonGia","Đơn giá phải là số nguyên dương.");
                if (!await _context.SanPhams.AnyAsync(s => s.MaSP == item.MaSP && (!s.IsDeleted||hd.ChiTietHoaDons.Select(c=>c.MaSP).Contains(s.MaSP))))
                    return VoucherFailure(maHD,$"items[{index}].MaSP","Sản phẩm không hợp lệ.");
            }

            if (hd.ChiTietHoaDons.Any(c => c.SoLuongDaTra > 0 && !items.Any(i => i.MaSP == c.MaSP && i.SoLuong >= c.SoLuongDaTra)))
                return Json(new { success = false, message = "Phải giữ dòng gốc đã đổi trả; số lượng mới không được nhỏ hơn số lượng đã trả." });

            // Snapshot dữ liệu gốc
            var snapshotGoc = InvoiceSnapshot(hd);

            // Tính tổng tiền mới
            decimal tongTienHangMoi = items.Sum(i => i.DonGia * i.SoLuong);
            decimal tienChietKhauMoi = Math.Round(tongTienHangMoi * (hd.TongTienHang > 0 ? hd.TienChietKhau / hd.TongTienHang : 0), 0);
            decimal thueMoi = Math.Round((tongTienHangMoi - tienChietKhauMoi) * (hd.TongTienHang > hd.TienChietKhau ? hd.TienThue / (hd.TongTienHang - hd.TienChietKhau) : 0), 0);
            decimal phuPhi = hd.TongThanhToan - hd.TongTienHang + hd.TienChietKhau - hd.TienThue;
            decimal tongThanhToanMoi = tongTienHangMoi - tienChietKhauMoi + thueMoi + phuPhi;
            if (tongThanhToanMoi < 0||tongThanhToanMoi>1_000_000_000_000m) return VoucherFailure(maHD,"itemsJson","Tổng phải thu không được âm hoặc vượt 1.000.000.000.000 đ.");

            decimal daThanhToanCu = hd.ThuRong;
            decimal canThuThem = 0;
            decimal phaiTraKhach = 0;

            if (tongThanhToanMoi > daThanhToanCu)
            {
                canThuThem = tongThanhToanMoi - daThanhToanCu;
            }
            else if (tongThanhToanMoi < daThanhToanCu)
            {
                phaiTraKhach = daThanhToanCu - tongThanhToanMoi;
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV002";
            string maPhieu = NewCode("PGD");

            // Cập nhật lại chi tiết sản phẩm của hóa đơn
            var removed = hd.ChiTietHoaDons.Where(c => !items.Any(i => i.MaSP == c.MaSP)).ToList();
            _context.CT_HoaDons.RemoveRange(removed);
            foreach(var line in removed) hd.ChiTietHoaDons.Remove(line);
            foreach (var it in items)
            {
                var line = hd.ChiTietHoaDons.FirstOrDefault(c => c.MaSP == it.MaSP);
                if (line == null) hd.ChiTietHoaDons.Add(new CT_HoaDon { MaHD = hd.MaHD, MaSP = it.MaSP, DonGiaBan = it.DonGia, SoLuong = it.SoLuong });
                else { line.DonGiaBan = it.DonGia; line.SoLuong = it.SoLuong; }
            }

            // Snapshot dữ liệu mới

            // Tăng số lần ghi đè
            hd.SoLanGhiDe++;
            if (hd.SoLanGhiDe >= 7)
            {
                hd.IsLockedGhiDe = true;
            }

            hd.TienChietKhau = tienChietKhauMoi;
            hd.TienThue = thueMoi;
            hd.TongTienHang = tongTienHangMoi;
            hd.TongThanhToan = tongThanhToanMoi;
            if(ghiChu!=null) hd.GhiChu=ghiChu;
            UpdatePaymentStatus(hd);

            var phieuGD = new Phieu
            {
                MaPhieu = maPhieu,
                LoaiPhieu = "GhiDe",
                NgayLap = DateTime.Now,
                LyDo = lyDoGhiDe.Trim(),
                SoTien = canThuThem > 0 ? canThuThem : phaiTraKhach,
                TienChenhLech = canThuThem > 0 ? canThuThem : -phaiTraKhach,
                SoLanGhiDeHienTai = hd.SoLanGhiDe,
                DuLieuGocJson = snapshotGoc,
                DuLieuMoiJson = InvoiceSnapshot(hd),
                MaNV = maNV,
                MaHD = hd.MaHD,
                TrangThai = "Hiệu lực"
            };

            _context.Phieus.Add(phieuGD);
            await _context.SaveChangesAsync();

            await _logger.LogAsync("HoaDon", $"Ghi đè hóa đơn {hd.MaHD} lần thứ {hd.SoLanGhiDe} (Cần thu thêm: {canThuThem:N0} đ, Phải trả khách: {phaiTraKhach:N0} đ)", lyDoGhiDe, maNV);

            return Json(new {
                success = true,
                maHD = hd.MaHD,
                soLanGhiDe = hd.SoLanGhiDe,
                canThuThem = canThuThem,
                phaiTraKhach = phaiTraKhach,
                maPhieu, message="Tạo phiếu thành công.",redirect=Url.Action("ChiTietHoaDon",new {id=hd.MaHD})
            });
        }

        // ==========================================
        // 9. NỘP YÊU CẦU MỞ KHÓA GHI ĐÈ (Khi chạm mốc 7 lần)
        // ==========================================
        [HttpPost, Authorize(Roles="Chủ cửa hàng,Kế toán bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiYeuCauMoKhoa(string maHD, string lyDoMoKhoa)
        {
            if (string.IsNullOrWhiteSpace(lyDoMoKhoa)||lyDoMoKhoa.Length>500)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do xin mở khóa quyền (bắt buộc)!";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            var hd = await _context.HoaDons.FindAsync(maHD);
            if (hd == null || !await OverrideBlocked(hd)) return BadRequest("Hóa đơn chưa bị khóa.");
            if (await _context.YeuCauMoKhoas.AnyAsync(y => y.MaHD == maHD && y.TrangThai == "Chờ duyệt"))
            {
                TempData["ErrorMessage"] = "Hóa đơn đã có yêu cầu đang chờ duyệt.";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }
            var maNV = User.FindFirstValue("MaNV") ?? "NV002";

            var yc = new YeuCauMoKhoa
            {
                MaHD = maHD,
                MaNV = maNV,
                LyDoMoKhoa = lyDoMoKhoa.Trim(),
                ThoiGianGui = DateTime.Now,
                TrangThai = "Chờ duyệt"
            };

            _context.YeuCauMoKhoas.Add(yc);
            await _context.SaveChangesAsync();

            await _logger.LogAsync("YeuCauMoKhoa", $"Gửi yêu cầu mở khóa ghi đè cho hóa đơn {maHD}", lyDoMoKhoa, maNV);

            TempData["SuccessMessage"] = "Đã gửi yêu cầu mở khóa thành công tới Chủ cửa hàng!";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }

        // ==========================================
        // 10. THÊM ĐỢT GIAO HÀNG (Đơn công trình)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemDotGiaoHang(string maHD, int dotThu, string? nguoiGiao, string? ghiChu, List<ChiTietSPItem> items)
        {
            var hd = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();

            if (hd.LoaiChungTu != "HoaDon" || hd.TyLeDatCoc <= 0 || dotThu <= 0 || items == null || !items.Any(i => i.SoLuong > 0)
                || items.Any(i => i.SoLuong < 0) || items.Select(i => i.MaSP).Distinct().Count() != items.Count
                || await _context.DotGiaoHangs.AnyAsync(d => d.MaHD == maHD && d.DotThu == dotThu))
            {
                TempData["ErrorMessage"] = "Đợt giao không hợp lệ hoặc đã tồn tại. Chỉ giao đơn công trình đã phát hành.";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }
            foreach (var item in items.Where(i => i.SoLuong > 0))
            {
                var original = await _context.CT_HoaDons.FindAsync(maHD, item.MaSP);
                var delivered = await _context.CT_DotGiaoHangs.Where(c => c.DotGiaoHang!.MaHD == maHD && c.MaSP == item.MaSP).SumAsync(c => c.SoLuongGiao);
                if (original == null || item.SoLuong + delivered > original.SoLuong)
                {
                    TempData["ErrorMessage"] = "Số lượng giao vượt số lượng mua hoặc sản phẩm không thuộc đơn.";
                    return RedirectToAction("ChiTietHoaDon", new { id = maHD });
                }
            }

            var dotGiao = new DotGiaoHang
            {
                MaHD = maHD,
                DotThu = dotThu,
                NgayGiao = DateTime.Now,
                NguoiGiao = nguoiGiao,
                TrangThaiGiao = "Đã giao thành công",
                GhiChu = ghiChu
            };

            if (items != null)
            {
                foreach (var it in items.Where(i => i.SoLuong > 0))
                {
                    dotGiao.ChiTietGiaoHangs.Add(new CT_DotGiaoHang
                    {
                        MaSP = it.MaSP,
                        SoLuongGiao = it.SoLuong
                    });
                }
            }

            _context.DotGiaoHangs.Add(dotGiao);
            await _context.SaveChangesAsync();

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";
            await _logger.LogAsync("DonHang", $"Cập nhật đợt giao hàng thứ {dotThu} cho đơn {maHD}", ghiChu, maNV);

            TempData["SuccessMessage"] = $"Đã cập nhật đợt giao hàng {dotThu} thành công!";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }
        private async Task<List<ReturnableLine>> ReturnableLines(HoaDon hd)
        {
            var lines = hd.ChiTietHoaDons.Select(c => new ReturnableLine { MaSP = c.MaSP, Name = c.SanPham?.TenSP ?? c.MaSP,
                Unit = c.SanPham?.DonViTinh ?? "", Purchased = c.SoLuong, Remaining = c.SoLuongConLai,
                Price = Math.Round(c.DonGiaBan * (1 - (hd.TongTienHang > 0 ? hd.TienChietKhau / hd.TongTienHang : 0)), 2) }).ToList();
            var exchanged = await _context.CT_Phieus.Where(c => c.Phieu!.MaHD == hd.MaHD && c.Phieu.TrangThai == "Hiệu lực" && c.LoaiDong == "DoiMoi")
                .Include(c => c.SanPham).Include(c => c.Phieu).ToListAsync();
            var returned = await _context.CT_Phieus.Where(c => c.Phieu!.MaHD == hd.MaHD && c.Phieu.TrangThai == "Hiệu lực" && c.SourceVoucherLineId != null)
                .GroupBy(c => c.SourceVoucherLineId).Select(g => new { Id = g.Key, Quantity = g.Sum(c => c.SoLuong) }).ToListAsync();
            foreach (var source in exchanged)
                lines.Add(new ReturnableLine { MaSP = source.MaSP, Name = source.SanPham?.TenSP ?? source.MaSP, Unit = source.SanPham?.DonViTinh ?? "",
                    Purchased = source.SoLuong, Remaining = Math.Max(source.SoLuong - (returned.FirstOrDefault(r => r.Id == source.Id)?.Quantity ?? 0), 0),
                    Price = source.DonGiaXuLy, SourceVoucherLineId = source.Id, SourceVoucher = source.MaPhieu });
            return lines;
        }

        private static string NewCode(string prefix) => prefix + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..10];
        [HttpGet]
        public async Task<IActionResult> ChiTietPhieu(string id)
        {
            var voucher = await _context.Phieus.Include(p => p.NhanVien).Include(p => p.ChiTietPhieus).ThenInclude(c => c.SanPham).FirstOrDefaultAsync(p => p.MaPhieu == id);
            return voucher == null ? NotFound() : View(voucher);
        }
        private static bool ValidPayment(string value) => value == "Tiền mặt" || value == "Chuyển khoản";
        private static bool CanIssue(HoaDon hd) => hd.LoaiChungTu == "DonTamTinh" && string.IsNullOrEmpty(hd.MaHoaDon)
            && hd.TrangThaiThanhToan != "Đã hủy" && hd.TongThanhToan > 0 && hd.ChiTietHoaDons.Any() && hd.ThuRong >= hd.TongThanhToan;
        private static void UpdatePaymentStatus(HoaDon hd) => hd.TrangThaiThanhToan = hd.PhaiHoan > 0 ? "Phải hoàn tiền" :
            hd.ConNo == 0 ? (hd.LoaiChungTu == "HoaDon" ? "Hoàn thành" : "Đã thanh toán") : hd.ThuRong > 0 ? "Đã đặt cọc" : "Chưa thanh toán";
        private async Task<bool> OverrideBlocked(HoaDon hd) => hd.IsLockedGhiDe ||
            (hd.SoLanGhiDe >= 7 && await _context.YeuCauMoKhoas.CountAsync(y => y.MaHD == hd.MaHD && y.TrangThai == "Đã duyệt") <= hd.SoLanGhiDe - 7);

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanHoanTien(string maHD, decimal soTien, string phuongThuc, string lyDo)
        {
            var hd = await _context.HoaDons.Include(h => h.KhachHang).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();
            if (soTien <= 0 || soTien > hd.PhaiHoan || !ValidPayment(phuongThuc) || string.IsNullOrWhiteSpace(lyDo) || lyDo.Length > 500)
            {
                TempData["ErrorMessage"] = "Số tiền hoàn không hợp lệ hoặc vượt số phải hoàn. Cần lý do và hình thức chi.";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }
            var maNV = User.FindFirstValue("MaNV")!;
            hd.DaHoanKhach += soTien;
            if (hd.KhachHang != null) hd.KhachHang.TongTichLuy = Math.Max(0, hd.KhachHang.TongTichLuy - soTien);
            UpdatePaymentStatus(hd);
            _context.Phieus.Add(new Phieu { MaPhieu = NewCode("PHT"), LoaiPhieu = "HoanTien", MaHD = maHD, MaNV = maNV,
                LyDo = lyDo.Trim(), SoTien = soTien, TienThucHoan = soTien, PhuongThucThanhToan = phuongThuc });
            await _context.SaveChangesAsync();
            await _logger.LogAsync("HoaDon", $"Xác nhận đã hoàn {soTien:N0} đ cho {maHD} ({phuongThuc})", lyDo, maNV);
            TempData["SuccessMessage"] = "Đã ghi nhận tiền thực hoàn cho khách.";
            return RedirectToAction("ChiTietHoaDon", new { id = maHD });
        }

    }
}
