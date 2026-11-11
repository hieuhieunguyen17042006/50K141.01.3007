using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using THomeApp.Helpers;

namespace THomeApp.Models;

public sealed class ProductInput
{
    [Required(ErrorMessage="Vui lòng nhập mã sản phẩm."), StringLength(50)] public string MaSP { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập tên sản phẩm."), StringLength(200)] public string TenSP { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập danh mục."), StringLength(100)] public string DanhMuc { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập đơn vị tính."), StringLength(50)] public string DonViTinh { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập đơn giá."), ModelBinder(BinderType=typeof(InvariantDecimalBinder))] public decimal? DonGia { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
    [StringLength(500)] public string? LyDo { get; set; }
}

public sealed class DiscountInput
{
    public int MaCK { get; set; }
    [Required(ErrorMessage="Vui lòng nhập mốc giá trị mua hàng tích lũy."), ModelBinder(BinderType=typeof(InvariantDecimalBinder))] public decimal? MocTichLuy { get; set; }
    [Required(ErrorMessage="Vui lòng nhập tỷ lệ chiết khấu."), ModelBinder(BinderType=typeof(InvariantDecimalBinder))] public decimal? TyLeChietKhau { get; set; }
    [StringLength(500)] public string? LyDo { get; set; }
}

public sealed record CustomerPurchases(KhachHang Customer, List<HoaDon> Invoices);
