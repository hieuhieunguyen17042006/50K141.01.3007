using Microsoft.EntityFrameworkCore;
using THomeApp.Data;

namespace THomeApp.Services;
public class ProductSale
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
public class ProductSales(THomeDbContext db)
{
    public async Task<List<ProductSale>> ReadAsync(DateTime? from = null, DateTime? to = null)
    {
        // BR-UC4.1-01: only completed sales, excluding pending/cancelled orders.
        var invoices = db.HoaDons.Where(h => h.LoaiChungTu == "HoaDon" && h.TrangThaiThanhToan == "Hoàn thành"
            && h.TongThanhToan > 0 && h.TongThanhToan <= h.DaThu - h.DaHoanKhach);
        if (from.HasValue) { var start = from.Value.Date; invoices = invoices.Where(h => (h.NgayPhatHanh ?? h.NgayTao) >= start); }
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); invoices = invoices.Where(h => (h.NgayPhatHanh ?? h.NgayTao) < end); }
        var original = await db.CT_HoaDons.Where(c => invoices.Any(h => h.MaHD == c.MaHD))
            .Select(c => new ProductSale { Code = c.MaSP, Name = c.SanPham!.TenSP, Category = c.SanPham.DanhMuc, Unit = c.SanPham.DonViTinh,
                Quantity = c.SoLuong - c.SoLuongDaTra,
                Revenue = (c.SoLuong - c.SoLuongDaTra) * c.DonGiaBan * (1 - (c.HoaDon!.TongTienHang > 0 ? c.HoaDon.TienChietKhau / c.HoaDon.TongTienHang : 0)) }).ToListAsync();
        var exchanged = await db.CT_Phieus.Where(c => c.LoaiDong == "DoiMoi" && c.Phieu!.TrangThai == "Hiệu lực" && invoices.Any(h => h.MaHD == c.Phieu.MaHD))
            .Select(c => new ProductSale { Code = c.MaSP, Name = c.SanPham!.TenSP, Category = c.SanPham.DanhMuc, Unit = c.SanPham.DonViTinh,
                Quantity = c.SoLuong - db.CT_Phieus.Where(t => t.SourceVoucherLineId == c.Id && t.Phieu!.TrangThai == "Hiệu lực").Sum(t => t.SoLuong),
                Revenue = (c.SoLuong - db.CT_Phieus.Where(t => t.SourceVoucherLineId == c.Id && t.Phieu!.TrangThai == "Hiệu lực").Sum(t => t.SoLuong)) * c.DonGiaXuLy }).ToListAsync();
        return original.Concat(exchanged).GroupBy(p => p.Code).Select(g => new ProductSale { Code = g.Key, Name = g.First().Name,
            Category = g.First().Category, Unit = g.First().Unit, Quantity = g.Sum(p => p.Quantity), Revenue = g.Sum(p => p.Revenue) })
            .Where(p => p.Quantity > 0).ToList();
    }
}
