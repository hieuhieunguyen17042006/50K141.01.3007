using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace THomeApp.Models
{
    public class CT_Phieu
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)] public string? MaHDGoc { get; set; }
        [MaxLength(50)] public string? MaSPGoc { get; set; }
        public int? SourceVoucherLineId { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaPhieu { get; set; } = string.Empty;

        [ForeignKey("MaPhieu")]
        public virtual Phieu? Phieu { get; set; }

        [Required]
        [MaxLength(50)]
        public string MaSP { get; set; } = string.Empty;

        [ForeignKey("MaSP")]
        public virtual SanPham? SanPham { get; set; }

        [Required]
        [MaxLength(50)]
        public string LoaiDong { get; set; } = "TraHang"; // "TraHang", "DoiMoi"

        [Required]
        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGiaXuLy { get; set; }

        [MaxLength(50)]
        public string TinhTrangHang { get; set; } = "Còn nguyên vẹn"; // "Còn nguyên vẹn", "Hư hỏng"

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        [NotMapped]
        public decimal ThanhTien => DonGiaXuLy * SoLuong;
    }
}
