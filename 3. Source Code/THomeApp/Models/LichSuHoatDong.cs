using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class LichSuHoatDong
    {
        [Key]
        public int MaLichSu { get; set; }

        public DateTime ThoiGianThaoTac { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(100)]
        public string LoaiDoiTuong { get; set; } = string.Empty; // "DonHang", "HoaDon", "KhachHang", "SanPham", "ChietKhau", "NhanVien", "YeuCauMoKhoa"

        [Required]
        [MaxLength(1000)]
        public string NoiDungThaoTac { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LyDo { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }
    }
}
