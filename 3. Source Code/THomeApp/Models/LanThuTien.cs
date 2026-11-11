using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class LanThuTien
    {
        [Key]
        public int MaLanThu { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaHD { get; set; } = string.Empty;

        [ForeignKey("MaHD")]
        public virtual HoaDon? HoaDon { get; set; }

        public DateTime NgayThu { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienThu { get; set; }

        [Required]
        [MaxLength(50)]
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt"; // "Tiền mặt", "Chuyển khoản"

        [Required]
        [MaxLength(200)]
        public string NoiDung { get; set; } = string.Empty; // "Đặt cọc", "Thu tiền đợt 2", "Thu hoàn tất", v.v.

        [Required]
        [MaxLength(20)]
        public string MaNVThu { get; set; } = string.Empty;

        [ForeignKey("MaNVThu")]
        public virtual NhanVien? NhanVienThu { get; set; }

        [Required]
        [MaxLength(50)]
        public string TrangThai { get; set; } = "Hiệu lực"; // "Hiệu lực", "Đã hủy"
    }
}
