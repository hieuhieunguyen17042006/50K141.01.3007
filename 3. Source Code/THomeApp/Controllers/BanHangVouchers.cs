using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Models;

namespace THomeApp.Controllers;

public partial class BanHangController
{
    private static bool Adjustable(HoaDon invoice)=>invoice.LoaiChungTu=="HoaDon"&&invoice.TrangThaiThanhToan!="Đã hủy";

    [HttpGet,Authorize(Roles=InvoiceRoles)]
    public async Task<IActionResult> TaoPhieuCPPS(string id)=>await VoucherPage(id,"TaoPhieuCPPS");

    [HttpGet,Authorize(Roles=InvoiceRoles)]
    public async Task<IActionResult> TaoPhieuDoiTra(string id)=>await VoucherPage(id,"TaoPhieuDoiTra");

    private async Task<IActionResult> VoucherPage(string id,string view)
    {
        var invoice=await _context.HoaDons.AsNoTracking().Include(h=>h.KhachHang)
            .Include(h=>h.ChiTietHoaDons).ThenInclude(c=>c.SanPham).FirstOrDefaultAsync(h=>h.MaHD==id||h.MaHoaDon==id);
        if(invoice==null) return NotFound();
        if(!Adjustable(invoice)) return BadRequest("Chỉ lập phiếu cho hóa đơn đã phát hành và chưa hủy.");
        ViewBag.ReturnableLines=await ReturnableLines(invoice);
        ViewBag.Products=await _context.SanPhams.AsNoTracking().Where(s=>!s.IsDeleted).Include(s=>s.LichSuGia).OrderBy(s=>s.TenSP).ToListAsync();
        return View(view,invoice);
    }

    private void ValidateVoucherReason(string? reason,string field)
    {
        if(string.IsNullOrWhiteSpace(reason)||reason.Length>500) ModelState.AddModelError(field,"Vui lòng nhập lý do, tối đa 500 ký tự.");
    }
    private IActionResult VoucherFailure(string maHD,string? field=null,string? description=null)
    {
        if(field!=null) ModelState.AddModelError(field,description??"Không hợp lệ!");
        var errors=ModelState.Where(p=>p.Value?.Errors.Count>0).ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>e.ErrorMessage).ToArray());
        if(Request.Headers.Accept.ToString().Contains("application/json")||Request.Path.Value?.EndsWith("LuuPhieuGhiDe")==true)
            return Json(new {success=false,message="Không hợp lệ!",errors});
        TempData["ErrorMessage"]="Không hợp lệ! "+string.Join(" ",errors.Values.SelectMany(v=>v));
        return RedirectToAction("ChiTietHoaDon",new {id=maHD});
    }
    private IActionResult VoucherSuccess(HoaDon invoice,Phieu voucher,string message)
    {
        if(Request.Headers.Accept.ToString().Contains("application/json"))
            return Json(new {success=true,message,maHD=invoice.MaHD,maPhieu=voucher.MaPhieu,redirect=Url.Action("ChiTietHoaDon",new {id=invoice.MaHD})});
        TempData["SuccessMessage"]=message;return RedirectToAction("ChiTietHoaDon",new {id=invoice.MaHD});
    }
    // Stored on the voucher in the same transaction as the adjustment, never recomputed from live catalog prices.
    private static string InvoiceSnapshot(HoaDon h)=>JsonSerializer.Serialize(new {
        Version=1,h.MaHD,h.MaHoaDon,h.MaKH,Customer=new {h.KhachHang?.HoTen,h.KhachHang?.SDT},h.DiaChiGiao,h.GhiChu,h.MaCK,h.TongTienHang,h.TienChietKhau,h.ChiPhiVanChuyen,h.TienThue,
        h.TongThanhToan,h.DaThu,h.DaHoanKhach,h.SoLanGhiDe,h.TrangThaiThanhToan,
        Items=h.ChiTietHoaDons.OrderBy(c=>c.MaSP).Select(c=>new{c.MaSP,TenSP=c.SanPham?.TenSP,c.DonGiaBan,c.SoLuong,c.SoLuongDaTra})
    });
}
