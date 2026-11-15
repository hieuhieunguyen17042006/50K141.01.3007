using System.ComponentModel.DataAnnotations;

namespace THomeApp.Models;

// Profile edits cannot bind passwords, account status, or employee identifiers.
public sealed class EmployeeProfileInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên nhân viên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự.")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và bắt đầu bằng 0.")]
    public string SDT { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số CCCD.")]
    [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "CCCD phải gồm đúng 12 chữ số.")]
    public string CCCD { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn vị trí công việc.")]
    public string ViTri { get; set; } = "";

    [StringLength(255, ErrorMessage = "Địa chỉ không được quá 255 ký tự.")]
    public string? DiaChi { get; set; }
}
