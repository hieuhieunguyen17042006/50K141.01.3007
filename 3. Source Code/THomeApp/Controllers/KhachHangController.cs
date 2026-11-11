using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers;

[Authorize]
public class KhachHangController(THomeDbContext db,IActivityLogger logger) : UiFormController
{
    public async Task<IActionResult> Index(string? search=null)
    {
        ViewBag.Search=search;
        var query=db.KhachHangs.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(search)) {var term=search.Trim().ToLower();query=query.Where(k=>k.HoTen.ToLower().Contains(term)||k.SDT.Contains(term)||(k.TenCongTy!=null&&k.TenCongTy.ToLower().Contains(term))||k.MaKH.ToLower().Contains(term));}
        return View(await query.OrderBy(k=>k.MaKH).ToListAsync());
    }

    [HttpGet]
    public IActionResult ChiTiet(string id) => RedirectToAction(nameof(Index),new {customer=id});
    [HttpGet]
    public IActionResult Sua(string id) => RedirectToAction(nameof(Index),new {customer=id,edit=1});
    [HttpGet]
    public IActionResult Them() => RedirectToAction(nameof(Index),new {add=1});

    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> ThongTin(string id)
    {
        var customer=await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k=>k.MaKH==id);
        return customer==null?NotFound():Json(customer);
    }

    [HttpGet]
    public async Task<IActionResult> HoaDonDaMua(string id)
    {
        var customer=await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k=>k.MaKH==id);
        if(customer==null) return NotFound();
        var invoices=await db.HoaDons.AsNoTracking().Where(h=>h.MaKH==id&&h.LoaiChungTu=="HoaDon").OrderByDescending(h=>h.NgayPhatHanh??h.NgayTao).ToListAsync();
        return View(new CustomerPurchases(customer,invoices));
    }

    private async Task ValidateNew(KhachHang model)
    {
        ModelState.Remove("MaKH");
        if(model.LoaiKhach is not ("Cá nhân" or "Doanh nghiệp")) ModelState.AddModelError("LoaiKhach","Vui lòng chọn Cá nhân hoặc Doanh nghiệp.");
        if(!string.IsNullOrWhiteSpace(model.GioiTinh)&&model.GioiTinh is not ("Nam" or "Nữ" or "Khác")) ModelState.AddModelError("GioiTinh","Giới tính không hợp lệ.");
        await ValidateContact(model.SDT,model.Email);
        if(model.LoaiKhach=="Doanh nghiệp") {
            if(string.IsNullOrWhiteSpace(model.TenCongTy)) ModelState.AddModelError("TenCongTy","Vui lòng nhập tên công ty.");
            if(string.IsNullOrWhiteSpace(model.MaSoThue)||!Regex.IsMatch(model.MaSoThue,@"\A([0-9]{10}|[0-9]{13})\z")) ModelState.AddModelError("MaSoThue","Mã số thuế phải gồm 10 hoặc 13 chữ số.");
        }
    }

    private async Task ValidateContact(string? phone,string? email,string? except=null)
    {
        if(string.IsNullOrEmpty(phone)||!Regex.IsMatch(phone,@"\A0[0-9]{9}\z")) ModelState.AddModelError("SDT","Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.");
        else if(await db.KhachHangs.AnyAsync(k=>k.SDT==phone&&k.MaKH!=except)) ModelState.AddModelError("SDT","Số điện thoại đã tồn tại ở khách hàng khác.");
        if(!string.IsNullOrWhiteSpace(email)&&(!new EmailAddressAttribute().IsValid(email)||email.Length>100)) ModelState.AddModelError("Email","Email không đúng định dạng hoặc vượt 100 ký tự.");
    }

    private async Task Create(KhachHang model)
    {
        var codes=await db.KhachHangs.Select(k=>k.MaKH).ToListAsync();
        var next=codes.Where(c=>c.StartsWith("KH")).Select(c=>int.TryParse(c[2..],out var number)?number:0).DefaultIfEmpty(0).Max()+1;
        model.MaKH=$"KH{next:D4}";model.TongTichLuy=0;model.NgayTao=DateTime.Now;model.HoTen=model.HoTen.Trim();
        if(model.LoaiKhach=="Cá nhân") {model.TenCongTy=null;model.MaSoThue=null;}
        db.KhachHangs.Add(model);await db.SaveChangesAsync();
        await logger.LogAsync("KhachHang",$"Thêm khách hàng {model.MaKH} ({model.HoTen})",null,User.FindFirstValue("MaNV"));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(KhachHang model)
    {
        await ValidateNew(model);
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        await Create(model);
        return FormSuccess("Thêm thành công.",nameof(Index));
    }

    // Keep the order screen's existing JSON quick-add contract.
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuKhachHangAjax([FromBody] KhachHang model)
    {
        if(model is null) return Json(new {success=false,message="Vui lòng nhập thông tin khách hàng."});
        await ValidateNew(model);
        if(!ModelState.IsValid) return Json(new {success=false,message=string.Join(" ",ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage)),
            errors=ModelState.Where(p=>p.Value?.Errors.Count>0).ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>e.ErrorMessage).ToArray())});
        await Create(model);return Json(new {success=true,kh=model});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(string maKH,string? sdt,string? diaChi,string? email,string? ghiChu,string? lyDo)
    {
        var customer=await db.KhachHangs.FirstOrDefaultAsync(k=>k.MaKH==maKH);
        if(customer==null) return NotFound();
        await ValidateContact(sdt,email,maKH);RequireReason(lyDo);
        if(diaChi?.Length>255) ModelState.AddModelError("DiaChi","Địa chỉ không được quá 255 ký tự.");
        if(ghiChu?.Length>500) ModelState.AddModelError("GhiChu","Ghi chú không được quá 500 ký tự.");
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        // Deliberate allowlist: posted identity/type/name/company/totals are ignored.
        customer.SDT=sdt!;customer.DiaChi=diaChi;customer.Email=email;customer.GhiChu=ghiChu;
        await db.SaveChangesAsync();
        await logger.LogAsync("KhachHang",$"Sửa thông tin khách hàng {customer.MaKH} ({customer.HoTen})",lyDo!.Trim(),User.FindFirstValue("MaNV"));
        return FormSuccess("Sửa thành công.",nameof(Index));
    }
}
