using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class YeuCauMoKhoa
    {
        [Key]
        public int MaYC { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [ForeignKey("MaHD")]
        public virtual HoaDon? HoaDon { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaNV { get; set; } = string.Empty;

        [ForeignKey("MaNV")]
        public virtual NhanVien? NhanVien { get; set; }

        [Required]
        [MaxLength(500)]
        public string LyDoMoKhoa { get; set; } = string.Empty;

        public DateTime ThoiGianGui { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Chờ duyệt"; // "Chờ duyệt", "Đã duyệt", "Từ chối"

        public DateTime? ThoiGianDuyet { get; set; }

        [MaxLength(20)]
        public string? MaNVDuyet { get; set; }

        [ForeignKey("MaNVDuyet")]
        public virtual NhanVien? NhanVienDuyet { get; set; }

        [MaxLength(500)]
        public string? GhiChuDuyet { get; set; }
    }
}
