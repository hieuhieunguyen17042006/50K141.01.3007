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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(string tab = "dontamtinh", string? search = null, string? status = null)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            ViewBag.ActiveTab = tab.ToLower();
            ViewBag.Search = search;
            if(tab.Equals("hoadon",StringComparison.OrdinalIgnoreCase)) status=null;
            status = status is SalesListStatus.PaidLabel or SalesListStatus.UnpaidLabel ? status : null;
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
            if (status == SalesListStatus.PaidLabel)
                query = query.Where(SalesListStatus.Paid);
            else if (status == SalesListStatus.UnpaidLabel)
                query = query.Where(SalesListStatus.Unpaid);

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
            var metrics=await _context.HoaDons.AsNoTracking().GroupBy(h=>1).Select(g=>new{
                Total=g.Count(),Unpaid=g.Count(h=>h.TrangThaiThanhToan!="Đã hủy"&&h.DaThu-h.DaHoanKhach<h.TongThanhToan),
                Paid=g.Count(h=>h.TrangThaiThanhToan!="Đã hủy"&&h.DaThu-h.DaHoanKhach==h.TongThanhToan),Issued=g.Count(h=>h.LoaiChungTu=="HoaDon")}).FirstOrDefaultAsync();
            ViewBag.TotalOrders=metrics?.Total??0;ViewBag.UnpaidCount=metrics?.Unpaid??0;ViewBag.PaidCount=metrics?.Paid??0;ViewBag.IssuedCount=metrics?.Issued??0;
            return View(list);
        }

        // ==========================================
        // 2. TẠO HÓA ĐƠN TẠM TÍNH (UC_2.1)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TaoDonTamTinh(string? preparedId = null)
        {
            EnsurePreparationScope();
            if (preparedId != null)
            {
                var prepared = await GetPreparedOrder(preparedId);
                if (prepared == null) TempData["ErrorMessage"] = "Dữ liệu chuẩn bị đã hết hiệu lực. Vui lòng nhập lại đơn.";
                else
                {
                    if (await _context.HoaDons.AnyAsync(h => h.MaHD == prepared.Preview.MaHD && h.MaNV == OrderActor))
                        return RedirectToAction("ChiTietHoaDon", new { id = prepared.Preview.MaHD });
                    ViewBag.RestoreOrder = prepared.InputJson;
                }
            }
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
            public string PhuongThucThanhToan { get; set; } = "Chưa xác định";
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
            // Retain the explicit-save API for existing integrations; the entry screen now only prepares.
            var order = await ValidateOrder(model, NewCode("TT"));
            if (order == null) return OrderErrors();
            await PersistOrder(order);
            return Json(new { success = true, maHD = order.MaHD, tongThanhToan = order.TongThanhToan, tienDatCoc = order.TienDatCoc });
        }

        // ==========================================
        // 3. XÁC NHẬN THU TIỀN & SINH MÃ QR (UC_2.1 / UC_2.3)
        // ==========================================
        [HttpGet, Authorize(Roles = InvoiceRoles)]
        public async Task<IActionResult> MaQR(string? id = null, decimal? amount = null, string? preparedId = null)
        {
            if (preparedId != null)
            {
                var prepared = await GetPreparedOrder(preparedId);
                if (prepared == null)
                {
                    if (id != null && await _context.HoaDons.AnyAsync(h => h.MaHD == id))
                        return RedirectToAction("ChiTietHoaDon", new { id });
                    TempData["ErrorMessage"] = "Dữ liệu chuẩn bị đã hết hiệu lực. Vui lòng mở lại màn nhập đơn.";
                    return RedirectToAction("TaoDonTamTinh");
                }
                if (await _context.HoaDons.AnyAsync(h => h.MaHD == prepared.Preview.MaHD && h.MaNV == OrderActor))
                    return RedirectToAction("ChiTietHoaDon", new { id = prepared.Preview.MaHD });
                var pendingAmount = prepared.Preview.TyLeDatCoc > 0 ? prepared.Preview.TienDatCoc : prepared.Preview.TongThanhToan;
                return View(new OrderQrView(prepared.Preview, pendingAmount, prepared.Id));
            }
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
        public async Task<IActionResult> XacNhanThuTien(string maHD, decimal soTienThu, string? phuongThuc = null, string? noiDung = null)
        {
            var hd = await _context.HoaDons.Include(h => h.KhachHang).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();

            if (hd.TrangThaiThanhToan == "Đã hủy" || soTienThu <= 0 || soTienThu > hd.ConNo || !ValidPayment(phuongThuc))
            {
                if (Request.Headers.Accept.ToString().Contains("application/json")) return Json(new {success=false,message="Không hợp lệ!",errors=new Dictionary<string,string[]> {
                    {soTienThu<=0||soTienThu>hd.ConNo?"soTienThu":"phuongThuc",new[]{"Kiểm tra số tiền, hình thức thu và trạng thái đơn."}}
                }});
                TempData["ErrorMessage"] = "Không hợp lệ! Kiểm tra số tiền, hình thức thu và trạng thái đơn.";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            var maNV = User.FindFirstValue("MaNV") ?? "NV001";

            // Tạo bản ghi Lần thu tiền
            var lanThu = new LanThuTien
            {
                MaHD = hd.MaHD,
                NgayThu = DateTime.Now,
                SoTienThu = soTienThu,
                PhuongThucThanhToan = phuongThuc!,
                NoiDung = noiDung ?? (hd.TyLeDatCoc > 0 && hd.DaThu == 0 ? "Thu tiền đặt cọc" : "Thu tiền đơn hàng"),
                MaNVThu = maNV,
                TrangThai = "Hiệu lực"
            };
            _context.LanThuTiens.Add(lanThu);

            // Cập nhật DaThu
            hd.DaThu += soTienThu;
            // Determine the displayed method from confirmed receipts, not a planned method.
            var methods = await _context.LanThuTiens.Where(t => t.MaHD == maHD && t.TrangThai == "Hiệu lực")
                .Select(t => t.PhuongThucThanhToan).Distinct().ToListAsync();
            hd.PhuongThucThanhToan = methods.Append(phuongThuc!).Distinct().Count() == 1 ? phuongThuc! : "Nhiều phương thức";

            UpdatePaymentStatus(hd);

            // Cập nhật điểm tích lũy cho khách hàng khi thu tiền
            if (hd.KhachHang != null)
            {
                hd.KhachHang.TongTichLuy += soTienThu;
            }

            await _context.SaveChangesAsync();

            await _logger.LogAsync("DonHang", $"Xác nhận thu {soTienThu:N0} đ cho chứng từ {hd.MaHD} ({phuongThuc})", "Thu tiền", maNV);

            TempData["ReceiptConfirmed"] = lanThu.MaLanThu.ToString();
            if (Request.Headers.Accept.ToString().Contains("application/json")) return Json(new {success=true,redirect=Url.Action("ChiTietHoaDon",new{id=maHD})});
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
            if (!CanIssue(don) && don.LoaiChungTu != "HoaDon")
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
                // Replaying this action must not issue another number or schedule another export.
                return RedirectToAction("TaoHoaDon", new { id = maHD });
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

            // One-shot notice on the creation screen, rendered after the transaction commits.
            TempData["InvoiceCreated"] = System.Text.Json.JsonSerializer.Serialize(new {
                maHD, inHoaDonGiay, guiZalo, guiEmail, exportId = Guid.NewGuid().ToString("N")
            });
            return RedirectToAction("TaoHoaDon", new { id = maHD });
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
            // Keep old links without a result-only page or GET-triggered exports.
            return RedirectToAction("TaoHoaDon", new { id });
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
        public async Task<IActionResult> TaoPhieuCPPS(string maHD, [ModelBinder(BinderType=typeof(InvariantDecimalBinder))] decimal soTien, string lyDo, string loaiChiPhi = "ThuThemKhach", string? phuongThuc = null)
        {
            var hd = await _context.HoaDons.Include(h=>h.KhachHang).Include(h=>h.ChiTietHoaDons).ThenInclude(c=>c.SanPham).FirstOrDefaultAsync(h => h.MaHD == maHD);
            if (hd == null) return NotFound();
            if (!Adjustable(hd)) return VoucherFailure(maHD,"maHD","Chỉ lập phiếu cho hóa đơn đã phát hành và chưa hủy.");
            if(soTien<=0||soTien>1_000_000_000_000m||hd.TongThanhToan+soTien>1_000_000_000_000m) ModelState.AddModelError("soTien","Số tiền phải lớn hơn 0 và tổng phải thu không vượt 1.000.000.000.000 đ.");
            ValidateVoucherReason(lyDo,"lyDo");
            if(loaiChiPhi is not ("ThuThemKhach" or "CuaHangChi")) ModelState.AddModelError("loaiChiPhi","Loại chi phí không hợp lệ.");
            // A surcharge is a receivable, not a cash receipt. Preserve legacy store-paid integrations.
            if(loaiChiPhi == "CuaHangChi" && !ValidPayment(phuongThuc)) ModelState.AddModelError("phuongThuc","Phương thức không hợp lệ.");
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
                PhuongThucThanhToan = loaiChiPhi == "ThuThemKhach" ? "Chưa thu" : phuongThuc!,
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
            string hinhThucXuLy = "CanTruCongNo", List<ChiTietSPItem>? returns = null, List<ChiTietSPItem>? replacements = null)
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
            var newLines=new List<ChiTietSPItem>();

            if (loaiPhieu == "DoiHang")
            {
                newLines=replacements??new List<ChiTietSPItem>{new(){MaSP=maSPDoi??"",SoLuong=soLuongDoi}};
                if(newLines.Count==0||newLines.Count>100) return VoucherFailure(maHD,"replacements","Chọn từ 1 đến 100 dòng hàng đổi mới.");
                for(var i=0;i<newLines.Count;i++){
                    var item=newLines[i];
                    if(item.SoLuong<=0||item.SoLuong>100000) return VoucherFailure(maHD,$"replacements[{i}].SoLuong","Không hợp lệ!");
                    if(!await _context.SanPhams.AnyAsync(s=>s.MaSP==item.MaSP&&!s.IsDeleted)) return VoucherFailure(maHD,$"replacements[{i}].MaSP","Không hợp lệ!");
                    var price=await _context.Gias.Where(g=>g.MaSP==item.MaSP&&g.NgayApDung<=DateTime.Now).OrderByDescending(g=>g.NgayApDung).ThenByDescending(g=>g.MaGia).Select(g=>g.DonGia).FirstOrDefaultAsync();
                    if(price<=0) return VoucherFailure(maHD,$"replacements[{i}].MaSP","Sản phẩm chưa có giá hiệu lực.");
                    item.DonGia=ReturnUnitPrice(hd,price);tienHangDoi+=item.DonGia*item.SoLuong;
                }
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
                    TenSPDaChot = line.Line.Name, DonViTinhDaChot = line.Line.Unit,
                    DonGiaXuLy = line.Line.Price, MaHDGoc = line.Original?.MaHD, MaSPGoc = line.Original?.MaSP, SourceVoucherLineId = line.Line.SourceVoucherLineId,
                    TinhTrangHang = tinhTrangHang, GhiChu = "Trả lại hàng từ hóa đơn gốc"
                });
                if (line.Original != null) line.Original.SoLuongDaTra += line.Quantity;
            }

            // Nếu là đổi hàng, thêm dòng sản phẩm mới
            if (loaiPhieu == "DoiHang")
            {
                foreach(var item in newLines) phieu.ChiTietPhieus.Add(new CT_Phieu
                {
                    MaPhieu = maPhieu,
                    MaSP = item.MaSP,
                    LoaiDong = "DoiMoi",
                    SoLuong = item.SoLuong,
                    DonGiaXuLy = item.DonGia,
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
                return RedirectToAction("ChiTietHoaDon",new{id=hd.MaHD});
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
            ViewBag.Delivered=await _context.CT_DotGiaoHangs.Where(c=>c.DotGiaoHang!.MaHD==hd.MaHD).GroupBy(c=>c.MaSP).ToDictionaryAsync(g=>g.Key,g=>g.Sum(c=>c.SoLuongGiao));

            return View(hd);
        }

        [HttpPost, Authorize(Roles="Chủ cửa hàng,Kế toán bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuPhieuGhiDe(string maHD, string lyDoGhiDe, string itemsJson, string? ghiChu=null, int? expectedVersion=null)
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

            if(expectedVersion!=hd.SoLanGhiDe) return VoucherFailure(maHD,"itemsJson","Hóa đơn đã thay đổi hoặc thiếu phiên bản. Vui lòng tải lại phiếu ghi đè.");
            List<OverrideLineInput>? items;
            try { items = JsonSerializer.Deserialize<List<OverrideLineInput>>(itemsJson); }
            catch (JsonException) { return VoucherFailure(maHD,"itemsJson","Danh sách sản phẩm không hợp lệ."); }
            if (items == null || !items.Any() || items.Count>100 || items.Any(i=>i==null) || items.Select(i => i.MaSP).Distinct().Count() != items.Count)
            {
                return VoucherFailure(maHD,"itemsJson","Danh sách dòng hàng không hợp lệ hoặc trùng sản phẩm.");
            }
            var originals=hd.ChiTietHoaDons.ToDictionary(c=>c.MaSP);
            if(items.Count!=originals.Count) return VoucherFailure(maHD,"itemsJson","Phiếu ghi đè phải giữ nguyên số dòng hiện có; không được thêm hoặc xóa dòng.");
            if(items.Any(i=>string.IsNullOrWhiteSpace(i.OriginalMaSP)||!originals.ContainsKey(i.OriginalMaSP))||items.Select(i=>i.OriginalMaSP).Distinct().Count()!=originals.Count)
                return VoucherFailure(maHD,"itemsJson","Mỗi dòng phải liên kết đúng một dòng hóa đơn hiện tại. Vui lòng tải lại phiếu.");
            var delivered=await _context.CT_DotGiaoHangs.Where(c=>c.DotGiaoHang!.MaHD==maHD).GroupBy(c=>c.MaSP).ToDictionaryAsync(g=>g.Key,g=>g.Sum(c=>c.SoLuongGiao));
            for(var index=0;index<items.Count;index++) {
                var item=items[index];var original=originals[item.OriginalMaSP];
                // Existing lines retain their agreed price; replacements use the current catalog price.
                var expectedPrice = item.MaSP == original.MaSP ? original.DonGiaBan : await _context.Gias
                    .Where(g=>g.MaSP==item.MaSP&&g.NgayApDung<=DateTime.Now).OrderByDescending(g=>g.NgayApDung).ThenByDescending(g=>g.MaGia)
                    .Select(g=>g.DonGia).FirstOrDefaultAsync();
                if(item.DonGia!=expectedPrice) return VoucherFailure(maHD,$"items[{index}].DonGia","Đơn giá do hệ thống xác định. Vui lòng tải lại phiếu.");
                item.DonGia=expectedPrice;
                var minimum=Math.Max(original.SoLuongDaTra,delivered.GetValueOrDefault(original.MaSP));
                if(minimum>0&&(item.MaSP!=original.MaSP||item.SoLuong<minimum))
                    return VoucherFailure(maHD,$"items[{index}].MaSP","Dòng đã giao/đổi trả phải giữ mặt hàng gốc; số lượng không nhỏ hơn số đã giao/đã trả.");
                if(original.SoLuongDaTra>0&&item.DonGia!=original.DonGiaBan)
                    return VoucherFailure(maHD,$"items[{index}].DonGia","Không đổi giá dòng gốc đã có phiếu trả để giữ giá trị xử lý đã chốt.");
                if(item.SoLuong<=0||item.SoLuong>100000) return VoucherFailure(maHD,$"items[{index}].SoLuong","Số lượng phải là số nguyên từ 1 đến 100.000.");
                if(item.DonGia<=0||item.DonGia>1_000_000_000_000m||item.DonGia!=decimal.Truncate(item.DonGia)) return VoucherFailure(maHD,$"items[{index}].DonGia","Đơn giá phải là số nguyên dương.");
                if (!await _context.SanPhams.AnyAsync(s => s.MaSP == item.MaSP && (!s.IsDeleted||s.MaSP==original.MaSP)))
                    return VoucherFailure(maHD,$"items[{index}].MaSP","Sản phẩm không hợp lệ.");
            }

            if (hd.ChiTietHoaDons.Any(c => c.SoLuongDaTra > 0 && !items.Any(i => i.MaSP == c.MaSP && i.SoLuong >= c.SoLuongDaTra)))
                return Json(new { success = false, message = "Phải giữ dòng gốc đã đổi trả; số lượng mới không được nhỏ hơn số lượng đã trả." });
            if(hd.ChiTietHoaDons.Any(c=>c.SoLuongDaTra>0&&items.Any(i=>i.MaSP==c.MaSP&&i.DonGia!=c.DonGiaBan)))
                return VoucherFailure(maHD,"itemsJson","Không đổi giá dòng gốc đã có phiếu trả để giữ giá trị xử lý đã chốt.");

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
                if (line == null)
                {
                    var product=await _context.SanPhams.AsNoTracking().FirstAsync(p=>p.MaSP==it.MaSP);
                    hd.ChiTietHoaDons.Add(new CT_HoaDon { MaHD=hd.MaHD,MaSP=it.MaSP,DonGiaBan=it.DonGia,SoLuong=it.SoLuong,TenSPDaChot=product.TenSP,DonViTinhDaChot=product.DonViTinh });
                }
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
                DuLieuMoiJson = InvoiceSnapshot(hd,items),
                MaNV = maNV,
                MaHD = hd.MaHD,
                TrangThai = "Hiệu lực"
            };

            _context.Phieus.Add(phieuGD);
            await _context.SaveChangesAsync();

            await _logger.LogAsync("HoaDon", $"Ghi đè hóa đơn {hd.MaHD} lần thứ {hd.SoLanGhiDe}, phiếu {maPhieu} (Cần thu thêm: {canThuThem:N0} đ, Phải trả khách: {phaiTraKhach:N0} đ)", lyDoGhiDe, maNV,
                JsonSerializer.Serialize(new {maPhieu,before=JsonSerializer.Deserialize<JsonElement>(snapshotGoc),after=JsonSerializer.Deserialize<JsonElement>(phieuGD.DuLieuMoiJson!)}));

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
            var json=Request.Headers.Accept.ToString().Contains("application/json");
            if (string.IsNullOrWhiteSpace(lyDoMoKhoa)||lyDoMoKhoa.Length>500)
            {
                if(json)return Json(new{success=false,message="Không hợp lệ!",errors=new Dictionary<string,string[]>{{"lyDoMoKhoa",new[]{"Không hợp lệ!"}}}});
                TempData["ErrorMessage"] = "Vui lòng nhập lý do xin mở khóa quyền (bắt buộc)!";
                return RedirectToAction("ChiTietHoaDon", new { id = maHD });
            }

            var hd = await _context.HoaDons.FindAsync(maHD);
            if (hd == null || !await OverrideBlocked(hd)) return BadRequest("Hóa đơn chưa bị khóa.");
            if (await _context.YeuCauMoKhoas.AnyAsync(y => y.MaHD == maHD && y.TrangThai == "Chờ duyệt"))
            {
                if(json)return Json(new{success=false,message="Hóa đơn đã có yêu cầu đang chờ duyệt."});
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

            await _logger.LogAsync("YeuCauMoKhoa", $"Gửi yêu cầu #{yc.MaYC} mở khóa ghi đè cho hóa đơn {maHD}", lyDoMoKhoa, maNV,
                JsonSerializer.Serialize(new {yc.MaYC,yc.MaHD,yc.MaNV,yc.ThoiGianGui,yc.TrangThai,yc.LyDoMoKhoa,hd.SoLanGhiDe,hd.IsLockedGhiDe}));

            if(json)return Json(new{success=true,message="Đã yêu cầu mở khóa."});
            TempData["SuccessMessage"] = "Đã yêu cầu mở khóa.";
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
            var lines = hd.ChiTietHoaDons.Select(c => new ReturnableLine { MaSP = c.MaSP, Name = c.TenSPDaChot ?? c.SanPham?.TenSP ?? c.MaSP,
                Unit = c.DonViTinhDaChot ?? c.SanPham?.DonViTinh ?? "", Purchased = c.SoLuong, Remaining = c.SoLuongConLai,
                Price = ReturnUnitPrice(hd,c.DonGiaBan) }).ToList();
            var exchanged = await _context.CT_Phieus.Where(c => c.Phieu!.MaHD == hd.MaHD && c.Phieu.TrangThai == "Hiệu lực" && c.LoaiDong == "DoiMoi")
                .Include(c => c.SanPham).Include(c => c.Phieu).ToListAsync();
            var returned = await _context.CT_Phieus.Where(c => c.Phieu!.MaHD == hd.MaHD && c.Phieu.TrangThai == "Hiệu lực" && c.SourceVoucherLineId != null)
                .GroupBy(c => c.SourceVoucherLineId).Select(g => new { Id = g.Key, Quantity = g.Sum(c => c.SoLuong) }).ToListAsync();
            foreach (var source in exchanged)
                lines.Add(new ReturnableLine { MaSP = source.MaSP, Name = source.TenSPDaChot ?? source.SanPham?.TenSP ?? source.MaSP, Unit = source.DonViTinhDaChot ?? source.SanPham?.DonViTinh ?? "",
                    Purchased = source.SoLuong, Remaining = Math.Max(source.SoLuong - (returned.FirstOrDefault(r => r.Id == source.Id)?.Quantity ?? 0), 0),
                    Price = source.DonGiaXuLy, SourceVoucherLineId = source.Id, SourceVoucher = source.MaPhieu });
            return lines;
        }

        // Apply the invoice's frozen discount/VAT once. Shipping and saved fees stay in its total.
        private static decimal ReturnUnitPrice(HoaDon invoice,decimal price)=>Math.Round(price
            *(1-(invoice.TongTienHang>0?invoice.TienChietKhau/invoice.TongTienHang:0))
            *(1+(invoice.TongTienHang>invoice.TienChietKhau?invoice.TienThue/(invoice.TongTienHang-invoice.TienChietKhau):0)),2);

        private static string NewCode(string prefix) => prefix + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..10];
        [HttpGet]
        public async Task<IActionResult> ChiTietPhieu(string id)
        {
            var voucher = await _context.Phieus.Include(p => p.NhanVien).Include(p => p.ChiTietPhieus).ThenInclude(c => c.SanPham).FirstOrDefaultAsync(p => p.MaPhieu == id);
            return voucher == null ? NotFound() : View(voucher);
        }
        private static bool ValidPayment(string? value) => value == "Tiền mặt" || value == "Chuyển khoản";
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
