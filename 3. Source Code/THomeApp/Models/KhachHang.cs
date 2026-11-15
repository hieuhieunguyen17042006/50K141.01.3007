using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class KhachHang
    {
        [Key]
        [MaxLength(20)]
        public string MaKH { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LoaiKhach { get; set; } = "Cá nhân"; // "Cá nhân", "Doanh nghiệp"

        [Required]
        [MaxLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string SDT { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DiaChi { get; set; }

        [MaxLength(10)]
        public string? GioiTinh { get; set; } // "Nam", "Nữ", "Khác"

        [MaxLength(100)]
        public string? Email { get; set; }

        public DateTime? NgaySinh { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        [MaxLength(20)]
        public string? MaSoThue { get; set; }

        [MaxLength(200)]
        public string? TenCongTy { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTichLuy { get; set; } = 0;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
