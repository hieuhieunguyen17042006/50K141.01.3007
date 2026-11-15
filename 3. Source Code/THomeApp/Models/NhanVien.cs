using System;
using System.ComponentModel.DataAnnotations;

namespace THomeApp.Models
{
    public class NhanVien
    {
        [Key]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string SDT { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DiaChi { get; set; }

        [MaxLength(20)]
        public string? CCCD { get; set; }

        [Required]
        [MaxLength(50)]
        public string ViTri { get; set; } = "Nhân viên bán hàng"; // "Chủ cửa hàng", "Kế toán bán hàng", "Nhân viên bán hàng"

        [Required]
        [MaxLength(10)]
        public string TenDN { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string MatKhau { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Đang hoạt động"; // "Đang hoạt động", "Đã khóa"

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
