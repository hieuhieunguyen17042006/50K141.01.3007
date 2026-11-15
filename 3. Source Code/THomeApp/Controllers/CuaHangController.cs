using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using THomeApp.Data;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers;

[Authorize(Roles="Chủ cửa hàng")]
public class CuaHangController(THomeDbContext db, IActivityLogger logger) : UiFormController
{
    private string Actor => User.FindFirstValue("MaNV")!;

    public async Task<IActionResult> Index(string? search=null, string? danhmuc=null)
    {
        ViewBag.Search=search; ViewBag.DanhMuc=danhmuc;
        var query=db.SanPhams.AsNoTracking().Include(s=>s.LichSuGia).Where(s=>!s.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search)) { var term=search.Trim().ToLower(); query=query.Where(s=>s.MaSP.ToLower().Contains(term)||s.TenSP.ToLower().Contains(term)); }
        if (!string.IsNullOrEmpty(danhmuc)&&danhmuc!="Tất cả") query=query.Where(s=>s.DanhMuc==danhmuc);
        return View(await query.OrderBy(s=>s.MaSP).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> SuaSanPham(string id)
    {
        var product=await db.SanPhams.AsNoTracking().Include(s=>s.LichSuGia).FirstOrDefaultAsync(s=>s.MaSP==id&&!s.IsDeleted);
        if(product==null) return NotFound();
        return View(new ProductInput {MaSP=product.MaSP,TenSP=product.TenSP,DanhMuc=product.DanhMuc,DonViTinh=product.DonViTinh,GhiChu=product.GhiChu,
            DonGia=product.LichSuGia.Where(g=>g.NgayApDung<=DateTime.Now).OrderByDescending(g=>g.NgayApDung).ThenByDescending(g=>g.MaGia).FirstOrDefault()?.DonGia});
    }

    private void ValidateProduct(ProductInput model)
    {
        if(model.DonGia.HasValue&&(model.DonGia<=0||model.DonGia!=decimal.Truncate(model.DonGia.Value)||model.DonGia>1_000_000_000_000m))
            ModelState.AddModelError("DonGia","Đơn giá phải là số nguyên dương, không vượt 1.000.000.000.000 VNĐ.");
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSanPham(ProductInput model)
    {
        ValidateProduct(model);
        model.MaSP=(model.MaSP??"").Trim().ToUpperInvariant();
        if(await db.SanPhams.AnyAsync(s=>s.MaSP==model.MaSP)) ModelState.AddModelError("MaSP","Mã sản phẩm đã tồn tại.");
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        var product=new SanPham {MaSP=model.MaSP,TenSP=model.TenSP.Trim(),DanhMuc=model.DanhMuc.Trim(),DonViTinh=model.DonViTinh.Trim(),GhiChu=model.GhiChu?.Trim(),NgayTao=DateTime.Now};
        product.LichSuGia.Add(new Gia {MaSP=product.MaSP,DonGia=model.DonGia!.Value,NgayApDung=DateTime.Now});
        db.SanPhams.Add(product);await db.SaveChangesAsync();
        await logger.LogAsync("SanPham",$"Thêm sản phẩm {product.MaSP} ({product.TenSP}), giá {model.DonGia:N0} đ",null,Actor);
        return FormSuccess("Thêm thành công.",nameof(Index));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaSanPham(ProductInput model)
    {
        ValidateProduct(model);RequireReason(model.LyDo);
        var product=await db.SanPhams.Include(s=>s.LichSuGia).FirstOrDefaultAsync(s=>s.MaSP==model.MaSP&&!s.IsDeleted);
        if(product==null) return NotFound();
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        var oldPrice=product.LichSuGia.Where(g=>g.NgayApDung<=DateTime.Now).OrderByDescending(g=>g.NgayApDung).ThenByDescending(g=>g.MaGia).FirstOrDefault()?.DonGia;
        product.TenSP=model.TenSP.Trim();product.DanhMuc=model.DanhMuc.Trim();product.DonViTinh=model.DonViTinh.Trim();product.GhiChu=model.GhiChu?.Trim();
        // Invoice detail rows retain their own DonGiaBan snapshot.
        if(oldPrice!=model.DonGia) product.LichSuGia.Add(new Gia {MaSP=product.MaSP,DonGia=model.DonGia!.Value,NgayApDung=DateTime.Now});
        await db.SaveChangesAsync();
        await logger.LogAsync("SanPham",$"Sửa sản phẩm {product.MaSP}: giá {oldPrice:N0} → {model.DonGia:N0} đ",model.LyDo!.Trim(),Actor);
        return FormSuccess("Sửa thành công.",nameof(Index));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaSanPham(string maSP,string? lyDoXoa)
    {
        RequireReason(lyDoXoa,"lyDoXoa");
        var product=await db.SanPhams.FirstOrDefaultAsync(s=>s.MaSP==maSP&&!s.IsDeleted);
        if(product==null) return NotFound();
        if(await db.CT_HoaDons.AnyAsync(c=>c.MaSP==maSP&&c.HoaDon!.LoaiChungTu=="DonTamTinh"))
            ModelState.AddModelError("lyDoXoa","Không thể xóa sản phẩm đang có trong đơn tạm tính.");
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        product.IsDeleted=true;await db.SaveChangesAsync();
        await logger.LogAsync("SanPham",$"Xóa sản phẩm {maSP} ({product.TenSP})",lyDoXoa!.Trim(),Actor);
        return FormSuccess("Xóa thành công.",nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChietKhau() => View(await db.ChietKhaus.AsNoTracking().Where(c=>c.DangApDung).OrderBy(c=>c.MocTichLuy).ToListAsync());

    private async Task ValidateDiscount(DiscountInput model)
    {
        if(model.MocTichLuy<0||model.MocTichLuy>1_000_000_000_000m) ModelState.AddModelError("MocTichLuy","Mốc tích lũy phải từ 0 đến 1.000.000.000.000 VNĐ.");
        if(model.TyLeChietKhau<0||model.TyLeChietKhau>100) ModelState.AddModelError("TyLeChietKhau","Tỷ lệ chiết khấu phải từ 0% đến 100%.");
        if(model.MocTichLuy.HasValue&&await db.ChietKhaus.AnyAsync(c=>c.MaCK!=model.MaCK&&c.DangApDung&&c.MocTichLuy==model.MocTichLuy))
            ModelState.AddModelError("MocTichLuy","Mốc tích lũy đã có mức chiết khấu đang áp dụng.");
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemChietKhau(DiscountInput model)
    {
        model.MaCK=0;await ValidateDiscount(model);
        if(!ModelState.IsValid) return FormErrors(nameof(ChietKhau));
        var discount=new ChietKhau {MocTichLuy=model.MocTichLuy!.Value,TyLeChietKhau=model.TyLeChietKhau!.Value,DangApDung=true,NgayTao=DateTime.Now};
        db.ChietKhaus.Add(discount);await db.SaveChangesAsync();
        await logger.LogAsync("ChietKhau",$"Thêm mức chiết khấu #{discount.MaCK}: mốc {discount.MocTichLuy:N0} đ, tỷ lệ {discount.TyLeChietKhau}%",null,Actor);
        return FormSuccess("Thêm thành công.",nameof(ChietKhau));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaChietKhau(DiscountInput model)
    {
        await ValidateDiscount(model);RequireReason(model.LyDo);
        var discount=await db.ChietKhaus.FirstOrDefaultAsync(c=>c.MaCK==model.MaCK&&c.DangApDung);
        if(discount==null) return NotFound();
        if(!ModelState.IsValid) return FormErrors(nameof(ChietKhau));
        discount.MocTichLuy=model.MocTichLuy!.Value;discount.TyLeChietKhau=model.TyLeChietKhau!.Value;
        await db.SaveChangesAsync();
        await logger.LogAsync("ChietKhau",$"Sửa mức chiết khấu #{discount.MaCK}: mốc {discount.MocTichLuy:N0} đ, tỷ lệ {discount.TyLeChietKhau}%",model.LyDo!.Trim(),Actor);
        return FormSuccess("Sửa thành công.",nameof(ChietKhau));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaChietKhau(int maCK,string? lyDo)
    {
        RequireReason(lyDo);
        var discount=await db.ChietKhaus.FirstOrDefaultAsync(c=>c.MaCK==maCK&&c.DangApDung);
        if(discount==null) return NotFound();
        if(!ModelState.IsValid) return FormErrors(nameof(ChietKhau));
        discount.DangApDung=false;await db.SaveChangesAsync();
        await logger.LogAsync("ChietKhau",$"Xóa mức chiết khấu #{maCK}",lyDo!.Trim(),Actor);
        return FormSuccess("Xóa thành công.",nameof(ChietKhau));
    }

    [HttpGet]
    public async Task<IActionResult> LichSuHoatDong(string? search=null,string? doiTuong=null)
    {
        ViewBag.Search=search;ViewBag.DoiTuong=doiTuong;
        var query=db.LichSuHoatDongs.AsNoTracking().Include(l=>l.NhanVien).AsQueryable();
        if(!string.IsNullOrWhiteSpace(search)) {var term=search.Trim().ToLower();query=query.Where(l=>l.NoiDungThaoTac.ToLower().Contains(term)||(l.LyDo!=null&&l.LyDo.ToLower().Contains(term))||(l.NhanVien!=null&&l.NhanVien.HoTen.ToLower().Contains(term)));}
        if(!string.IsNullOrWhiteSpace(doiTuong)&&doiTuong!="Tất cả") query=query.Where(l=>l.LoaiDoiTuong==doiTuong);
        return View(await query.OrderByDescending(l=>l.ThoiGianThaoTac).Take(200).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> YeuCauMoKhoa() => View(await db.YeuCauMoKhoas.AsNoTracking().Include(y=>y.NhanVien).Include(y=>y.HoaDon).OrderByDescending(y=>y.ThoiGianGui).ToListAsync());

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XuLyMoKhoa(int maYC,[FromForm(Name="action")] string decision,string? ghiChuDuyet)
    {
        var request=await db.YeuCauMoKhoas.Include(y=>y.HoaDon).FirstOrDefaultAsync(y=>y.MaYC==maYC);
        if(request==null) return NotFound();
        if(request.TrangThai!="Chờ duyệt"||(decision!="Duyet"&&decision!="TuChoi")) ModelState.AddModelError("ghiChuDuyet","Yêu cầu đã được xử lý hoặc lựa chọn không hợp lệ.");
        RequireReason(ghiChuDuyet,"ghiChuDuyet");
        if(!ModelState.IsValid) return FormErrors(nameof(YeuCauMoKhoa));
        request.ThoiGianDuyet=DateTime.Now;request.MaNVDuyet=Actor;request.GhiChuDuyet=ghiChuDuyet!.Trim();request.TrangThai=decision=="Duyet"?"Đã duyệt":"Từ chối";
        // Preserve the existing one-approval/one-additional-override rule in BanHang.
        if(decision=="Duyet"&&request.HoaDon!=null) request.HoaDon.IsLockedGhiDe=false;
        await db.SaveChangesAsync();
        await logger.LogAsync("YeuCauMoKhoa",$"{request.TrangThai} yêu cầu #{maYC} mở khóa ghi đè hóa đơn {request.MaHD}",request.GhiChuDuyet,Actor);
        return FormSuccess(decision=="Duyet"?"Duyệt mở khóa thành công.":"Đã từ chối yêu cầu mở khóa.",nameof(YeuCauMoKhoa));
    }
}
