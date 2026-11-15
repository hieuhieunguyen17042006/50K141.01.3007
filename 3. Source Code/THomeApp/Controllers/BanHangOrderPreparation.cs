using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers;

public partial class BanHangController
{
    private const string PreparationScopeKey = OrderPreparationStore.SessionScopeKey;
    private string OrderActor => User.FindFirstValue("MaNV")!;
    private OrderPreparationStore Preparations => HttpContext.RequestServices.GetRequiredService<OrderPreparationStore>();
    private string EnsurePreparationScope()
    {
        var scope = HttpContext.Session.GetString(PreparationScopeKey);
        if (scope == null) HttpContext.Session.SetString(PreparationScopeKey, scope = Guid.NewGuid().ToString("N"));
        return scope;
    }
    private Task<PreparedOrder?> GetPreparedOrder(string? id) => Preparations.GetAsync(OrderActor, HttpContext.Session.GetString(PreparationScopeKey), id);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = InvoiceRoles)]
    public async Task<IActionResult> ChuanBiDonTamTinh([FromBody] TaoDonTamTinhRequest model)
    {
        var scope = EnsurePreparationScope();
        var order = await ValidateOrder(model, NewCode("TT"));
        if (order == null) return OrderErrors();
        var prepared = new PreparedOrder(Guid.NewGuid().ToString("N"), OrderActor, JsonSerializer.Serialize(model), order,
            DateTimeOffset.UtcNow.Add(OrderPreparationStore.Lifetime));
        await Preparations.StoreAsync(scope, prepared);
        return Json(new { success = true, preparedId = prepared.Id, redirect = Url.Action("MaQR", new { preparedId = prepared.Id }) });
    }

    public sealed class SavePreparedOrderRequest { public string PreparedId { get; set; } = ""; }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = InvoiceRoles)]
    public async Task<IActionResult> LuuDonDaChuanBi([FromBody] SavePreparedOrderRequest request)
    {
        var prepared = await GetPreparedOrder(request.PreparedId);
        if (prepared == null) return Json(new { success = false, message = "Dữ liệu chuẩn bị đã hết hiệu lực. Vui lòng mở lại màn nhập đơn." });
        // Also guard by the reserved invoice key, even when a caller supplies another request id.
        var saved = await _context.HoaDons.AsNoTracking().FirstOrDefaultAsync(h => h.MaHD == prepared.Preview.MaHD && h.MaNV == OrderActor);
        if (saved != null) return PreparedSaved(saved);
        var model = JsonSerializer.Deserialize<TaoDonTamTinhRequest>(prepared.InputJson)!;
        var order = await ValidateOrder(model, prepared.Preview.MaHD);
        if (order == null) return OrderErrors();
        if (OrderFingerprint(order) != OrderFingerprint(prepared.Preview))
            return Json(new { success = false, message = "Giá, chiết khấu hoặc số tiền đã thay đổi. Bấm Hủy để kiểm tra lại đơn và tạo mã QR mới. Chưa lưu đơn." });
        await PersistOrder(order);
        return PreparedSaved(order);
    }

    private IActionResult PreparedSaved(HoaDon order) => Json(new { success = true, maHD = order.MaHD,
        message = "Tạo đơn tạm tính thành công", redirect = Url.Action("ChiTietHoaDon", new { id = order.MaHD }) });

    private IActionResult OrderErrors() => Json(new { success = false, message = "Thông tin đơn chưa hợp lệ. Vui lòng kiểm tra các ô được đánh dấu.",
        errors = ModelState.Where(p => p.Value?.Errors.Count > 0).ToDictionary(p => p.Key, p => p.Value!.Errors.Select(e => e.ErrorMessage).ToArray()) });

    private async Task<HoaDon?> ValidateOrder(TaoDonTamTinhRequest? model, string code)
    {
        if (model == null) { ModelState.AddModelError("MaKH", "Vui lòng nhập thông tin đơn."); return null; }
        var customer = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKH == model.MaKH);
        if (customer == null) ModelState.AddModelError("MaKH", "Vui lòng chọn khách hàng hợp lệ.");
        if (model.DiaChiGiao?.Length > 255) ModelState.AddModelError("DiaChiGiao", "Địa chỉ tối đa 255 ký tự.");
        if (model.GhiChu?.Length > 500) ModelState.AddModelError("GhiChu", "Ghi chú tối đa 500 ký tự.");
        if (model.ChiPhiVanChuyen < 0 || model.ChiPhiVanChuyen > 1_000_000_000_000m) ModelState.AddModelError("ChiPhiVanChuyen", "Phí vận chuyển không hợp lệ.");
        if (model.IsDatCoc && (model.TyLeDatCoc < 30 || model.TyLeDatCoc > 50)) ModelState.AddModelError("TyLeDatCoc", "Tỷ lệ đặt cọc phải từ 30% đến 50%.");
        if (model.IsDatCoc && model.HanThanhToan?.Date < DateTime.Today) ModelState.AddModelError("HanThanhToan", "Hạn thanh toán không được trước ngày hôm nay.");
        if (model.SanPhams == null || model.SanPhams.Count == 0 || model.SanPhams.Count > 100) ModelState.AddModelError("SanPhams", "Chọn từ 1 đến 100 dòng sản phẩm.");
        var lines = new List<CT_HoaDon>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < (model.SanPhams?.Count ?? 0); i++)
        {
            var item = model.SanPhams![i];
            if (item == null) { ModelState.AddModelError($"SanPhams[{i}].MaSP", "Vui lòng chọn sản phẩm."); continue; }
            if (item.SoLuong <= 0 || item.SoLuong > 100000) ModelState.AddModelError($"SanPhams[{i}].SoLuong", "Số lượng phải là số nguyên từ 1 đến 100.000.");
            if (!seen.Add(item.MaSP)) ModelState.AddModelError($"SanPhams[{i}].MaSP", "Sản phẩm đã có ở dòng khác.");
            var product = await _context.SanPhams.AsNoTracking().FirstOrDefaultAsync(p => p.MaSP == item.MaSP && !p.IsDeleted);
            var price = await _context.Gias.AsNoTracking().Where(g => g.MaSP == item.MaSP && g.NgayApDung <= DateTime.Now)
                .OrderByDescending(g => g.NgayApDung).ThenByDescending(g => g.MaGia).Select(g => g.DonGia).FirstOrDefaultAsync();
            if (product == null || price <= 0) { ModelState.AddModelError($"SanPhams[{i}].MaSP", "Sản phẩm không còn bán hoặc chưa có giá hiệu lực."); continue; }
            lines.Add(new CT_HoaDon { MaHD = code, MaSP = product.MaSP, SoLuong = item.SoLuong, DonGiaBan = price,
                SanPham = new SanPham { MaSP = product.MaSP, TenSP = product.TenSP, DonViTinh = product.DonViTinh } });
        }
        if (!ModelState.IsValid) return null;
        var subtotal = lines.Sum(l => l.ThanhTien);
        var discount = await _context.ChietKhaus.AsNoTracking().Where(c => c.DangApDung && c.MocTichLuy <= customer!.TongTichLuy)
            .OrderByDescending(c => c.MocTichLuy).FirstOrDefaultAsync();
        var discountAmount = Math.Round(subtotal * (discount?.TyLeChietKhau ?? 0) / 100m, 0);
        var tax = model.ApDungVAT ? Math.Round((subtotal - discountAmount) * 0.1m, 0) : 0;
        var total = subtotal - discountAmount + tax + Math.Round(model.ChiPhiVanChuyen, 2);
        if (total <= 0 || total > 1_000_000_000_000m) { ModelState.AddModelError("TongThanhToan", "Tổng thanh toán phải lớn hơn 0 và không vượt 1.000.000.000.000 đ."); return null; }
        return new HoaDon { MaHD = code, MaKH = customer!.MaKH, MaNV = OrderActor, LoaiChungTu = "DonTamTinh", NgayTao = DateTime.Now,
            TrangThaiThanhToan = "Chưa thanh toán", PhuongThucThanhToan = "Chưa xác định", DiaChiGiao = model.DiaChiGiao ?? customer.DiaChi,
            GhiChu = model.GhiChu, ChiPhiVanChuyen = Math.Round(model.ChiPhiVanChuyen, 2), TongTienHang = subtotal, MaCK = discount?.MaCK,
            TienChietKhau = discountAmount, TienThue = tax, TongThanhToan = total, TyLeDatCoc = model.IsDatCoc ? model.TyLeDatCoc : 0,
            TienDatCoc = model.IsDatCoc ? Math.Round(total * model.TyLeDatCoc / 100m, 0) : 0, HanThanhToan = model.IsDatCoc ? model.HanThanhToan : null,
            DaThu = 0, DaHoanKhach = 0, ChiTietHoaDons = lines,
            KhachHang = new KhachHang { MaKH = customer.MaKH, HoTen = customer.HoTen, SDT = customer.SDT, DiaChi = customer.DiaChi } };
    }

    private async Task PersistOrder(HoaDon order)
    {
        var customerName = order.KhachHang?.HoTen;
        order.KhachHang = null;
        foreach (var line in order.ChiTietHoaDons) line.SanPham = null;
        // Parent and all children are inserted in one SaveChanges within DocumentWriteFilter's transaction.
        _context.HoaDons.Add(order); await _context.SaveChangesAsync();
        await _logger.LogAsync("DonHang", $"Tạo đơn tạm tính {order.MaHD} cho khách hàng {customerName}", "Tạo đơn tạm tính", OrderActor);
    }

    private static string OrderFingerprint(HoaDon order)
    {
        string N(decimal n) => n.ToString("G29", CultureInfo.InvariantCulture);
        return string.Join("|", order.MaKH, order.MaCK, N(order.TongTienHang), N(order.TienChietKhau), N(order.ChiPhiVanChuyen), N(order.TienThue),
            N(order.TongThanhToan), N(order.TienDatCoc), N(order.TyLeDatCoc), order.PhuongThucThanhToan,
            string.Join(";", order.ChiTietHoaDons.OrderBy(l => l.MaSP).Select(l => $"{l.MaSP}:{l.SoLuong}:{N(l.DonGiaBan)}")));
    }
}
