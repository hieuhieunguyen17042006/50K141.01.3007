using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using THomeApp.Data;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers
{
    [Authorize]
    public class DoanhThuController(THomeDbContext context, IActivityLogger logger, ProductSales sales, RevenueReports reports) : Controller
    {
        private readonly THomeDbContext _context = context;
        private readonly IActivityLogger _logger = logger;
        private bool CheckPermission() => User.FindFirstValue(ClaimTypes.Role) is "Chủ cửa hàng" or "Kế toán bán hàng";

        public IActionResult Index(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            // Preserve bookmarked report filters while making Index the function chooser.
            if (fromDate.HasValue || toDate.HasValue) return RedirectToAction(nameof(BaoCao), new { fromDate, toDate });
            return View();
        }

        public async Task<IActionResult> BaoCao(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            if (!ModelState.IsValid) return BadRequest("Ngày báo cáo không hợp lệ.");
            try { return View(await reports.ReadAsync(fromDate, toDate)); }
            catch (ArgumentException e) { return BadRequest(e.Message); }
        }

        public IActionResult ChonTieuChi()
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            return View();
        }

        public async Task<IActionResult> BanChay(string tieuChi = "ca2", DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            tieuChi = tieuChi.ToLowerInvariant();
            if (!new[] { "ca2", "doanhthu", "soluong" }.Contains(tieuChi)) return BadRequest("Tiêu chí thống kê không hợp lệ.");
            if (!ModelState.IsValid || (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                || toDate?.Date >= DateTime.MaxValue.Date) return BadRequest("Kỳ thống kê không hợp lệ.");
            var rows = await sales.ReadAsync(fromDate, toDate);
            return View(new ProductStatistics {
                Criterion = tieuChi, From = fromDate, To = toDate,
                ByRevenue = rows.OrderByDescending(p => p.Revenue).ThenByDescending(p => p.Quantity).ThenBy(p => p.Code).ToList(),
                ByQuantity = rows.OrderByDescending(p => p.Quantity).ThenByDescending(p => p.Revenue).ThenBy(p => p.Code).ToList()
            });
        }

        // ==========================================
        // 3. ĐỐI SOÁT TIỀN MẶT THEO CA TRỰC (BR-UC4.2)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> DoiSoatCa(string? caLamViec = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");

            caLamViec ??= "Cuối ngày (Tổng kết)";
            var (start, end) = ShiftRange(caLamViec);
            ViewBag.CaLamViec = caLamViec;
            var tienMatRongPhanMem = await CashBalance(start, end);

            ViewBag.TienMatPhanMem = tienMatRongPhanMem;

            var history = await _context.KiemKeTienMats
                .Include(k => k.NhanVien)
                .OrderByDescending(k => k.NgayTao)
                .Take(10)
                .ToListAsync();

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LuuDoiSoatCa(string caLamViec, decimal tienMatPhanMem, decimal tienMatKiemDem, string? ghiChu)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");

            if (tienMatKiemDem < 0 || !new[] { "Ca sáng (07:30 - 12:00)", "Ca chiều (13:30 - 17:30)", "Cuối ngày (Tổng kết)" }.Contains(caLamViec))
                return BadRequest("Ca làm việc hoặc tiền kiểm đếm không hợp lệ.");
            var (start, end) = ShiftRange(caLamViec);
            tienMatPhanMem = await CashBalance(start, end);
            var maNV = User.FindFirstValue("MaNV") ?? "NV002";

            var kiemKe = new KiemKeTienMat
            {
                NgayKiemKe = DateTime.Today,
                CaLamViec = caLamViec,
                MaNV = maNV,
                TienMatPhanMem = tienMatPhanMem,
                TienMatKiemDem = tienMatKiemDem,
                GhiChu = ghiChu,
                NgayTao = DateTime.Now
            };

            _context.KiemKeTienMats.Add(kiemKe);
            await _context.SaveChangesAsync();

            decimal diff = tienMatKiemDem - tienMatPhanMem;
            string msg = diff == 0 ? "Khớp hoàn toàn 100%!" : (diff > 0 ? $"Thừa quỹ {diff:N0} đ" : $"Thiếu quỹ {Math.Abs(diff):N0} đ");

            await _logger.LogAsync("DoanhThu", $"Đối soát tiền mặt {caLamViec}: Phần mềm {tienMatPhanMem:N0} đ, Kiểm đếm {tienMatKiemDem:N0} đ ({msg})", ghiChu, maNV);

            TempData["SuccessMessage"] = $"Lưu đối soát tiền mặt {caLamViec} thành công! Kết quả: {msg}";
            return RedirectToAction("DoiSoatCa");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            if (!ModelState.IsValid) return BadRequest(new { message = "Ngày báo cáo không hợp lệ." });
            RevenueReport report;
            try { report = await reports.ReadAsync(fromDate, toDate); }
            catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
            if (!report.HasData) return NotFound(new { message = "Không có dữ liệu!" });
            using var workbook = new XLWorkbook();
            var summary = workbook.Worksheets.Add("BaoCaoDoanhThu");
            summary.Cell(1, 1).Value = "T-HOME — BÁO CÁO DOANH THU";
            summary.Cell(2, 1).Value = report.Period;
            int r = 4;
            foreach (var (label, amount) in report.Summary) {
                summary.Cell(r, 1).Value = label;
                summary.Cell(r++, 2).Value = amount;
            }
            summary.Column(2).Style.NumberFormat.Format = "#,##0";
            summary.Cell(++r, 1).Value = RevenueReport.Basis;
            summary.Range(r, 1, r, 5).Merge().Style.Alignment.SetWrapText();
            summary.Row(r).Height = 85;
            if (report.LegacyWithoutReceipts > 0)
                summary.Cell(++r, 1).Value = $"{report.LegacyWithoutReceipts} hóa đơn dữ liệu cũ chưa có sổ thu: không tính thành tiền thực thu trong kỳ; công nợ dùng số dư đã lưu.";
            summary.Column(1).Width = 48; summary.Column(2).Width = 24;
            var invoices = workbook.Worksheets.Add("HoaDonPhatHanh");
            string[] headers = ["STT", "Mã hóa đơn", "Đơn gốc", "Ngày phát hành", "Khách hàng", "Doanh thu trước CK", "Chiết khấu", "VAT", "Nhân viên lập"];
            for (var i = 0; i < headers.Length; i++) invoices.Cell(1, i + 1).Value = headers[i];
            r = 2;
            foreach (var h in report.Invoices) {
                invoices.Cell(r, 1).Value = r - 1;
                invoices.Cell(r, 2).Value = h.MaHoaDon ?? h.MaHD;
                invoices.Cell(r, 3).Value = h.MaHD;
                invoices.Cell(r, 4).Value = (h.NgayPhatHanh ?? h.NgayTao).ToString("dd/MM/yyyy HH:mm");
                invoices.Cell(r, 5).Value = h.KhachHang?.HoTen ?? "Khách lẻ";
                invoices.Cell(r, 6).Value = h.TongTienHang + h.ChiPhiVanChuyen;
                invoices.Cell(r, 7).Value = h.TienChietKhau;
                invoices.Cell(r, 8).Value = h.TienThue;
                invoices.Cell(r++, 9).Value = h.NhanVien?.HoTen ?? h.MaNV;
            }
            invoices.Columns(6, 8).Style.NumberFormat.Format = "#,##0";
            var receipts = workbook.Worksheets.Add("TienThucThu");
            string[] receiptHeaders = ["Mã lần thu", "Đơn gốc", "Ngày thu", "Phương thức", "Số tiền", "Nội dung"];
            for (var i = 0; i < receiptHeaders.Length; i++) receipts.Cell(1, i + 1).Value = receiptHeaders[i];
            r = 2;
            foreach (var receipt in report.Receipts) {
                receipts.Cell(r, 1).Value = receipt.MaLanThu;
                receipts.Cell(r, 2).Value = receipt.MaHD;
                receipts.Cell(r, 3).Value = receipt.NgayThu.ToString("dd/MM/yyyy HH:mm");
                receipts.Cell(r, 4).Value = receipt.PhuongThucThanhToan;
                receipts.Cell(r, 5).Value = receipt.SoTienThu;
                receipts.Cell(r++, 6).Value = receipt.NoiDung;
            }
            receipts.Column(5).Style.NumberFormat.Format = "#,##0";
            var vouchers = workbook.Worksheets.Add("DieuChinhVaChiPhi");
            string[] voucherHeaders = ["Mã phiếu", "Đơn gốc", "Ngày lập", "Loại phiếu", "Loại chi phí", "Số tiền chi phí", "Chênh lệch đổi trả", "Tiền thực hoàn", "Phương thức"];
            for (var i = 0; i < voucherHeaders.Length; i++) vouchers.Cell(1, i + 1).Value = voucherHeaders[i];
            r = 2;
            foreach (var p in report.Vouchers) {
                vouchers.Cell(r, 1).Value = p.MaPhieu;
                vouchers.Cell(r, 2).Value = p.MaHD;
                vouchers.Cell(r, 3).Value = p.NgayLap.ToString("dd/MM/yyyy HH:mm");
                vouchers.Cell(r, 4).Value = p.LoaiPhieu;
                vouchers.Cell(r, 5).Value = p.LoaiChiPhiPhatSinh ?? "";
                vouchers.Cell(r, 6).Value = p.SoTien;
                vouchers.Cell(r, 7).Value = p.TienChenhLech;
                vouchers.Cell(r, 8).Value = p.TienThucHoan;
                vouchers.Cell(r++, 9).Value = p.PhuongThucThanhToan;
            }
            vouchers.Columns(6, 8).Style.NumberFormat.Format = "#,##0";
            foreach (var sheet in workbook.Worksheets) {
                sheet.Row(1).Style.Font.Bold = true;
                sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#083e70");
                sheet.Row(1).Style.Font.FontColor = XLColor.White;
                sheet.SheetView.FreezeRows(1);
                if (sheet != summary) sheet.Columns().AdjustToContents(8, 55);
            }
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            await LogExport("Excel", report);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"THome_DoanhThu_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!CheckPermission()) return RedirectToAction("AccessDenied", "Auth");
            if (!ModelState.IsValid) return BadRequest(new { message = "Ngày báo cáo không hợp lệ." });
            RevenueReport report;
            try { report = await reports.ReadAsync(fromDate, toDate); }
            catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
            if (!report.HasData) return NotFound(new { message = "Không có dữ liệu!" });
            var document = Document.Create(container => container.Page(page => {
                page.Size(PageSizes.A4.Landscape()); page.Margin(24); page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Column(c => {
                    c.Item().Text("T-HOME — BÁO CÁO DOANH THU").FontSize(18).Bold().FontColor("#083e70");
                    c.Item().Text(report.Period).FontSize(12);
                });
                page.Content().PaddingVertical(15).Column(c => {
                    c.Spacing(12);
                    c.Item().Table(t => {
                        t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(2); });
                        foreach (var (label, amount) in report.Summary) {
                            t.Cell().Background("#eef8fc").Padding(6).Text(label);
                            t.Cell().Padding(6).AlignRight().Text(RevenueReport.Money(amount)).Bold();
                        }
                    });
                    c.Item().Text(RevenueReport.Basis).FontSize(9);
                    if (report.LegacyWithoutReceipts > 0)
                        c.Item().Text($"Lưu ý: {report.LegacyWithoutReceipts} hóa đơn dữ liệu cũ thiếu sổ thu. Không tự tính thành tiền thực thu; công nợ dùng số dư đã lưu.").FontSize(9);
                    c.Item().Text("Hóa đơn phát hành trong kỳ").Bold();
                    c.Item().Table(t => {
                        t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(2); d.RelativeColumn(3); d.RelativeColumn(2); d.RelativeColumn(2); });
                        t.Header(h => { foreach (var title in new[] { "Mã hóa đơn / đơn gốc", "Ngày phát hành", "Khách hàng", "Trước chiết khấu", "Chiết khấu" })
                            h.Cell().Background("#083e70").Padding(5).Text(title).FontColor(Colors.White); });
                        foreach (var h in report.Invoices) {
                            t.Cell().Padding(5).Text($"{h.MaHoaDon ?? h.MaHD} / {h.MaHD}");
                            t.Cell().Padding(5).Text($"{h.NgayPhatHanh ?? h.NgayTao:dd/MM/yyyy}");
                            t.Cell().Padding(5).Text(h.KhachHang?.HoTen ?? "Khách lẻ");
                            t.Cell().Padding(5).AlignRight().Text(RevenueReport.Money(h.TongTienHang + h.ChiPhiVanChuyen));
                            t.Cell().Padding(5).AlignRight().Text(RevenueReport.Money(h.TienChietKhau));
                        }
                    });
                });
                page.Footer().AlignCenter().Text(x => { x.Span("Trang "); x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            }));
            var bytes = document.GeneratePdf();
            await LogExport("PDF", report);
            return File(bytes, "application/pdf", $"THome_DoanhThu_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.pdf");
        }

        private Task LogExport(string type, RevenueReport report) => _logger.LogAsync("DoanhThu",
            $"Xuất báo cáo doanh thu {type} ({report.From:dd/MM/yyyy} - {report.To:dd/MM/yyyy})", $"Xuất {type}", User.FindFirstValue("MaNV") ?? "NV002");

        private static (DateTime Start, DateTime End) ShiftRange(string shift) => shift.StartsWith("Ca sáng")
            ? (DateTime.Today.AddHours(7.5), DateTime.Today.AddHours(12)) : shift.StartsWith("Ca chiều")
            ? (DateTime.Today.AddHours(13.5), DateTime.Today.AddHours(17.5)) : (DateTime.Today, DateTime.Today.AddDays(1));

        private async Task<decimal> CashBalance(DateTime start, DateTime end)
        {
            var collected = await _context.LanThuTiens.Where(l => l.NgayThu >= start && l.NgayThu < end && l.PhuongThucThanhToan == "Tiền mặt" && l.TrangThai == "Hiệu lực").SumAsync(l => l.SoTienThu);
            var expenses = await _context.Phieus.Where(p => p.NgayLap >= start && p.NgayLap < end && p.PhuongThucThanhToan == "Tiền mặt" && p.TrangThai == "Hiệu lực")
                .SumAsync(p => p.LoaiPhieu == "ChiPhiPhatSinh" && p.LoaiChiPhiPhatSinh == "CuaHangChi" ? p.SoTien : p.TienThucHoan);
            return collected - expenses;
        }
    }
}
