using Microsoft.EntityFrameworkCore;
using THomeApp.Data;

namespace THomeApp.Services;

public sealed record CategoryRevenue(string Name, List<decimal> Values);
public sealed class DashboardView
{
    public decimal NetToday { get; init; }
    public List<DateTime> Days { get; init; } = [];
    public List<CategoryRevenue> Categories { get; init; } = [];
    public List<ProductSale> ByQuantity { get; init; } = [];
    public List<ProductSale> ByRevenue { get; init; } = [];
    public List<string> BirthdayNames { get; init; } = [];
    public int PendingUnlockRequests { get; init; }
}

public sealed class HomeDashboard(THomeDbContext db, RevenueReports reports, ProductSales sales)
{
    public async Task<DashboardView> ReadAsync(bool owner)
    {
        var today=DateTime.Today;var start=today.AddDays(-6);var end=today.AddDays(1);
        var days=Enumerable.Range(0,7).Select(i=>start.AddDays(i)).ToList();
        var values=new Dictionary<string,decimal[]>();
        void Add(string? category,DateTime date,decimal amount)
        {
            var index=(date.Date-start).Days;if(index<0||index>=days.Count)return;
            var key=string.IsNullOrWhiteSpace(category)?"Chưa phân loại":category.Trim();
            if(!values.TryGetValue(key,out var series))values[key]=series=new decimal[days.Count];
            series[index]+=amount;
        }
        // Product/category revenue comes from issued line snapshots, never payment methods or deposits.
        var invoices=await db.HoaDons.AsNoTracking().Include(h=>h.ChiTietHoaDons).ThenInclude(c=>c.SanPham)
            .Where(h=>h.LoaiChungTu=="HoaDon"&&h.TrangThaiThanhToan!="Đã hủy"&&(h.NgayPhatHanh??h.NgayTao)>=start&&(h.NgayPhatHanh??h.NgayTao)<end).ToListAsync();
        foreach(var invoice in invoices)
        {
            var factor=invoice.TongTienHang>0?1-invoice.TienChietKhau/invoice.TongTienHang:1;
            foreach(var line in invoice.ChiTietHoaDons)Add(line.SanPham?.DanhMuc,invoice.NgayPhatHanh??invoice.NgayTao,line.SoLuong*line.DonGiaBan*factor);
        }
        var vouchers=await db.Phieus.AsNoTracking().Include(p=>p.HoaDon).Include(p=>p.ChiTietPhieus).ThenInclude(c=>c.SanPham)
            .Where(p=>p.TrangThai=="Hiệu lực"&&(p.LoaiPhieu=="TraHang"||p.LoaiPhieu=="DoiHang")&&p.NgayLap>=start&&p.NgayLap<end
                &&p.HoaDon!.LoaiChungTu=="HoaDon"&&p.HoaDon.TrangThaiThanhToan!="Đã hủy").ToListAsync();
        foreach(var voucher in vouchers)
        {
            var h=voucher.HoaDon!;var baseAmount=h.TongTienHang-h.TienChietKhau;
            var vatFactor=baseAmount>0?1+h.TienThue/baseAmount:1;
            foreach(var line in voucher.ChiTietPhieus)
                Add(line.SanPham?.DanhMuc,voucher.NgayLap,(line.LoaiDong=="TraHang"?-1:1)*line.SoLuong*line.DonGiaXuLy/vatFactor);
        }
        var ordered=values.OrderByDescending(p=>p.Value.Sum(Math.Abs)).ThenBy(p=>p.Key).ToList();
        var categories=ordered.Take(3).Select(p=>new CategoryRevenue(p.Key,p.Value.ToList())).ToList();
        if(ordered.Count>3)categories.Add(new CategoryRevenue("Loại hàng khác",Enumerable.Range(0,7).Select(i=>ordered.Skip(3).Sum(p=>p.Value[i])).ToList()));
        var products=await sales.ReadAsync();var report=await reports.ReadAsync(today,today);
        var birthdays=await db.KhachHangs.AsNoTracking().Where(k=>k.NgaySinh.HasValue&&k.NgaySinh.Value.Month==today.Month&&k.NgaySinh.Value.Day==today.Day).Select(k=>k.HoTen).ToListAsync();
        var pending=owner?await db.YeuCauMoKhoas.CountAsync(y=>y.TrangThai=="Chờ duyệt"):0;
        return new DashboardView {NetToday=report.Net,Days=days,Categories=categories,BirthdayNames=birthdays,PendingUnlockRequests=pending,
            ByQuantity=products.OrderByDescending(p=>p.Quantity).ThenBy(p=>p.Code).Take(7).ToList(),
            ByRevenue=products.OrderByDescending(p=>p.Revenue).ThenBy(p=>p.Code).Take(7).ToList()};
    }
}
