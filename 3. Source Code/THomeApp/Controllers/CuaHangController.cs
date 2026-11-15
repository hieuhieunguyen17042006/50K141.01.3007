using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using THomeApp.Data;
using THomeApp.Models;
using THomeApp.Services;

namespace THomeApp.Controllers;

[Authorize(Roles="Chủ cửa hàng")]
public class CuaHangController(THomeDbContext db, IActivityLogger logger) : UiFormController
{
    private string Actor => User.FindFirstValue("MaNV")!;

    [HttpGet]
    public async Task<IActionResult> ThemSanPham()
    {
        ViewBag.ExistingProductCodes=await db.SanPhams.AsNoTracking().Select(s=>s.MaSP).ToListAsync();
        return View(new ProductInput());
    }

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

    private IActionResult ProductInputErrors(bool duplicateCode)
    {
        if(!WantsJson) return FormErrors(nameof(Index));
        return Json(new {success=false,code=duplicateCode?"PRODUCT_CODE_EXISTS":"VALIDATION_ERROR",
            message=duplicateCode?"Mã sản phẩm đã tồn tại!":"Thông tin không hợp lệ!",
            errors=ModelState.Where(p=>p.Value?.Errors.Count>0).ToDictionary(p=>p.Key,p=>p.Value!.Errors.Select(e=>e.ErrorMessage).ToArray())});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSanPham(ProductInput model)
    {
        ValidateProduct(model);
        model.MaSP=(model.MaSP??"").Trim().ToUpperInvariant();
        var duplicateCode=await db.SanPhams.AnyAsync(s=>s.MaSP==model.MaSP);
        if(duplicateCode) ModelState.AddModelError("MaSP","Mã sản phẩm đã tồn tại!");
        if(!ModelState.IsValid) return ProductInputErrors(duplicateCode);
        var product=new SanPham {MaSP=model.MaSP,TenSP=model.TenSP.Trim(),DanhMuc=model.DanhMuc.Trim(),DonViTinh=model.DonViTinh.Trim(),GhiChu=model.GhiChu?.Trim(),NgayTao=DateTime.Now};
        product.LichSuGia.Add(new Gia {MaSP=product.MaSP,DonGia=model.DonGia!.Value,NgayApDung=DateTime.Now});
        db.SanPhams.Add(product);
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException e) when(e.InnerException is SqlException {Number:2601 or 2627})
        {
            // The global write lock handles app requests; the PK also protects against
            // concurrent writers outside the app. Do not expose raw SQL or misclassify other failures.
            if(!await db.SanPhams.AsNoTracking().AnyAsync(s=>s.MaSP==model.MaSP)) throw;
            ModelState.AddModelError("MaSP","Mã sản phẩm đã tồn tại!");
            return ProductInputErrors(true);
        }
        await logger.LogAsync("SanPham",$"Thêm sản phẩm {product.MaSP} ({product.TenSP}), giá {model.DonGia:N0} đ",null,Actor);
        return FormSuccess("Thêm thành công.",nameof(Index));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaSanPham(ProductInput model)
    {
        var product=await db.SanPhams.Include(s=>s.LichSuGia).FirstOrDefaultAsync(s=>s.MaSP==model.MaSP&&!s.IsDeleted);
        if(product==null) return NotFound();
        // Reduced edit form keeps the catalog identity/category/unit from the server.
        model.DanhMuc=product.DanhMuc;model.DonViTinh=product.DonViTinh;
        ModelState.Remove(nameof(model.DanhMuc));ModelState.Remove(nameof(model.DonViTinh));
        ValidateProduct(model);
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        var oldPrice=product.LichSuGia.Where(g=>g.NgayApDung<=DateTime.Now).OrderByDescending(g=>g.NgayApDung).ThenByDescending(g=>g.MaGia).FirstOrDefault()?.DonGia;
        var before = new { product.MaSP, product.TenSP, product.DanhMuc, product.DonViTinh, product.GhiChu, DonGia=oldPrice };
        product.TenSP=model.TenSP.Trim();product.DanhMuc=model.DanhMuc.Trim();product.DonViTinh=model.DonViTinh.Trim();product.GhiChu=model.GhiChu?.Trim();
        // Invoice detail rows retain their own DonGiaBan snapshot.
        if(oldPrice!=model.DonGia) product.LichSuGia.Add(new Gia {MaSP=product.MaSP,DonGia=model.DonGia!.Value,NgayApDung=DateTime.Now});
        await db.SaveChangesAsync();
        await logger.LogAsync("SanPham",$"Sửa sản phẩm {product.MaSP}: giá {oldPrice:N0} → {model.DonGia:N0} đ",null,Actor,
            JsonSerializer.Serialize(new {before, after=new {product.MaSP,product.TenSP,product.DanhMuc,product.DonViTinh,product.GhiChu,DonGia=model.DonGia}}));
        return FormSuccess("Sửa thành công.",nameof(Index));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaSanPham(string maSP,string? lyDoXoa)
    {
        if(string.IsNullOrWhiteSpace(lyDoXoa))ModelState.AddModelError("lyDoXoa","Vui lòng điền đầy đủ lý do xóa!");else RequireReason(lyDoXoa,"lyDoXoa");
        if(!ModelState.IsValid) return FormErrors(nameof(Index));
        var product=await db.SanPhams.FirstOrDefaultAsync(s=>s.MaSP==maSP&&!s.IsDeleted);
        if(product==null) return NotFound();
        if(await db.CT_HoaDons.AnyAsync(c=>c.MaSP==maSP)||await db.CT_Phieus.AnyAsync(c=>c.MaSP==maSP)
            ||await db.CT_DotGiaoHangs.AnyAsync(c=>c.MaSP==maSP))
        {
            if(WantsJson) return new JsonResult(new {success=false,code="PRODUCT_IN_USE",message="Không thể xóa!"}) {StatusCode=409};
            TempData["ErrorMessage"]="Không thể xóa!";
            return RedirectToAction(nameof(Index));
        }
        product.IsDeleted=true;await db.SaveChangesAsync();
        await logger.LogAsync("SanPham",$"Xóa sản phẩm {maSP} ({product.TenSP})",lyDoXoa!.Trim(),Actor);
        return FormSuccess("Xóa thành công.",nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChietKhau() => View(await db.ChietKhaus.AsNoTracking().Where(c=>c.DangApDung).OrderBy(c=>c.MocTichLuy).ThenBy(c=>c.MaCK).ToListAsync());

    private async Task ValidateDiscount(DiscountInput model)
    {
        if(model.MocTichLuy<0||model.MocTichLuy>1_000_000_000_000m) ModelState.AddModelError("MocTichLuy","Mốc tích lũy phải từ 0 đến 1.000.000.000.000 VNĐ.");
        if(model.TyLeChietKhau<0||model.TyLeChietKhau>100) ModelState.AddModelError("TyLeChietKhau","Tỷ lệ chiết khấu phải từ 0% đến 100%.");
        if(model.TyLeChietKhau.HasValue&&decimal.Round(model.TyLeChietKhau.Value,2)!=model.TyLeChietKhau.Value)
            ModelState.AddModelError("TyLeChietKhau","Tỷ lệ chiết khấu chỉ được có tối đa 2 chữ số thập phân.");
        if(model.MocTichLuy.HasValue&&model.MocTichLuy!=decimal.Truncate(model.MocTichLuy.Value)) ModelState.AddModelError("MocTichLuy","Mốc tích lũy phải là số nguyên.");
        if(!ModelState.IsValid) return;
        if(model.MocTichLuy.HasValue&&await db.ChietKhaus.AnyAsync(c=>c.MaCK!=model.MaCK&&c.DangApDung&&c.MocTichLuy==model.MocTichLuy))
            ModelState.AddModelError("MocTichLuy","Mốc tích lũy đã có mức chiết khấu đang áp dụng.");
        if(model.TyLeChietKhau.HasValue&&await db.ChietKhaus.AnyAsync(c=>c.MaCK!=model.MaCK&&c.DangApDung&&c.TyLeChietKhau==model.TyLeChietKhau))
            ModelState.AddModelError("TyLeChietKhau","Tỷ lệ chiết khấu đã tồn tại!");
        if(model.MocTichLuy.HasValue&&model.TyLeChietKhau.HasValue)
        {
            // The write filter holds the transaction lock: re-read current neighbours,
            // excluding the edited row, rather than trusting the browser's policy list.
            var others=db.ChietKhaus.AsNoTracking().Where(c=>c.DangApDung&&c.MaCK!=model.MaCK);
            var lower=await others.Where(c=>c.MocTichLuy<model.MocTichLuy.Value)
                .OrderByDescending(c=>c.MocTichLuy).ThenByDescending(c=>c.TyLeChietKhau).FirstOrDefaultAsync();
            var upper=await others.Where(c=>c.MocTichLuy>model.MocTichLuy.Value)
                .OrderBy(c=>c.MocTichLuy).ThenBy(c=>c.TyLeChietKhau).FirstOrDefaultAsync();
            if(lower!=null&&model.TyLeChietKhau.Value<=lower.TyLeChietKhau)
                ModelState.AddModelError("TyLeChietKhau",$"Tỷ lệ phải lớn hơn {lower.TyLeChietKhau}% của mốc thấp hơn gần nhất (#{lower.MaCK}).");
            if(upper!=null&&model.TyLeChietKhau.Value>=upper.TyLeChietKhau)
                ModelState.AddModelError("TyLeChietKhau",$"Tỷ lệ phải nhỏ hơn {upper.TyLeChietKhau}% của mốc cao hơn gần nhất (#{upper.MaCK}).");
        }
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
        await ValidateDiscount(model);
        var discount=await db.ChietKhaus.FirstOrDefaultAsync(c=>c.MaCK==model.MaCK&&c.DangApDung);
        if(discount==null) return NotFound();
        if(!ModelState.IsValid) return FormErrors(nameof(ChietKhau));
        var before=$"mốc {discount.MocTichLuy}, tỷ lệ {discount.TyLeChietKhau}%";
        discount.MocTichLuy=model.MocTichLuy!.Value;discount.TyLeChietKhau=model.TyLeChietKhau!.Value;
        await db.SaveChangesAsync();
        await logger.LogAsync("ChietKhau",$"Sửa mức chiết khấu #{discount.MaCK}; trước: {before}; sau: mốc {discount.MocTichLuy}, tỷ lệ {discount.TyLeChietKhau}%",null,Actor);
        return FormSuccess("Sửa thành công.",nameof(ChietKhau));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaChietKhau(int maCK)
    {
        var discount=await db.ChietKhaus.FirstOrDefaultAsync(c=>c.MaCK==maCK&&c.DangApDung);
        if(discount==null) return NotFound();
        if(!ModelState.IsValid) return FormErrors(nameof(ChietKhau));
        discount.DangApDung=false;await db.SaveChangesAsync();
        await logger.LogAsync("ChietKhau",$"Xóa mức chiết khấu #{maCK}: mốc {discount.MocTichLuy}, tỷ lệ {discount.TyLeChietKhau}%; đang áp dụng: true → false",null,Actor);
        return FormSuccess("Xóa thành công.",nameof(ChietKhau));
    }

    [HttpGet]
    public async Task<IActionResult> LichSuHoatDong(string? search=null,string? doiTuong=null)
    {
        ViewBag.Search=search;ViewBag.DoiTuong=doiTuong;
        var query=db.LichSuHoatDongs.AsNoTracking().Include(l=>l.NhanVien).AsQueryable();
        if(!string.IsNullOrWhiteSpace(search)) {var term=search.Trim().ToLower();query=query.Where(l=>l.NoiDungThaoTac.ToLower().Contains(term)||(l.LyDo!=null&&l.LyDo.ToLower().Contains(term))||(l.NhanVien!=null&&l.NhanVien.HoTen.ToLower().Contains(term)));}
        if(!string.IsNullOrWhiteSpace(doiTuong)&&doiTuong!="Tất cả") query=query.Where(l=>l.LoaiDoiTuong==doiTuong||(doiTuong=="HoaDon"&&l.LoaiDoiTuong=="YeuCauMoKhoa"));
        return View(await query.OrderByDescending(l=>l.ThoiGianThaoTac).Take(200).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> YeuCauMoKhoa() => View(await db.YeuCauMoKhoas.AsNoTracking().Include(y=>y.NhanVien).Include(y=>y.HoaDon).OrderByDescending(y=>y.ThoiGianGui).ToListAsync());

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> XuLyMoKhoa(int maYC,[FromForm(Name="action")] string decision)
    {
        var request=await db.YeuCauMoKhoas.Include(y=>y.HoaDon).FirstOrDefaultAsync(y=>y.MaYC==maYC);
        if(request==null) return NotFound();
        if(request.TrangThai!="Chờ duyệt"||(decision!="Duyet"&&decision!="TuChoi")) ModelState.AddModelError("ghiChuDuyet","Yêu cầu đã được xử lý hoặc lựa chọn không hợp lệ.");
        if(!ModelState.IsValid) return FormErrors(nameof(YeuCauMoKhoa));
        var before=new {request.TrangThai,request.MaHD,request.LyDoMoKhoa,SoLanGhiDe=request.HoaDon?.SoLanGhiDe,IsLockedGhiDe=request.HoaDon?.IsLockedGhiDe};
        // DocumentWriteFilter holds the transaction-owned SQL lock across this read and update.
        request.ThoiGianDuyet=DateTime.Now;request.MaNVDuyet=Actor;request.GhiChuDuyet=null;request.TrangThai=decision=="Duyet"?"Đã duyệt":"Từ chối";
        // Preserve the existing one-approval/one-additional-override rule in BanHang.
        if(decision=="Duyet"&&request.HoaDon!=null) request.HoaDon.IsLockedGhiDe=false;
        await db.SaveChangesAsync();
        await logger.LogAsync("YeuCauMoKhoa",$"Yêu cầu #{maYC} mở khóa ghi đè hóa đơn {request.MaHD}: Chờ duyệt → {request.TrangThai}; người duyệt {Actor}, lúc {request.ThoiGianDuyet:dd/MM/yyyy HH:mm:ss}",request.LyDoMoKhoa,Actor,
            JsonSerializer.Serialize(new {before,after=new {request.TrangThai,request.MaHD,request.MaNVDuyet,request.ThoiGianDuyet,SoLanGhiDe=request.HoaDon?.SoLanGhiDe,IsLockedGhiDe=request.HoaDon?.IsLockedGhiDe}}));
        return FormSuccess(decision=="Duyet"?"Đã duyệt yêu cầu mở khóa.":"Đã từ chối yêu cầu mở khóa.",nameof(YeuCauMoKhoa));
    }
}
