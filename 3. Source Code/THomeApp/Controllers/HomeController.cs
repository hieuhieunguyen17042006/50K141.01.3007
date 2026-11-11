using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;

namespace THomeApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly THomeDbContext _context;
        private readonly THomeApp.Services.ProductSales _sales;

        public HomeController(THomeDbContext context, THomeApp.Services.ProductSales sales)
        {
            _context = context;
            _sales = sales;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Nhân viên bán hàng";
            if (userRole == "Nhân viên bán hàng") return RedirectToAction("Index", "BanHang");

            // Dashboard Metrics
            var todayInvoices = await _context.HoaDons
                .Where(h => h.NgayTao.Date == today && h.LoaiChungTu == "HoaDon")
                .ToListAsync();

            var todayRevenue = await _context.LanThuTiens.Where(l => l.NgayThu.Date == today && l.TrangThai == "Hiệu lực").SumAsync(l => l.SoTienThu)
                - await _context.Phieus.Where(p => p.NgayLap.Date == today && p.TrangThai == "Hiệu lực").SumAsync(p => p.TienThucHoan);
            var todayOrderCount = todayInvoices.Count;

            // Cong no con lai tren toan he thong
            var allActiveInvoices = await _context.HoaDons
                .Where(h => h.LoaiChungTu == "HoaDon")
                .ToListAsync();

            var totalDebt = allActiveInvoices.Sum(h => h.ConNo);
            var congTrinhCount = allActiveInvoices.Count(h => h.TyLeDatCoc > 0 && h.ConNo > 0);

            // NFR-03: Birthday alerts today
            var birthdayCustomers = await _context.KhachHangs
                .Where(k => k.NgaySinh.HasValue &&
                            k.NgaySinh.Value.Month == today.Month &&
                            k.NgaySinh.Value.Day == today.Day)
                .ToListAsync();

            // Discount milestone alerts: 500M (2%), 1B (3%)
            var milestoneCustomers = await _context.KhachHangs
                .Where(k => k.TongTichLuy >= 500_000_000m)
                .OrderByDescending(k => k.TongTichLuy)
                .ToListAsync();

            // Yeu cau mo khoa dang cho duyet (cho Chu cua hang)
            var pendingUnlockRequests = await _context.YeuCauMoKhoas
                .Include(y => y.NhanVien)
                .Include(y => y.HoaDon)
                .Where(y => y.TrangThai == "Chờ duyệt")
                .ToListAsync();

            // Recent invoices
            var recentInvoices = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.NhanVien)
                .OrderByDescending(h => h.NgayTao)
                .Take(6)
                .ToListAsync();

            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.TodayOrderCount = todayOrderCount;
            ViewBag.TotalDebt = totalDebt;
            ViewBag.CongTrinhCount = congTrinhCount;
            ViewBag.BirthdayCustomers = birthdayCustomers;
            ViewBag.MilestoneCustomers = milestoneCustomers;
            ViewBag.PendingUnlockRequests = pendingUnlockRequests;
            ViewBag.RecentInvoices = recentInvoices;
            ViewBag.UserRole = userRole;

            var since = today.AddDays(-6);
            var receipts = await _context.LanThuTiens.Where(l => l.NgayThu >= since && l.TrangThai == "Hiệu lực").ToListAsync();
            var refunds = await _context.Phieus.Where(p => p.NgayLap >= since && p.TrangThai == "Hiệu lực" && p.TienThucHoan > 0).ToListAsync();
            ViewBag.DailyRevenue = Enumerable.Range(0,7).Select(i => {
                var day = since.AddDays(i);
                return new THomeApp.Models.DailyRevenue { Day = day,
                    Cash = receipts.Where(r => r.NgayThu.Date == day && r.PhuongThucThanhToan == "Tiền mặt").Sum(r => r.SoTienThu)
                        - refunds.Where(r => r.NgayLap.Date == day && r.PhuongThucThanhToan == "Tiền mặt").Sum(r => r.TienThucHoan),
                    Transfer = receipts.Where(r => r.NgayThu.Date == day && r.PhuongThucThanhToan == "Chuyển khoản").Sum(r => r.SoTienThu)
                        - refunds.Where(r => r.NgayLap.Date == day && r.PhuongThucThanhToan == "Chuyển khoản").Sum(r => r.TienThucHoan) };
            }).ToList();
            ViewBag.TopProducts = (await _sales.ReadAsync()).OrderByDescending(p => p.Revenue).Take(7)
                .Select(p => new THomeApp.Models.ProductSummary { Name = p.Name, Quantity = p.Quantity, Revenue = p.Revenue }).ToList();

            return View();
        }
    }
}
