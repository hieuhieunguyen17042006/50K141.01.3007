using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Models;

namespace THomeApp.Services;

public sealed class RevenueReports(THomeDbContext db)
{
    public static (DateTime From, DateTime To) Period(DateTime? from, DateTime? to)
    {
        var start = (from ?? DateTime.Today.AddDays(-30)).Date;
        var finish = (to ?? DateTime.Today).Date;
        if (start > finish || finish >= DateTime.MaxValue.Date)
            throw new ArgumentException("Kỳ báo cáo không hợp lệ: ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        return (start, finish);
    }

    public async Task<RevenueReport> ReadAsync(DateTime? from, DateTime? to)
    {
        var (start, finish) = Period(from, to);
        var end = finish.AddDays(1);
        // Keep the invoice's stored prices and discounts; never read today's product price.
        var invoices = await db.HoaDons.AsNoTracking().Include(h => h.KhachHang).Include(h => h.NhanVien)
            .Where(h => h.LoaiChungTu == "HoaDon" && h.TrangThaiThanhToan != "Đã hủy"
                && (h.NgayPhatHanh ?? h.NgayTao) >= start && (h.NgayPhatHanh ?? h.NgayTao) < end)
            .OrderBy(h => h.NgayPhatHanh ?? h.NgayTao).ToListAsync();
        var receipts = await db.LanThuTiens.AsNoTracking().Where(r => r.TrangThai == "Hiệu lực" && r.NgayThu >= start && r.NgayThu < end)
            .OrderBy(r => r.NgayThu).ToListAsync();
        var vouchers = await db.Phieus.AsNoTracking().Include(p => p.HoaDon)
            .Where(p => p.TrangThai == "Hiệu lực" && p.NgayLap >= start && p.NgayLap < end).OrderBy(p => p.NgayLap).ToListAsync();
        // A return/exchange affects revenue once via its delta, never via both the
        // mutable invoice total and its voucher; an actual refund affects cash only.
        static decimal Adjustment(Phieu p) => p.LoaiPhieu == "ChiPhiPhatSinh" && p.LoaiChiPhiPhatSinh == "ThuThemKhach" ? p.SoTien
            : p.LoaiPhieu is "TraHang" or "DoiHang" ? p.TienChenhLech : 0;
        var issuedIds = invoices.Select(h => h.MaHD).ToHashSet();
        var adjustments = vouchers.Where(p => p.HoaDon?.LoaiChungTu == "HoaDon" && p.HoaDon.TrangThaiThanhToan != "Đã hủy"
            && (p.HoaDon.NgayPhatHanh ?? p.HoaDon.NgayTao) < end).Sum(Adjustment);
        // Include any adjustments made on a draft before its issue date in this
        // period as part of that issue, without adding the same voucher twice.
        adjustments += await db.Phieus.AsNoTracking().Where(p => p.TrangThai == "Hiệu lực" && issuedIds.Contains(p.MaHD) && p.NgayLap < start)
            .Select(p => p.LoaiPhieu == "ChiPhiPhatSinh" && p.LoaiChiPhiPhatSinh == "ThuThemKhach" ? p.SoTien
                : p.LoaiPhieu == "TraHang" || p.LoaiPhieu == "DoiHang" ? p.TienChenhLech : 0).SumAsync();

        var orders = await db.HoaDons.AsNoTracking().Include(h => h.LanThuTiens).Include(h => h.Phieus).AsSplitQuery()
            .Where(h => h.NgayTao < end && h.TrangThaiThanhToan != "Đã hủy").ToListAsync();
        decimal debt = 0;
        foreach (var h in orders)
        {
            var active = h.Phieus.Where(p => p.TrangThai == "Hiệu lực" && p.NgayLap < end).ToList();
            var ledger = h.LanThuTiens.Where(r => r.TrangThai == "Hiệu lực" && r.NgayThu < end).Sum(r => r.SoTienThu);
            // Historical seed invoices predate the receipt ledger. Preserve their
            // paid balance for debt, but never invent a receipt or receipt date.
            var legacy = h.LanThuTiens.Count == 0 && h.DaThu > 0;
            if (legacy) ledger = h.DaThu;
            var formalAtEnd = h.LoaiChungTu == "HoaDon" && (h.NgayPhatHanh ?? h.NgayTao) < end;
            if (!formalAtEnd && ledger <= 0) continue;
            var due = h.TongTienHang - h.TienChietKhau + h.ChiPhiVanChuyen + h.TienThue + active.Sum(Adjustment);
            debt += Math.Max(due - ledger + active.Sum(p => p.TienThucHoan), 0);
        }
        decimal Collected(string method) => receipts.Where(r => r.PhuongThucThanhToan == method).Sum(r => r.SoTienThu)
            - vouchers.Where(p => p.PhuongThucThanhToan == method).Sum(p => p.TienThucHoan);
        var relevant = vouchers.Where(p => Adjustment(p) != 0 || p.TienThucHoan > 0 || p.LoaiChiPhiPhatSinh == "CuaHangChi").ToList();
        return new RevenueReport
        {
            From = start, To = finish, Invoices = invoices, Receipts = receipts, Vouchers = relevant,
            Gross = invoices.Sum(h => h.TongTienHang + h.ChiPhiVanChuyen) + adjustments,
            Discount = invoices.Sum(h => h.TienChietKhau), Vat = invoices.Sum(h => h.TienThue),
            Cost = vouchers.Where(p => p.LoaiPhieu == "ChiPhiPhatSinh" && p.LoaiChiPhiPhatSinh == "CuaHangChi").Sum(p => p.SoTien),
            Cash = Collected("Tiền mặt"), Bank = Collected("Chuyển khoản"), Debt = debt,
            HasData = invoices.Count > 0 || receipts.Count > 0 || relevant.Count > 0,
            LegacyWithoutReceipts = orders.Count(h => h.LoaiChungTu == "HoaDon" && (h.NgayPhatHanh ?? h.NgayTao) < end && h.DaThu > 0 && h.LanThuTiens.Count == 0)
        };
    }
}
