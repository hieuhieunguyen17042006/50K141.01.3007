using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using THomeApp.Data;
using THomeApp.Services;
namespace THomeApp.Controllers;
[Authorize]
public class QrController(THomeDbContext db, PaymentQr qr, OrderPreparationStore preparations) : Controller
{
    [HttpGet, Authorize(Roles = "Chủ cửa hàng,Kế toán bán hàng,Nhân viên bán hàng")]
    public async Task<IActionResult> Prepared(string id)
    {
        var prepared = await preparations.GetAsync(User.FindFirstValue("MaNV")!, HttpContext.Session.GetString(OrderPreparationStore.SessionScopeKey), id);
        if (prepared == null) return NotFound();
        var amount = prepared.Preview.TyLeDatCoc > 0 ? prepared.Preview.TienDatCoc : prepared.Preview.TongThanhToan;
        var lookupUrl = Url.Action("MaQR", "BanHang", new { preparedId = prepared.Id, id = prepared.Preview.MaHD }, Request.Scheme)!;
        return RenderQr(prepared.Preview.MaHD, amount, lookupUrl);
    }
    [HttpGet, Authorize(Roles = "Chủ cửa hàng,Kế toán bán hàng,Nhân viên bán hàng")]
    public async Task<IActionResult> Invoice(string id, decimal? amount)
    {
        var invoice = await db.HoaDons.FindAsync(id);
        if (invoice == null) return NotFound();
        var value = amount ?? invoice.ConNo;
        if (invoice.TrangThaiThanhToan == "Đã hủy" || value < 0 || value > invoice.ConNo) return BadRequest();
        return RenderQr(invoice.MaHD, value);
    }

    [HttpGet, Authorize(Roles = "Chủ cửa hàng,Kế toán bán hàng,Nhân viên bán hàng")]
    public async Task<IActionResult> Document(string id)
    {
        var invoice = await db.HoaDons.FindAsync(id);
        if (invoice == null) return NotFound();
        if (invoice.TrangThaiThanhToan == "Đã hủy" || invoice.TongThanhToan < 0 ||
            (invoice.LoaiChungTu != "HoaDon" && invoice.ThuRong < invoice.TongThanhToan)) return BadRequest();
        // QR printed on a paid document references its full total; it does not record a collection.
        return RenderQr(invoice.MaHD, invoice.TongThanhToan);
    }

    private IActionResult RenderQr(string id, decimal value, string? lookupUrl = null)
    {
        var url = lookupUrl ?? Url.Action("ChiTietHoaDon", "BanHang", new { id }, Request.Scheme)!;
        using var data = QRCodeGenerator.GenerateQrCode(qr.Payload(id, value, url), QRCodeGenerator.ECCLevel.M);
        using var png = new PngByteQRCode(data);
        Response.Headers.CacheControl = "no-store";
        return File(png.GetGraphic(12), "image/png");
    }
}
